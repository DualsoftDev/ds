namespace PLC.CodeGen.LSXGI

open System.Linq

open Engine.Common.FS
open PLC.CodeGen.Common.QGraph
open System.Collections.Generic
open Engine.Core

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

        let unusedTags:ITagWithAddress list = []
        let existingLSISprj = None

        let timerOrCountersNames =
            storages.Values.Filter(fun s -> s :? TimerCounterBaseStruct)
                .Select(fun struc -> struc.Name)
                |> HashSet
                ;

        (* Timer 및 Counter 의 Rung In Condition 을 제외한 부수의 조건들이 직접 tag 가 아닌 condition expression 으로
            존재하는 경우, condition 들을 임시 tag 에 assign 하는 rung 으로 분리해서 저장.
            => 새로운 임시 tag 와 새로운 임시 tag 에 저장하기 위한 rung 들이 추가된다.
        *)

        let newCommentedStatements = ResizeArray<CommentedXgiStatements>()
        let newStorages = ResizeArray<IStorage>(storages.Values)
        for cmtSt in commentedStatements do
            let xgiCmtStmts = commentedStatement2CommentedXgiStatements newStorages cmtSt
            let (CommentAndXgiStatements(comment_, xgiStatements)) = xgiCmtStmts
            if xgiStatements.Any() then
                newCommentedStatements.Add xgiCmtStmts

        let xgiSymbols =
            [   for s in newStorages do
                    match s with
                    | :? ITagWithAddress as t ->
                        let name = (t :> INamed).Name
                        if timerOrCountersNames.Contains(name.Split(".")[0]) then
                            // skip timer/counter structure member : timer 나 counter 명 + "." + field name
                            ()
                        else
                            XgiSymbol.DuXsTag t
                    | :? IXgiLocalVar as xgi ->
                        XgiSymbol.DuXsXgiLocalVar xgi
                    | :? TimerStruct as ts ->
                        XgiSymbol.DuXsTimer ts
                    | :? CounterBaseStruct as cs ->
                        XgiSymbol.DuXsCounter cs
                    | _ -> failwith "ERROR"
            ]

        let xml = generateXGIXmlFromStatement prologComments newCommentedStatements xgiSymbols unusedTags existingLSISprj
        xml
