namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open Dual.Common.Base.FS
open System.Linq
open System

[<AutoOpen>]
module ConvertCheckModule =

    let IsSpec (v:Vertex, vaild:ConvertType, alias:ConvertAlias)=
            let aliasSpec    = alias = AliasTure  || alias = AliasNotCare
            let aliasNoSpec  = alias = AliasFalse || alias = AliasNotCare
            let isValidVertex =
                match v with
                | :? Real            -> aliasNoSpec && vaild.HasFlag(RealInFlow)
                | :? Call as c  ->
                    match c.Parent with
                    | DuParentFlow _ -> aliasNoSpec && vaild.HasFlag(CallInFlow)
                    | DuParentReal _ -> aliasNoSpec && vaild.HasFlag(CallInReal)

                | :? Alias as a  ->
                     match a.Parent with
                     | DuParentFlow _ ->
                         match a.TargetWrapper with
                         |  DuAliasTargetReal _         -> aliasSpec && vaild.HasFlag(RealInFlow)
                         |  DuAliasTargetCall _         -> aliasSpec && vaild.HasFlag(CallInFlow)
                     | DuParentReal _ ->
                         match a.TargetWrapper with
                         | DuAliasTargetReal _         -> failwithlog $"Error {getFuncName()}"
                         | DuAliasTargetCall _         -> aliasSpec &&  vaild.HasFlag(CallInReal)
                |_ -> failwithlog $"Error {getFuncName()}"

            isValidVertex

    let private checkErrHWItem(sys:DsSystem)  (skipBtnLamp:bool) =
        let hwManuFlows = sys.ManualHWButtons |> Seq.collect(fun f -> f.SettingFlows)
        let hwAutoFlows = sys.AutoHWButtons   |> Seq.collect(fun f -> f.SettingFlows)
     
        for action in sys.HWActions do
            if action.OutAddress.IsOneOf(TextAddrEmpty, TextNotUsed) then
                failwithf $"HW Lamp : {action.Name} OutAddress 값이 없습니다."

        if not skipBtnLamp 
            then 
            for btn in sys.AutoHWButtons do
                for flow in btn.SettingFlows do
                    if not(hwManuFlows.Contains flow) then
                        failwithf $"{flow.Name} manual btn not exist"

            for btn in sys.ManualHWButtons do
                for flow in btn.SettingFlows do
                    if not(hwAutoFlows.Contains flow) then
                        failwithf $"{flow.Name} auto btn not exist"

            for btn in sys.HWButtons do
                if btn.InAddress.IsOneOf(TextAddrEmpty, TextNotUsed) then
                    failwithf $"HW Button : {btn.Name} InAddress 값이 없습니다."

            for lamp in sys.HWLamps do
                if lamp.OutAddress.IsOneOf(TextAddrEmpty, TextNotUsed) then
                    failwithf $"HW Lamp : {lamp.Name} OutAddress 값이 없습니다."

            for condi in sys.HWConditions do
                if condi.InAddress.IsOneOf(TextAddrEmpty, TextNotUsed) then
                    failwithf $"HW Button : {condi.Name} InAddress 값이 없습니다."


    let internal checkErrApi(sys:DsSystem) =
        for coin in sys.GetVerticesOfJobCalls() do
            for td in coin.TaskDefs do
                let api = td.ApiItem
                if api.RX.IsNull() then
                    failwithf $"interface 정의시 관찰 Work가 없습니다. \n(error: {api.Name})"
                if api.TX.IsNull() then
                    failwithf $"interface 정의시 지시 Work가 없습니다. \n(error: {api.Name})"

                if td.OutAddress <> TextNotUsed && coin.CallActionType =CallActionType.Push then
                    if coin.MutualResetCoins.IsEmpty() then
                        failwithf $"Push type must be an interlock device \n(error: {coin.Name})"



    // unused
    let private checkErrExternalStartRealExist (sys:DsSystem) =
        let flowEdges = (sys.Flows |> Seq.collect(fun f -> f.Graph.Edges))
        let exEdges =
            flowEdges
                .Where(fun e ->
                    e.Source.TryGetPureCall().IsSome
                        || e.Target.TryGetPureCall().IsSome)

        if not(exEdges.Any()) then
            failwithf $"PLC 시스템은 외부시작 신호가 없으면 시작 불가 입니다. HelloDS 모델을 참고하세요"


    let internal checkMultiDevPair(sys: DsSystem) =
        let devicesCalls =
            sys.GetTaskDevsCall()
                .DistinctBy(fun (td, c) -> (td, c.TargetJob))
                .Where(fun (_, call) -> call.TargetJob.TaskDevCount > 1)

        let groupDev = devicesCalls  |> Seq.groupBy (fun (dev, _) -> dev.DeviceName)

        for (_, calls) in groupDev  do
            let jobMultis =
                calls
                |> Seq.map (fun (_, call) -> call.TargetJob.TaskDevCount )
                |> Seq.distinct
            if Seq.length jobMultis > 1 then
                let callTexts = String.Join("\r\n", calls.Select(fun (_, call) -> call.Name))
                failwithf $"동일 다비이스의 multi 수량은 같아야 합니다. \r\n{callTexts}"

    let GetRealsWithError (sys:DsSystem) (bStart:bool) : Real array =
        let vs = sys.GetVertices()
        let reals = vs.OfType<Real>()
        [|
            let vsAliasReals = vs.GetAliasTypeReals().ToArray()
            for real in reals do
                let realAlias_ = vsAliasReals.Where(fun f -> f.GetPure() = real).OfType<Vertex>()
                let checkList = ( [real:>Vertex] @ realAlias_ )

                let checks =
                    let f = if bStart then getStartEdgeSources else getResetEdgeSources
                    checkList |> Seq.collect(f)

                if checks.IsEmpty() then
                    yield real
        |]

    let CheckRealReset (sys:DsSystem) =
        let errors = GetRealsWithError sys false
        if errors.Any() then
            let fullErrorMessage = String.Join("\n", errors.Select(fun e-> $"{e.Parent.GetFlow().Name}.{e.Name}"))
            failwithf $"Work는 Reset 연결이 반드시 필요합니다. \n\n{fullErrorMessage}"

    let checkNullAddress (sys: DsSystem) (skipBtnLamp:bool) =
        checkErrHWItem sys skipBtnLamp
        
        // Check for null addresses in jobs
        let nullTagJobs =
            sys.Jobs.SelectMany(fun j -> j.GetNullAddressDevTask()) |> toArray

        if nullTagJobs.Any() then
            let errJobs = String.Join ("\n", nullTagJobs.Select(fun s->s.QualifiedName))
            failwithf $"Device 주소가 없습니다. \n{errJobs} \n\nUtils > 주소 할당 수행하세요"


        if not skipBtnLamp
        then
            // Check for null buttons
            let nullBtns =
                sys.HWButtons
                |> filter (fun b -> b.InTag.IsNull() || (b.OutTag.IsNull() && b.OutAddress <> TextNotUsed))
                |> toArray

            if nullBtns.Any() then
                let errBtns =
                    nullBtns
                    |> map (fun b ->
                        let inout = if b.InTag.IsNull() then "입력" else "출력"
                        $"{b.ButtonType} 해당 {inout} 주소가 없습니다. [{b.Name}]")
                    |> String.concat "\n"

                failwithf $"버튼 주소가 없습니다. \n{errBtns}"

            // Check for null lamps
            let nullLamps = sys.HWLamps |> filter (fun l -> l.OutTag.IsNull()) |> toArray
            if nullLamps.Any() then
                let errLamps = nullLamps |> map (fun l -> l.Name) |> String.concat "\n"
                failwithf $"램프 주소가 없습니다. \n{errLamps}"

    let internal checkJobs(sys:DsSystem) =
        for call in sys.GetCallVertices() do
            if call.CallActionType = CallActionType.Push && call.ValueParamIO.Out.DataType <> DuBOOL then
                    failWithLog $"{call.Name} {call.CallActionType} 은 bool 타입만 지원합니다."
