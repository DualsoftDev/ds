namespace Dual.Core

open FSharpPlus
open Dual.Core
open Dual.Core.Types
open Dual.Common
open Dual.Core.QGraph
open Dual.Core.ModelPostProcessor
open Dual.Core.QGraph.RelayMarkerPath
open System.Collections.Generic

[<AutoOpen>]
module RungGenerator =
    /// 최종 결과물의 expression 에서 relay 이름을 주어진 spec 에 맞게 변경.
    let replaceRelayNames (exprs: (string * Expression)seq) (replaces:(string * string) seq) =
        let dic = replaces |> Tuple.toDictionary

        let prefix = "_tmp_"
        let rec replaceExpression (expr: Expression) encode =
            let doEncode exp = replaceExpression(exp) encode
            match expr with
            | Terminal(t) ->    // of IExpressionTerminal
                //assert(t.GetType() = typeof<Coil>)
                let tag = t.ToText()
                if encode then
                    if dic.ContainsKey(tag) then
                        let newTag = prefix + dic.[tag]
                        Terminal(Prelude.Coil(newTag))
                    else
                        expr
                else
                    if tag.StartsWith(prefix) then
                        Terminal(Prelude.Coil(tag.Replace(prefix, "")))
                    else
                        expr
            | Binary(l, op, r) ->  //    of Expression * Op * Expression
                Binary(doEncode(l), op, doEncode(r))
            | Unary(op, exp) ->    //    of Op * Expression
                Unary(op, doEncode(exp))
            | Zero ->
                expr
        let replaceCoil tag encode =
            if encode then
                if dic.ContainsKey(tag) then
                    prefix + dic.[tag]
                else
                    tag
            else
                if tag.StartsWith(prefix) then
                    tag.Replace(prefix, "")
                else
                    tag

        let tmps = [
            for y, x in exprs do
                yield replaceCoil y true, replaceExpression x true
        ]

        let replaced = [
            for y, x in tmps do
                yield replaceCoil y false, replaceExpression x false
        ] 

        replaced

    /// rungs과 userTags의 Tag중 existXmlTagDict에 이름이 같으나 주소가 다른 Tag가 있나 확인한다.
    let validateDupliacateSymbolName (usedTags:PLCTag seq) (existXmlTagDict:Dictionary<string, string>) (rungs:seq<RungInfo>) =
        // 기존 xml tags
        let existTags = 
            existXmlTagDict 
            |> Seq.map(fun kv -> 
                let name = kv.Key
                let addr = kv.Value

                PLCTag(name,  None, AddressM.tryParse(addr))
            )
            |> Seq.where(fun t -> t.Address.IsSome)
            |> List.ofSeq
        // DS 모델링 tags
        let dsTags = 
            let e = rungs |> map(rungInfoToExpr)
            let c = rungs |> map(fun ri -> ri.GetCoilTerminal() |> mkTerminal)
            let usedTerminals = usedTags |> Seq.cast<IExpressionTerminal>
            e @@ c
            |> Seq.collect(collectTerminals) 
            |> Seq.distinct
            |> Seq.append usedTerminals
            |> Seq.where(fun t -> match t with | :? PLCTag as tag when tag.Address.IsSome -> true | _ -> false)
            |> Seq.cast<PLCTag>
            |> List.ofSeq

        // 이름은 같으나 주소가 다른것을 찾는다.
        let duplicate = dsTags |> Seq.where(fun dt -> existTags |> Seq.exists(fun et -> et.Name = dt.Name && et.Address <> dt.Address))

        if duplicate.isEmpty() |> not then
            duplicate |> Seq.iter(fun t -> logWarn "%s와 같은 이름의 다른 주소를 사용하는 Symbol이 기존 xml에 존재합니다." t.Name) |> ignore
            failwith "다른 주소를 사용하는 같은 이름의 Symbol이 존재합니다."
        else 
            rungs

    /// 기존 xml과 사용자 정의 Tag, 모델링 자동생성 Address가 정의된 Tag를 수집한다.
    /// existXmlTag > usedTag > RungTag 순으로 정렬됨
    /// 우선 순위로 정렬되기떄문에 따로 sort하면 안된다.
    let getAllOrderedTerminal (usedTags:PLCTag seq) (existXmlTagDict:Dictionary<string, string>) (rungs:seq<RungInfo>) =
        let e = rungs |> map(rungInfoToExpr)
        let c = rungs |> map(fun ri -> ri.GetCoilTerminal() |> mkTerminal)
        let usedTerminals = usedTags |> Seq.cast<IExpressionTerminal>
        let existTerminals = 
            existXmlTagDict 
            |> Seq.map(fun kv -> 
                let name = kv.Key
                let addr = kv.Value

                PLCTag(name,  None, AddressM.tryParse(addr))
            )
            |> Seq.cast<IExpressionTerminal>

        e @@ c
        |> Seq.collect(collectTerminals) 
        |> Seq.distinct
        //|> Seq.sortBy(fun t -> t.ToText())
        |> Seq.append usedTerminals
        |> Seq.append existTerminals

    /// 주소가 중복되는 PLCTag들 수집
    let findDupliacteTagsByAddress (usedTags:PLCTag seq) (existXmlTagDict:Dictionary<string, string>) (rungs:seq<RungInfo>) = 
        getAllOrderedTerminal usedTags existXmlTagDict rungs
        // PLCTag중 주소가 있는 Tag만 수집
        |> Seq.filter(fun t -> match t with | :? PLCTag as tag when tag.Address.IsSome -> true | _ -> false) 
        |> Seq.cast<PLCTag> 
        |> Seq.groupBy(fun tag -> tag.Address) 
        // 같은 주소가 1개를 초과하는 것만 수집
        |> Seq.filter(fun (k, v) -> v.length() > 1)

    /// 이름 중복되는 Terminal을 수집
    let findDupliacteTagsByName (usedTags:PLCTag seq) (existXmlTagDict:Dictionary<string, string>) (rungs:seq<RungInfo>) = 
        getAllOrderedTerminal usedTags existXmlTagDict rungs
        // PLCTag는 이름과주소가 같은것 그외는 ToText가 같은것을 distinct 처리
        |> Seq.distinctBy(fun t ->
            match t with
            | :? PLCTag as pt -> pt.Name + (pt.Address |> Option.bind(fun addr -> addr.GetAddress() |> Some) |> Option.defaultValue "") /// 이름 + Address string (default "")
            | _ -> t.ToText()
        )
        |> Seq.groupBy(fun tag -> tag.ToText()) 
        // 같은 Tag가 1개를 초과하는 것만 수집
        |> Seq.filter(fun (k, v) -> v.length() > 1)

    // expression의 모든 중복된 태그를 변환
    let rec replaceRungExpression (on:seq<IExpressionTerminal * IExpressionTerminal>) expr =
        if on |> Seq.isEmpty |> not then
            let oldValue, newValue = on |> Seq.head
            let nExpr = expr |> replace oldValue newValue
            
            replaceRungExpression (on |> Seq.where(fun (o, n) -> o <> oldValue)) nExpr
        else
            expr

    /// 중복 검사 함수를 이용하여 Rung의 요소가 중복된지 판별하고
    /// Rung의 Expression들을 Replace 함수를 사용하여 중복된 요소를 replace한다.
    let replaceDuplicateRung isDuplicateFunc (replaceFunc:Expression -> Expression) ri =
        // rung이 중복된 Tag를 사용하면
        match isDuplicateFunc ri with
        | true ->
            let coil =
                match ri.CoilOrigin with
                | Function(func) ->  
                    let endtag = func.TerminalEndTag |> mkTerminal |> replaceFunc |> collectTerminals |> Seq.head
                    CommandTypesM.replaceEndTag endtag func |> CoilOriginTypeExt.fromPLCFunction
                | Relay(r) -> Prelude.Coil(r.Name) |> mkTerminal |> replaceFunc |> CoilOriginTypeExt.toCoilOrigin
                | Coil(vs) ->  
                    match vs.Terminal with  
                    | Terminal(t) -> t |> mkTerminal |> replaceFunc |> CoilOriginTypeExt.toCoilOrigin
                    |_-> failwith "Not support Expression coil"
                | NotYetDefined -> failwith "Not yet defined"
                
            {
                defaultExpressionInfo with
                    Start      = ri.Start   |> replaceFunc
                    Set        = ri.Set     |> replaceFunc
                    Manual     = ri.Manual  |> replaceFunc
                    Interlock  = ri.Interlock |> replaceFunc
                    Reset      = ri.Reset |> replaceFunc
                    CoilOrigin = coil
                    Selfhold   = ri.Selfhold |> replaceFunc
                    Comments   = ri.Comments
            }
        | false -> ri

    /// rungs의 Tag중 userTags와 이름이 같은 Tag들의 주소를 userTags의 주소로 바꾼다.
    let applyUserDefindTags (usedTags:PLCTag seq) (rungs:seq<RungInfo>) =
        let usedTerminals = usedTags |> Seq.cast<IExpressionTerminal>

        // userTags와 이름 중복된 Tag를 사용하는 Rung인가 확인
        let isDuplicateRung ri = 
            let expr = rungInfoToExpr ri
            let c = ri.GetCoilTerminal()
            collectTerminals expr @@ [c]
            |> Seq.exists(fun t -> usedTerminals |>  Seq.exists(fun ut -> t.ToText() = ut.ToText()))

        // 이름이 중복된 Tag에 해당되는 expressionTerminal들을 userTag로 바꿔줌
        let replaceOldNew expr = 
            // 중복된 태그를 찾고 대체할 태그와 튜플로 만듬
            let tuOldNew =
                usedTerminals 
                |> Seq.collect(fun ut -> 
                    collectTerminals expr 
                    |> Seq.where(fun t -> t.ToText() = ut.ToText()) 
                    |> Seq.map(fun o -> o, ut)
                    )
            replaceRungExpression tuOldNew expr

        rungs 
        |> List.ofSeq
        |> List.map(replaceDuplicateRung isDuplicateRung replaceOldNew)

    /// rung에서 중복 사용되는 주소를 가진 plctag들을 변경
    /// existXmlTag > usedTag > RungTag 순으로 우선권이 있음
    /// usedTags : 해당 이름과 주소로 사용하기로 하는 PLCTag 
    /// existXmlTagDict : xml에서 이미 사용되고있는 tag의 name, address dictionary
    let replaceDuplicateTagsByAddress usedTags existXmlTagDict rungs = 
        // 같은 주소를 사용하는 PLCTag Group
        // Key : Address Option // Value : PLCTag
        let dupliacteTags = findDupliacteTagsByAddress usedTags (existXmlTagDict:Dictionary<string, string>) rungs |> List.ofSeq

        // 주소가 중복된 Tag를 사용하는 Rung인가 확인
        let isDuplicateRung ri = 
            let expr = rungInfoToExpr ri
            let c = ri.GetCoilTerminal()
            collectTerminals expr @@ [c]
            |> Seq.filter(fun t -> match t with | :? PLCTag as tag when tag.Address.IsSome -> true | _ -> false)  
            |> Seq.cast<PLCTag> 
            |> Seq.exists(fun t -> dupliacteTags |>  Seq.collect(snd) |> Seq.contains(t))

        // Address가 중복된 Tag 그룹에 해당되는 expressionTerminal들을 해당 그룹의 첫 PLCTag로 변경
        let replaceOldNew expr = 
            // 중복된 태그를 찾고 대체할 태그와 튜플로 만듬
            let tuOldNew =
                dupliacteTags 
                |> Seq.collect(fun (_, ts) -> 
                    let n = ts |> Seq.head :> IExpressionTerminal
                    let os = ts |> Seq.where(fun t -> collectTerminals expr |> Seq.contains(t :> IExpressionTerminal)) |> Seq.cast<IExpressionTerminal>
                    os |> Seq.map(fun o -> o, n)
                    )
            replaceRungExpression tuOldNew expr

        rungs 
        |> List.ofSeq
        |> List.map(replaceDuplicateRung isDuplicateRung replaceOldNew)

    /// rung에서 중복 사용되는 이름를 가진 Terminal들을 변경
    /// existXmlTag > usedTag > RungTag 순으로 우선권이 있음
    /// usedTags : 해당 이름과 주소로 사용하기로 하는 PLCTag 
    /// existXmlTagDict : xml에서 이미 사용되고있는 tag의 name, address dictionary
    let replaceDuplicateTagsByName usedTags existXmlTagDict rungs =
        // 같은 이름을 사용하는 Terminal Group
        // Key : string // Value : IExpressionTerminal
        let dupliacteTags = findDupliacteTagsByName usedTags (existXmlTagDict:Dictionary<string, string>) rungs |> List.ofSeq

        // PLCTag는 이름+주소 나머지는 이름으로 중복확인
        let compareTerminalNameAndAddress (t1:IExpressionTerminal) (t2:IExpressionTerminal) =
            match t1, t2 with
            | (:? PLCTag as pt), (:? PLCTag as dpt) -> (pt.Name = dpt.Name) && (pt.Address = dpt.Address)
            | :? PLCTag, _ -> false
            | _, :? PLCTag -> false
            | _, _ -> t1.ToText() = t2.ToText()
        
        // 이름이 중복된 Terminal을 사용하는 Rung인가 확인
        let isDuplicateRung ri = 
            let expr = rungInfoToExpr ri
            let c = ri.GetCoilTerminal()
            collectTerminals expr @@ [c]
            |> Seq.exists(fun t -> 
                dupliacteTags 
                |> Seq.collect(snd >> Seq.skip(1)) 
                |> Seq.exists(fun dt -> compareTerminalNameAndAddress t dt))

        // 이름이 중복된 Tag 그룹에 해당되는 expressionTerminal들을 first를 제외하고 나머지는 이름을 변경
        let replaceOldNew expr = 
            // expression에서 중복된 태그를 찾고 대체할 태그와 튜플로 만듬
            let tuOldNew =
                dupliacteTags 
                |> Seq.collect(fun (_, ts) -> 
                    let os = ts |> Seq.skip(1) |> Seq.mapi(fun i dt -> i, dt) |> Seq.where(fun (i, dt) -> collectTerminals expr |> Seq.exists(fun t -> compareTerminalNameAndAddress t dt) ) 
                    os 
                    |> Seq.map(fun (i, o) -> 
                        let nt = 
                            match o with 
                            | :? PLCTag as pt -> PLCTag(sprintf "%s_Copy%d" (o.ToText()) i, pt.IOType, pt.Address) :> IExpressionTerminal
                            | _ -> sprintf "%s_Copy%d" (o.ToText()) i |> Prelude.Coil :> IExpressionTerminal
                            
                        o, nt)
                    )

            replaceRungExpression tuOldNew expr
        
        rungs 
        |> List.ofSeq
        |> List.map(replaceDuplicateRung isDuplicateRung replaceOldNew)

    let replaceDuplicateTags usedTags existXmlTagDict rungs =
        rungs 
        |> applyUserDefindTags usedTags
        |> validateDupliacateSymbolName usedTags existXmlTagDict
        |> replaceDuplicateTagsByAddress usedTags existXmlTagDict

    let processModelWithOption opt replaces model =
        let procinfos = processModel model opt
        let ladderInfo = procinfos.LadderInfo
        let exprs = ladderInfo.Rungs |> Seq.map(fun ri -> (ri.GetCoilName(), (rungInfoToExpr ri)))
        let replaced = replaceRelayNames exprs replaces
        procinfos, replaced

    let processSystem (rootVertices:seq<IVertex>) (edges:seq<IEdge>) (segments:ISegment seq) (opt:CodeGenerationOption) = 
            /// all vertices 
            let vs = rootVertices |> Seq.distinct |> Seq.collect(getAllVertices2) |> List.ofSeq
            /// internal v all 
            let internalv = 
                vs 
                |> Seq.where(fun v -> 
                    let st = v |> IVertexExt.GetSegmentType segments 
                    st = SegmentType.Internal
                    )
                |> List.ofSeq
            /// models
            let models = internalv |> Seq.collect(fun v -> v.generateVertexToModel()) 
            /// edge 없이 vertex로만 이루어진 하위 모델
            let onlyVerticesModels = 
                internalv 
                |> Seq.collect(fun v -> v.DAGs |> Seq.map(fun d -> v, d) )
                |> Seq.where(fun (p, dag) -> dag.Edges.isEmpty() && dag.Vertices.isEmpty() |> not)

            let externalDoubleActionDevice = 
                vs 
                |> Seq.where(fun v -> (v |> IVertexExt.GetSegmentType segments) = SegmentType.External)
                |> Seq.where(fun target -> 
                    monad{
                        let! targetResetVertex = target.ResetPort.ConnectedVertices |> Seq.tryHead
                        let! sourceResetVertex = targetResetVertex.ResetPort.ConnectedVertices |> Seq.tryHead
                        
                        target.ResetPort.ConnectedVertices.length() = 1
                        && targetResetVertex.ResetPort.ConnectedVertices.length() = 1
                        && target = sourceResetVertex
                        && target <> targetResetVertex
                    }
                    |> Option.defaultValue false
                    )
                |> Seq.map(fun target -> [target; target.ResetPort.ConnectedVertices |> Seq.head] |> Seq.sortBy(fun v -> v.ToText()))
                |> Seq.distinctBy(fun vs -> vs |> Seq.head)
                |> Seq.collect(Seq.pairwise)
                |> List.ofSeq

            /// ---------- preprocess vertex ---------
            PreProcessor.PreProcessVertex opt segments vs |> ignore

            /// 단독 inner 버텍스일떄 InitialStatus가 안정해졌으면 사용할수없음
            onlyVerticesModels 
            |> Seq.iter(fun (p, dag) -> 
                dag.Vertices 
                |> Seq.iter(fun v -> 
                    if v.isSelfReset() then v.InitialStatus <- VertexStatus.Ready
                    else if v.InitialStatus <> VertexStatus.Undefined then ()
                    else failwithlogf "self reset이 아닌 vertex가 Vertex안에 단독 사용되었습니다."
                ))

            /// condition 적용
            vs |> Seq.iter(fun v -> if v :? ISelect then applySelectCondition (v :?> ISelect) |> ignore)

            /// ---------- model ------------------
            let modelInfos =
                models |> Seq.map(fun model ->
                    let procInfos, exprs =
                        PreProcessor.PreProcessModel model 
                        |> processModelWithOption  (Some opt) []

                    procInfos
                )
                |> List.ofSeq

            /// ---------- plc ------------------
            let ladderInfo = 
                let rungs = modelInfos |> Seq.collect(fun (p) -> p.LadderInfo.Rungs) |> List.ofSeq
                let comments = modelInfos |> Seq.collect(fun (p) -> p.LadderInfo.PrologComments) |> List.ofSeq
                { Rungs = rungs; PrologComments = comments}

            let ladderRungs =
                ladderInfo.Rungs
                |> toList
            let status =
                /// ---------- port func --------------
                let functionRungs = 
                    vs
                    |> Seq.collect(generatePortFunctionRungInfo modelInfos opt) 
                    |> List.ofSeq
                /// ----------- reset lock ------------
                let resetlockinfos = internalv |> Seq.collect(generateResetLockRelayRungInfo opt internalv) |> List.ofSeq
                let resetlockRungs = resetlockinfos |> Seq.map(snd) |> Seq.map(snd)
                /// ---------- async ------------------
                let asyncInfos = generateAsyncRungInfo opt modelInfos segments edges |> List.ofSeq
                let asyncRungs = asyncInfos |> Seq.map(snd) |> Seq.flatten
                let asyncrelays = asyncInfos |> List.map(fst) |> Seq.flatten |> List.ofSeq
                /// ---------- status -------------
                let statusRungs = internalv |> Seq.collect(generateModelStatus modelInfos opt resetlockinfos asyncrelays)
                /// edge 관계가 존재하지 않고 단일 vertex만 존재하는 경우
                let nonDagInnerVertexInfos = 
                    onlyVerticesModels
                    |> Seq.collect(fun (parent, dag) -> 
                        dag.Vertices 
                        |> Seq.map(fun child ->
                            let coil = child.StartPort.GetCoil() |> Seq.head |> mkTerminal
                            let set = parent.GoingPort.GetTerminal() <&&> child.StartPort.ConditionExpression
                            let self = 
                                if child.UseSelfHold then coil else Expression.Zero
                            let interlock =
                                [
                                    if child.UseOutputInterlock then
                                        yield child.ResetPort.GetTerminal() |> mkNeg // 출력 interlock

                                    if child.UseOutputResetByWorkFinish then
                                        yield generateOutputResetByFinish opt child   // 출력 완료 비접

                                    yield child.StartPort.InterlockExpression
                                ]
                                |> List.tryReduce mkAnd
                                |> Option.defaultValue Expression.Zero

                            { defaultExpressionInfo with Set = set; Selfhold = self; Interlock = interlock; CoilOrigin = coil.toCoilOrigin(); Comments = lempty }
                        )
                    )

                /// reset tag
                let resetRungs = 
                    vs |> Seq.map(fun v ->
                        let wbs = modelInfos |> List.map(fun inf -> inf.Workbook)
                        let reset = generateOutputInterlocks opt wbs v 
                        let resetport = v.ResetPort.GetCoil() |> Seq.head
                        {
                            defaultExpressionInfo with
                                Set        = reset
                                CoilOrigin = resetport |> mkTerminal |> RelayMarkerPath.CoilOriginTypeExt.toCoilOrigin
                        }
                    )

                /// system layer에서 internal인데 외부 신호와 edge에 의해서 시작하지않고 expression에 의해 동작하는경우
                let firstVertexInfo =
                    rootVertices
                    |> Seq.where(fun v ->
                        let segType = v |> IVertexExt.GetSegmentType segments 
                        segType = SegmentType.Internal && v.StartPort.ConnectedPorts.isEmpty()
                    )
                    |> Seq.map(fun v ->
                        let set = v.StartPort.ConditionExpression
                        let coil = v.StartPort.GetCoil() |> Seq.head |> mkTerminal
                        let self = 
                            if v.UseSelfHold then coil else Expression.Zero
                        let interlock =
                            [
                                if v.UseOutputInterlock then
                                    yield v.ResetPort.GetTerminal() |> mkNeg // 출력 interlock

                                if v.UseOutputResetByWorkFinish then
                                    yield generateOutputResetByFinish opt v   // 출력 완료 비접

                                yield v.StartPort.InterlockExpression
                            ]
                            |> List.tryReduce mkAnd
                            |> Option.defaultValue Expression.Zero

                        { defaultExpressionInfo with Set = set; Selfhold = self; Interlock = interlock; CoilOrigin = coil.toCoilOrigin(); Comments = lempty }
                    )

                /// 단 Rung
                let endtags =
                    vs
                    |> Seq.map(fun v ->
                        match v.SensorPort.GetCoil().length() > 1 with
                        | true ->
                            monad{
                                let set = 
                                    v.SensorPort.PLCTags 
                                    |> Seq.map(fun t ->
                                        match t with 
                                        | :? NegPLCTag as nt -> nt |> mkTerminal |> mkNeg
                                        | _ -> t |> mkTerminal) 
                                    |> Seq.reduce mkAnd
                                let! endtag = v.SensorPort.EndTag 
                                let coil = endtag |> mkTerminal
                                { defaultExpressionInfo with Set = set; CoilOrigin = coil.toCoilOrigin(); Comments = lempty }
                            }
                            
                        | false ->
                            None
                    )
                    |> Seq.choose id

                let deviceErrorInfos =
                    externalDoubleActionDevice
                    |> List.map(fun (v1,v2) ->
                        let v1Sol = v1.StartPort.GetTerminal()
                        let v2Sol = v2.StartPort.GetTerminal()
                        let v1Ls = v1.SensorPort.GetTag() |> map(mkTerminal) |> Seq.reduce mkAnd
                        let v1LsNeg = v1.SensorPort.GetTag() |> map(mkTerminal >> mkNeg) |> Seq.reduce mkAnd
                        let v2LsNeg = v2.SensorPort.GetTag() |> map(mkTerminal >> mkNeg) |> Seq.reduce mkAnd
                        let v1Memo = sprintf "%s_Memo" v1.Name |> toCoil
                        let coil = sprintf "%s_%s_ERR" (v1.ToText()) (v2.ToText()) |> Prelude.Coil

                        let memoRung =
                            let set =
                                [
                                    v1Sol
                                    v1Ls (* <&&> RST_PB*)
                                ]
                                |> List.reduce mkOr
                            let reset = v2Sol <||> (v2.SensorPort.GetCoil() |> map(mkTerminal) |> Seq.reduce mkAnd)
                            { defaultExpressionInfo with Set = set; Reset = reset; CoilOrigin = v1Memo.toCoilOrigin(); Comments = lempty }
                        let advRung =
                            let set =
                                [
                                    v1LsNeg <&&> v2LsNeg
                                    v1Sol <&&> v1Memo <&&> v2LsNeg
                                    v2Sol <&&> (v1Memo |> mkNeg) <&&> v2LsNeg
                                ]
                                |> List.reduce mkOr
                            
                            let func = FunctionBlock.TimerMode(coil, 5000) :> IFunctionCommand
                            { defaultExpressionInfo with Set = set; CoilOrigin = func.fromPLCFunction(); Comments = lempty }

                        coil,
                        [
                            memoRung
                            advRung
                        ]
                    )
                let deviceErrorRungs = deviceErrorInfos |> Seq.collect(snd)
                let totalErrorRungs = 
                    let coil = PLCTag("TOTAL_ERROR", Some TagType.Dummy, "%MW1510.0") |> mkTerminal
                    let self = mkAnd coil (PLCTag("Reset",Some TagType.Dummy, "%MX10") |> mkTerminal |> mkNeg)
                    let set = seq{self} |> Seq.append (deviceErrorInfos |> Seq.map(fst >> mkTerminal)) |> Seq.reduce mkOr
                    [{ defaultExpressionInfo with Set = set; CoilOrigin = coil.toCoilOrigin(); Comments = lempty }]

                nonDagInnerVertexInfos 
                @@ firstVertexInfo 
                @@ asyncRungs 
                @@ statusRungs 
                @@ endtags 
                @@ resetRungs 
                @@ functionRungs 
                @@ resetlockRungs 
                @@ deviceErrorRungs 
                @@ totalErrorRungs
                |> toList

            // 임시 안전용
            let tempRungs = 
                (ladderRungs@status) |> List.ofSeq
                |> List.map(fun ri -> 
                    //ri.Interlock <- ri.Interlock |> mkAnd (PLCTag("Reset",Some TagType.Dummy, "%MW81010.1") |> mkTerminal)
                    {
                        defaultExpressionInfo with
                            Start      = ri.Start  
                            Set        = ri.Set    
                            Reset      = ri.Reset
                            Manual     = ri.Manual  
                            Interlock  = ri.Interlock |> mkAnd (PLCTag("Reset",Some TagType.Dummy, "%MX10") |> mkTerminal |> mkNeg)
                            CoilOrigin = ri.CoilOrigin
                            Selfhold   = ri.Selfhold
                            Comments   = ri.Comments
                    }
                )

            { Rungs = tempRungs; PrologComments = ladderInfo.PrologComments}


