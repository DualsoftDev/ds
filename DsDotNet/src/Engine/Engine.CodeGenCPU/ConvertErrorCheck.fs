namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System.Linq
open System

[<AutoOpen>]
module ConvertErrorCheck =
  
    let internal checkErrHWItem(sys:DsSystem) = 
        let hwManuFlows = sys.ManualHWButtons |> Seq.collect(fun f -> f.SettingFlows)
        let hwAutoFlows = sys.AutoHWButtons   |> Seq.collect(fun f -> f.SettingFlows)
        for btn in sys.AutoHWButtons do
            for flow in btn.SettingFlows do
                if not(hwManuFlows.Contains flow) then
                    failwithf $"{flow.Name} manual btn not exist"

        for btn in sys.ManualHWButtons do
            for flow in btn.SettingFlows do
                if not(hwAutoFlows.Contains flow) then
                    failwithf $"{flow.Name} auto btn not exist"

        if RuntimeDS.Package.IsPLCorPLCSIM() then
            for btn in sys.HWButtons do
                if btn.InAddress.IsOneOf(TextAddrEmpty, TextSkip) then
                    failwithf $"HW Button : {btn.Name} InAddress 값이 없습니다."

            for lamp in sys.HWLamps do
                if lamp.OutAddress.IsOneOf(TextAddrEmpty, TextSkip) then
                    failwithf $"HW Lamp : {lamp.Name} OutAddress 값이 없습니다."


    let internal checkErrApi(sys:DsSystem) =
        for coin in sys.GetVerticesOfJobCalls() do
            for td in coin.TargetJob.TaskDefs do
                for api in td.ApiItems do
                    if api.RX.IsNull() then
                        failwithf $"interface 정의시 관찰 Work가 없습니다. \n(error: {api.Name})"
                    if api.TX.IsNull() then
                        failwithf $"interface 정의시 지시 Work가 없습니다. \n(error: {api.Name})"

                    if td.OutAddress <> TextSkip && coin.TargetJob.JobParam.JobAction = Push then
                        if coin.MutualResetCoins.isEmpty() then 
                            failwithf $"Push type must be an interlock device \n(error: {coin.Name})"



    // unused
    let private checkErrExternalStartRealExist (sys:DsSystem) = 
        let flowEdges = (sys.Flows |> Seq.collect(fun f -> f.Graph.Edges))        
        let exEdges =
            flowEdges
                .Where(fun e ->
                    e.Source.TryGetPureCall().IsSome
                        || e.Target.TryGetPureCall().IsSome)  

        if not(exEdges.any()) then
            failwithf $"PLC 시스템은 외부시작 신호가 없으면 시작 불가 입니다. HelloDS 모델을 참고하세요"


    let internal checkMultiDevPair(sys: DsSystem) = 
        let devicesCalls = 
            sys.GetTaskDevsCall()
                .DistinctBy(fun (td, c) -> (td, c.TargetJob))
                .Where(fun (_, call) -> call.TargetJob.JobTaskDevInfo.TaskDevCount > 1)

        let groupDev = devicesCalls  |> Seq.groupBy (fun (dev, _) -> dev.DeviceName)
    
        for (_, calls) in groupDev  do
            let jobMultis =
                calls
                |> Seq.map (fun (_, call) -> call.TargetJob.JobTaskDevInfo.TaskDevCount )
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

    let CheckNullAddress (sys: DsSystem) = 
        // Check for null addresses in jobs
        let nullTagJobs = 
            sys.Jobs.SelectMany(fun j -> j.GetNullAddressDevTask()) |> toArray

        if nullTagJobs.any() then 
            let errJobs = String.Join ("\n", nullTagJobs.Select(fun s->s.QualifiedName))
            failwithf $"Device 주소가 없습니다. \n{errJobs} \n\nAdd I/O Table을 수행하세요"

        // Check for null buttons
        let nullBtns = 
            sys.HWButtons
            |> filter (fun b -> b.InTag.IsNull() || (b.OutTag.IsNull() && b.OutAddress <> TextSkip))
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
        for j in sys.Jobs do
            for td in j.TaskDefs do
                if td.ExistOutput then 
                    let outParam = td.GetOutParam(j)
                    if j.ActionType = Push && outParam.Type = DuBOOL then
                            failWithLog $"{td.Name} {j.ActionType} 은 bool 타입만 지원합니다." 
                    elif outParam.Type <> DuBOOL && outParam.DevValue.IsNull() then 
                            failWithLog $"{td.Name} {td.OutAddress} 은 value 값을 입력해야 합니다." 
