namespace PLC.CodeGen.LSXGI

open System.Linq

open Engine.Common.FS
open System.Collections.Generic
open Engine.Core

module LsXGI =
    let internal storagesToXgiSymbol(storages:IStorage seq) : XgiSymbol list = [
        let timerOrCountersNames =
            storages.Filter(fun s -> s :? TimerCounterBaseStruct)
                .Select(fun struc -> struc.Name)
                |> HashSet
                ;

        for s in storages do
            match s with
            | :? ITagWithAddress as t ->
                let name = (t :> INamed).Name
                if timerOrCountersNames.Contains(name.Split(".")[0]) then
                    // skip timer/counter structure member : timer 나 counter 명 + "." + field name
                    ()
                else
                    XgiSymbol.DuTag t
            | :? IXgiLocalVar as xgi ->
                XgiSymbol.DuXgiLocalVar xgi
            | :? TimerStruct as ts ->
                XgiSymbol.DuTimer ts
            | :? CounterBaseStruct as cs ->
                XgiSymbol.DuCounter cs
            | _ -> failwithlog "ERROR"
    ]

    let internal xgiSymbolsToXgiSymbolInfos (xgiSymbols:XgiSymbol seq) : SymbolInfo list = [
    ]


    let generateXml (storages:Storages) (commentedStatements:CommentedStatement list) : string =
        match Runtime.Target with
        | XGI -> ()
        | _ -> failwith $"ERROR: Require XGI Runtime target.  Current runtime target = {Runtime.Target}"

        let prologComments = ["DS Logic for XGI"]

        let existingLSISprj = None

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

        let xgiSymbols = storagesToXgiSymbol newStorages

        let xml = generateXgiXmlFromStatement prologComments newCommentedStatements xgiSymbols existingLSISprj
        xml
