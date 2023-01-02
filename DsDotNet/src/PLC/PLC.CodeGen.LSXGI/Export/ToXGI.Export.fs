namespace PLC.CodeGen.LSXGI

open System
open System.Linq

open Engine.Common.FS
open PLC.CodeGen.Common.QGraph
open System.Collections.Generic
open Engine.Core
open PLC.CodeGen.Common.K

module LsXGI =

    //<kwak>
    //let generateXGIXmlFromLadderInfo (opt:CodeGenerationOption) (ladderInfo:LadderInfo) (tags) (unusedTags) (existingLSISprj:string option) =
    //    let existTagdict =
    //        existingLSISprj
    //        |> map (DsXml.load >> XGIXml.createUsedVariableMap)
    //        |> Option.defaultValue (new Dictionary<string, string>())

    //    let statements =
    //        ladderInfo.Rungs
    //        |> RungGenerator.replaceDuplicateTags tags existTagdict
    //        |> Seq.groupBy(fun ri -> ri.GetCoilTerminal())
    //        |> Seq.map (rungInfoToStatement opt)

    //    let plctags = statementToTag statements |> Seq.append tags |> Seq.distinct

    //    File.generateXGIXmlFromStatement ladderInfo.PrologComments statements plctags unusedTags existingLSISprj

    //let generateXGIXmlFromLadderInfoAndStatus (opt:CodeGenerationOption) (ladderInfo:LadderInfo) status (tags) (unusedTags) (existingLSISprj:string option) =
    //    let existTagdict =
    //        existingLSISprj
    //        |> map (DsXml.load >> XGIXml.createUsedVariableMap)
    //        |> Option.defaultValue (new Dictionary<string, string>())

    //    let statements = ladderInfo.Rungs @ status |> RungGenerator.replaceDuplicateTags tags existTagdict |> Seq.groupBy(fun ri -> ri.GetCoilTerminal()) |> Seq.map (rungInfoToStatement opt)

    //    let plctags = statementToTag statements |> Seq.append tags |> Seq.distinct
    //    File.generateXGIXmlFromStatement ladderInfo.PrologComments statements plctags unusedTags existingLSISprj


    let generateXml (opt:CodeGenerationOption) (storages:Storages) (commentedStatements:CommentedStatement list) : string =
        match RuntimeTarget with
        | XGI -> ()
        | _ -> failwith $"ERROR: Require XGI Runtime target.  Current runtime target = {RuntimeTarget}"

        let prologComments = ["DS Logic for XGI"]

        // todo : Timer 및 Counter 도 PLC XGI 에 변수로 등록하여야 한다.
        // <Symbol Name="T_myTon" Kind="1" Type="TON" State="0" Address="" Trigger="" InitValue="" Comment="" Device="" DevicePos="-1" TotalSize="0" OrderIndex="-1" HMI="0" EIP="0" SturctureArrayOffset="0" ModuleInfo="" ArrayPointer="0"><MemberAddresses></MemberAddresses>

        let unusedTags:ITagWithAddress list = []
        let existingLSISprj = None

        let timerOrCountersNames =
            storages.Values.Filter(fun s -> s :? TimerCounterBaseStruct)
                .Select(fun struc -> struc.Name)
                |> HashSet
                ;
        let commentedXgiStatements:CommentedXgiStatement list =
            commentedStatements
            |> map commentedStatement2CommentedXgiStatement

        let xgiStatements:XgiStatement list =
            commentedXgiStatements
            |> map (fun (CommentedXgiStatement(cmt, stmt)) -> stmt)

        let extendedXgiStatements =
            xgiStatements
            |> map (fun x -> x.GetStatement())
            |> List.ofType<XgiStatementExptender>

        let newStorages:IStorage list =
            let temporaryTags =
                extendedXgiStatements
                |> Seq.collect(fun xgi -> xgi.TemporaryTags)
                |> Seq.cast<IStorage>
                |> Seq.distinct
            storages.Values @ temporaryTags
            |> List.ofSeq

        let newCommentedStatements: CommentedXgiStatement list  =
            let extendedStatements =
                [   for xgi in extendedXgiStatements do
                    for s in xgi.ExtendedStatements do
                        CommentedXgiStatement("Augmented", s)
                ]

            commentedXgiStatements @ extendedStatements

        noop()
        let xgiSymbols =
            [   for s in newStorages do
                    match s with
                    | :? ITagWithAddress as t ->
                        let name = (t :> INamed).Name
                        if timerOrCountersNames.Contains(name.Split(".")[0]) then
                            // skip timer/counter structure member : timer 나 counter 명 + "." + field name
                            ()
                        else
                            XgiSymbol.DuTag t
                    | :? TimerStruct as ts ->
                        XgiSymbol.DuTimer ts
                    | :? CounterBaseStruct as cs ->
                        XgiSymbol.DuCounter cs
                    | _ -> failwith "ERROR"
            ]

        let xml = generateXGIXmlFromStatement prologComments newCommentedStatements xgiSymbols unusedTags existingLSISprj
        xml
