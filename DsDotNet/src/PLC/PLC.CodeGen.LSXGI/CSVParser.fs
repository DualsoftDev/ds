namespace Dsu.PLCConverter.FS

open System
open System.Diagnostics
open System.IO
open System.Collections.Generic
open FSharp.Data
open Dual.Common

/// PLC 프로그램을 CSV 저장하였을 때의 하나의 CSV line 에 대한 정보
type ProgramCSVLine = {
    StepNo: int option
    /// Rung Comment
    LineStatement: string
    /// IL command
    Instruction: string
    Arguments: Argument array
    //Blank: string
    //PIStatement: string
}
and Device = {
    Name: string
    Comment: string
}
and Argument =
    | Contact of Device
    | String of string
    | Integer of int
    with
        member x.ToText() =
            match x with
            | Contact(dev) -> dev.Name // + (if dev.Comment.isNullOrEmpty() then "" else sprintf "(%s)" dev.Comment)
            | String(str) -> str
            | Integer(n) -> string n

/// PLC DeviceIO BASES 설정 정보를 읽을때 하나의 CSV line 에 대한 정보
type DeviceIO = {
    Slot : string
    Type : string
    Points:  int
    StartXY:  int
}

/// PLC DeviceRemoteIO  설정 정보를 읽을때 하나의 CSV line 에 대한 정보
type DeviceRemoteIO = {
    RemoteType : string
    StartX: string
    StartY: string
}

type CommentDictionary = Dictionary<string,string>
type Rung = ProgramCSVLine array



/// 단위 POU 당 rung 추출 결과
type POUParseResult = {
    /// Name of POU
    Name: string
    Rungs: Rung array
}


[<AutoOpen>]
module CSVParser =
    type CsvMxProgram = CsvProvider<"../Data/DesignTime/MAIN.csv", SkipRows=2>
    type CsvMxComment = CsvProvider<"../Data/DesignTime/COMMENT.csv", SkipRows=1>
    type CsvMxIO = CsvProvider<"../Data/DesignTime/IO Assignment Setting.csv", SkipRows=3, IgnoreErrors=true, HasHeaders=false>
    type CsvMxRemoteIO = CsvProvider<"../Data/DesignTime/Acknowledge XY Assignment.csv", SkipRows=1>

    /// CSV 에서 읽어 들인 IO Assignment Setting
    let readMxIO (files:string seq) =
        let readMxIO (file:string) =
            let data = CsvMxIO.Load(file)
            seq {
                for row in data.Rows do
                    yield {
                        Slot    = row.Column1 //Slot
                        Type    = row.Column2 //Type
                        Points =  row.Column3 |> fun f -> if(f.HasValue)then f.Value else -1   //Points
                        StartXY = row.Column4 |> fun f -> if(f.HasValue)then f.Value else -1   //Start XY
                        }
            }
        files
        |> Seq.collect readMxIO
        |> Seq.map (fun dv -> dv.Slot, dv.Type, dv.StartXY, dv.Points)

    /// CSV 에서 읽어 들인 Acknowledge XY Assignment
    let readMxRemoteIO (files:string seq) =
        let readMxRemoteIO (file:string) =
            let data = CsvMxRemoteIO.Load(file)
            seq {
                for row in data.Rows do
                    yield {
                        RemoteType    = row.``'Type'``
                        StartX = row.``'Network Assignment X'``
                        StartY =  row.``'Network Assignment Y'``
                        }
            }
        files
        |> Seq.collect readMxRemoteIO
        |> Seq.map (fun dv -> dv.RemoteType, dv.StartX, dv.StartY)


    /// CSV 에서 읽어 들인 comment : DeviceAddress => Comment dictionary
    // ==> dict [("X20", "2차푸셔전진(y45)"); ("X21", "2차푸셔후진"); ("X22", "1차푸셔전진(y44)"); ... |]
    let readCommentCSV (files:string seq) =
        // http://blog.leifbattermann.de/2016/03/24/writing-efficient-and-reliable-code-with-f-type-providers/
        let readCommentCSV (file:string) =
            let data = CsvMxComment.Load(file)
            seq {
                for row in data.Rows do
                    yield {
                        Name    = row.``Device Name``
                        Comment = row.Comment }
            }
        files
        |> Seq.collect readCommentCSV
        |> Seq.map (fun dv -> dv.Name, dv.Comment)
        |> dict
        |> Dictionary

    /// CSV 에서 읽어 들인 line 별 program
    let readProgramCSV (file:string) (commentDic:CommentDictionary) =
        let data = CsvMxProgram.Load(file)

        /// argument 만 존재하고, instruction 이 없는 line 을 바로 전 line 에 병합한다.
        let mergeLines (lines:ProgramCSVLine seq) =
            let lines = lines |> Array.ofSeq
            //assert(lines.[0].Instruction.any()) //test ahn
            //assert(lines.[1..] |> Array.forall(fun line -> line.Instruction.isNullOrEmpty())) //test ahn
            let arguments = lines |> Array.collect (fun line -> line.Arguments)
            {lines.[0] with Arguments = arguments}
        seq {
            for row in data.Rows do
                let dev = row.``I/O(Device)``
                let comment = if commentDic.ContainsKey(dev) then commentDic.[dev] else ""
                let arguments =
                    if dev = "" && comment = "" then
                        Array.empty
                    else
                        [| Contact({Name = dev; Comment = comment}) |]

                yield {
                    StepNo        = Option.ofNullable <| row.``Step No.``
                    LineStatement = row.``Line Statement``
                    Instruction   = row.Instruction
                    Arguments     = arguments
                    //Blank         = row.Blank
                    //PIStatement   = row.``PI Statement``
                }
        }
       // |> Seq.filter (fun line -> not(line.LineStatement.StartsWith("[Title]"))) //test ahn
        |> Seq.groupWhen (fun line -> line.Instruction.any() || line.LineStatement.any())
        |> Seq.map mergeLines

    let ExCMDType        = [|"FOR"; "NEXT"; "BREAK"; "CALL"; "INIT_DONE"; "JMP"; "RET"; "SBRT"; "END"|]  |> HashSet
    let ExCMDTypeHasPara = [|"FOR"; "CALL";  "JMP";  "SBRT"|]  |> HashSet
    let ExCMDTypeSingleLine = [|"FOR"; "NEXT"; "RET"; "SBRT"; "END"|]  |> HashSet
    /// 프로그램 라인들에서 rung 단위로 끊은 것
    let internal extractRungsFromPOU pouCsv (commentDic:CommentDictionary) =
        /// Rung 의 마지막에 올 수 없는 instructions
        let notFinishs = [
                             "LD"    ;"LD="     ;"LDD>="    ;"LD$="     ;"LDD<>"
                            ;"LDI"   ;"AND="    ;"ANDD>="   ;"AND$=>"   ;"ANDD<>"
                            ;"AND"   ;"OR="     ;"ORD>="    ;"OR$="     ;"ORD<>"
                            ;"ANI"   ;"LD<>"    ;"LDE="     ;"LD$<>"    ;"LDD>"
                            ;"OR"    ;"AND<>"   ;"ANDE="    ;"AND$<>"   ;"ANDD>"
                            ;"ORI"   ;"OR<>"    ;"ORE="     ;"OR$<>"    ;"ORD>"
                            ;"LDP"   ;"LD>"     ;"LDE<>"    ;"LD$>"     ;"LDD<="
                            ;"LDF"   ;"AND>"    ;"ANDE<>"   ;"AND$>="   ;"ANDD<="
                            ;"ANDP"  ;"OR>"     ;"ORE<>"    ;"OR$>"     ;"ORD<="
                            ;"ANDF"  ;"LD<="    ;"LDE>"     ;"LD$<="    ;"LDD<"
                            ;"ORP"   ;"AND<="   ;"ANDE>"    ;"AND$<="   ;"ANDD<"
                            ;"ORF"   ;"OR<="    ;"ORE>"     ;"OR$<="    ;"ORD<"
                            ;"ANB"   ;"LD<"     ;"LDE<="    ;"LD$<"
                            ;"ORB"   ;"AND<"    ;"ANDE<="   ;"AND$<"
                            ;"MPS"   ;"OR<"     ;"ORE<="    ;"OR$<"
                            ;"MRD"   ;"LD>="    ;"LDE<"     ;"LD$>="
                            ;"MPP"   ;"AND>="   ;"ANDE<"    ;"AND$>="
                            ;"INV"   ;"OR>="    ;"ORE<"     ;"OR$>="
                            ;"MEP"   ;"LDD="    ;"LDE>="
                            ;"MEF"   ;"ANDD="   ;"ANDE>="
                            ;"EGP"   ;"ORD="    ;"ORE>="
                            ;"EGF"
                            ] |> HashSet

        let rungs =
            let isSplitPosition (s1:ProgramCSVLine) (s2:ProgramCSVLine) =
                ((not (notFinishs.Contains(s1.Instruction))
                && s2.Instruction.Contains("LD"))
                || ExCMDTypeSingleLine.Contains(s2.Instruction)
                )
            let lines = readProgramCSV pouCsv commentDic
            lines
            |> Seq.splitOn isSplitPosition
            |> Seq.map Array.ofSeq
            |> Array.ofSeq

        let name = Path.GetFileNameWithoutExtension(pouCsv)
        {Name = name; Rungs = rungs}

    /// CSVs (MPS VS MPP) 개수를 비교해서 라인 합성
    let internal mergeRungs (pou:POUParseResult) =
        let lstRung = ResizeArray<Rung>()
        let updateRung (rung:Rung) =
            let newRung = lstRung |> Seq.collect (fun f -> f) |> Array.ofSeq
            lstRung.Clear()
            newRung

        let isMerge (rung:Rung) =
            lstRung.Add(rung)
            let mps = lstRung |> Seq.collect (fun ru -> ru |> Seq.filter (fun f -> f.Instruction = "MPS"))|> Array.ofSeq
            let mpp = lstRung |> Seq.collect (fun ru -> ru |> Seq.filter (fun f -> f.Instruction = "MPP"))|> Array.ofSeq
            mps.Length = mpp.Length

        let mergeRungs =
            pou.Rungs
            |> Seq.filter (fun f -> isMerge f)
            |> Seq.map updateRung
            |> Array.ofSeq
        {Name = pou.Name; Rungs = mergeRungs}


    /// CSVs (comment + program) 을 파싱해서, POU 와 comment dic 을 반환
    let parseCSVs csvs =
        /// CSV 가 comment 를 저장한 것인지 판별
        let isCommentCSV file =
            let lines = File.ReadAllLines(file)
            lines |> Array.length > 1 && lines.[1] = "\"Device Name\"\t\"Comment\""

        let commentCSVs, pouCSVs =
            csvs
            |> Array.ofSeq
            |> Array.partition isCommentCSV

        let commentDic = readCommentCSV commentCSVs
        let pous =
            pouCSVs
            |> Seq.map (fun csv ->
                            extractRungsFromPOU csv commentDic
                            |> mergeRungs
                            )
            |> Array.ofSeq

        pous, commentDic

    [<AutoOpen>]
    module Print =
        let ArgumentOfLine (line:ProgramCSVLine) =
            line.Arguments |> Seq.map (fun a -> a.ToText())

        let printArgumentOfLine (line:ProgramCSVLine) =
            ArgumentOfLine line |> String.concat " "

        let printArgumentNCmdOfLine (line:ProgramCSVLine) =
            (ArgumentOfLine line, line.Instruction)


        let printLine (line:ProgramCSVLine) =
            match line.StepNo with
            | Some(stepNo)-> sprintf "%d %s %s" stepNo line.Instruction (printArgumentOfLine line)
            | None -> sprintf "%s %s" line.Instruction (printArgumentOfLine line)

        let printRung (r:Rung) =
            r |> Seq.map printLine |> String.concat "\r\n"

