namespace Engine.CodeGenHMI

open System.Collections.Generic
open Engine.Core
open Engine.Common.FS

[<AutoOpen>]
module HmiGenModule =
    type Category =
        | None      = 0
        | System    = 1
        | Flow      = 2
        | Real      = 3
        | Job       = 4
        | Device    = 5
        | Interface = 6
        | Button    = 7
    and ButtonType =
        | None      = 0
        | Start     = 8
        | Reset     = 9
        | On        = 10
        | Off       = 11
        | Drive     = 12
        | Test      = 13
        | Emergency = 14
        | Auto      = 15
        | Manual    = 16
        | Clear     = 17
        | Stop      = 18
        | Home      = 19
        | Ready     = 20

    type Info = {
        name:string;
        parent:string;
        category:Category;
        botton_type:ButtonType;
        used_in:ResizeArray<string>;
        targets:ResizeArray<string>;
    }

    let GenHmiCode (model:Model) =
        let hmiInfos = new Dictionary<string, Info>()

        let genInfo name category buttonType parent =
            {
                name = name;
                parent = parent;
                category = category;
                botton_type = buttonType;
                used_in = new ResizeArray<string>(0);
                targets = new ResizeArray<string>(0);
            }

        let addSystemFlowReal (systemFlowReal:obj) =
            let target, parent, category  =
                match systemFlowReal with
                | :? DsSystem ->
                    let sys = (systemFlowReal :?> DsSystem)
                    sys.QualifiedName, null, Category.System
                | :? Flow ->
                    let flow = (systemFlowReal :?> Flow)
                    flow.QualifiedName, flow.System.Name, Category.Flow
                | :? Real ->
                    let real = (systemFlowReal :?> Real)
                    real.QualifiedName, real.Flow.QualifiedName, Category.Real
                | _ ->
                    null, null, Category.None

            if target <> null && false = hmiInfos.ContainsKey(target) then
                let info = genInfo target category ButtonType.None parent
                hmiInfos.Add(target, info)

        let addButton btnName systemName buttonType =
            let info = genInfo btnName Category.Button buttonType systemName
            hmiInfos.Add(btnName, info)

        let addFlowButton btnName systemName flowNames buttonType =
            if hmiInfos.ContainsKey(btnName) = false then
                addButton btnName systemName buttonType
                for flow in flowNames do hmiInfos[btnName].targets.Add(flow)

        let addGroupButtons
                (system:DsSystem) (buttonsInFlow:ButtonDef seq) buttonType =
            for btn in buttonsInFlow do
                let flowNames = [
                    for flow in btn.SettingFlows do flow.QualifiedName
                ]
                addFlowButton btn.Name system.Name flowNames buttonType

        let addUnionButtons
                (system:DsSystem) (flow:Flow)
                (btnTargetMap:Dictionary<ButtonType, ResizeArray<string>>) =
            let flowName = $"{flow.QualifiedName}"
            let btnNames = [
                "AUTOof",   ButtonType.Auto;
                "MANUALof", ButtonType.Manual;
                "DRIVEof",  ButtonType.Drive;
                "STOPof",   ButtonType.Stop;
                "CLEARof",  ButtonType.Clear;
                "EMSTOPof", ButtonType.Emergency;
                "TESTof",   ButtonType.Test;
                "HOMEof",   ButtonType.Home;
                "READYof",  ButtonType.Ready;
            ]
            for name, btnType in btnNames do
                let btnName = $"{name}__{flowName}"
                if false = btnTargetMap.ContainsKey(btnType) ||
                        false = btnTargetMap[btnType].Contains(flowName) then
                    addFlowButton btnName system.Name [flowName] btnType

        let addGlobalButtons =
            let flowNames =
                hmiInfos
                |> Seq.filter(fun info -> info.Value.category = Category.Flow)
                |> Seq.map(fun info -> info.Value.name)
            let buttons = [
                "AUTO",   ButtonType.Auto;
                "MANUAL", ButtonType.Manual;
                "DRIVE",  ButtonType.Drive;
                "STOP",   ButtonType.Stop;
                "CLEAR",  ButtonType.Clear;
                "EMSTOP", ButtonType.Emergency;
                "TEST",   ButtonType.Test;
                "HOME",   ButtonType.Home;
                "READY",  ButtonType.Ready;
            ]
            for button, btnType in buttons do
                addButton button null btnType
                match btnType with
                | ButtonType.Drive | ButtonType.Test ->
                    for sys in model.Systems do
                        for sp in sys.StartPoints do
                            hmiInfos[button].targets.Add(sp.QualifiedName)
                | ButtonType.Emergency | ButtonType.Auto | ButtonType.Manual
                | ButtonType.Clear | ButtonType.Home | ButtonType.Ready
                | ButtonType.Stop | ButtonType.Emergency ->
                    for flow in flowNames do hmiInfos[button].targets.Add(flow)
                | _ ->
                    printfn "%A" btnType
                    failwithlog "type error"

        let addInterface (api:ApiItem) (usedIn:string) =
            if false = hmiInfos.ContainsKey(api.QualifiedName) then
                let info =
                    genInfo
                        api.QualifiedName Category.Interface
                        ButtonType.None api.System.QualifiedName
                info.used_in.Add(usedIn)
                hmiInfos.Add(api.QualifiedName, info)

        let addApiGroup (system:DsSystem) (job:Job) =
            let jobName = $"{system.Name}.{job.Name}"
            if hmiInfos.ContainsKey(jobName) = false then
                let info =
                    genInfo
                        jobName Category.Job ButtonType.None system.Name
                hmiInfos.Add(jobName, info)
            for dvc in job.JobDefs do
                let api = dvc.ApiItem
                let device = dvc.ApiItem.System.Name
                if false = hmiInfos.ContainsKey(device) then
                    let info =
                        genInfo
                            device Category.Device ButtonType.None system.Name
                    hmiInfos.Add(device, info)
                addInterface api jobName

        let addUses (system:DsSystem) (flow:Flow) (vertex:Vertex) =
            let addToUsedIn nowVertex target =
                if false = hmiInfos[nowVertex].used_in.Contains(target) then
                    hmiInfos[nowVertex].used_in.Add(target)
            let getJobName name = $"{system.Name}.{name}"
            let vertName =
                match vertex with
                | :? Real as r -> r.QualifiedName
                | :? Call as c -> getJobName c.CallTargetJob.Name
                | :? Alias as a ->
                    match a.TargetWrapper with
                    | DuAliasTargetReal r    -> r.QualifiedName
                    | DuAliasTargetRealEx rx -> rx.QualifiedName
                    | DuAliasTargetCall c    -> getJobName c.CallTargetJob.Name
                | _  -> null
            addToUsedIn vertName system.Name
            addToUsedIn vertName flow.QualifiedName
            match vertex.Parent with
            | DuParentReal pr -> addToUsedIn vertName pr.QualifiedName
            | _ -> ()

        let addBasicComponents () =
            for sys in model.Systems do
                let groupBtnCombiner = addGroupButtons sys
                addSystemFlowReal sys
                groupBtnCombiner sys.AutoButtons      ButtonType.Auto
                groupBtnCombiner sys.ManualButtons    ButtonType.Manual
                groupBtnCombiner sys.DriveButtons     ButtonType.Drive
                groupBtnCombiner sys.StopButtons      ButtonType.Stop
                groupBtnCombiner sys.ClearButtons     ButtonType.Clear
                groupBtnCombiner sys.EmergencyButtons ButtonType.Emergency
                groupBtnCombiner sys.TestButtons      ButtonType.Test
                groupBtnCombiner sys.HomeButtons      ButtonType.Home
                let btnTgtMap =
                    new Dictionary<ButtonType, ResizeArray<string>>()
                for flow in sys.Flows do
                    addSystemFlowReal flow
                    addUnionButtons sys flow btnTgtMap
                    for rootSeg in flow.Graph.Vertices do
                        match rootSeg with
                        | :? Real -> addSystemFlowReal rootSeg
                        | _ -> ()

        let addJobComponentAndUses () =
            for sys in model.Systems do
                for job in sys.Jobs do
                    addApiGroup sys job
                for flow in sys.Flows do
                    for rootSeg in flow.Graph.Vertices do
                        match rootSeg with
                        | :? Real as r ->
                            addUses sys flow r
                            for vertex in r.Graph.Vertices do
                                addUses sys flow vertex
                        | _ -> addUses sys flow rootSeg

        let success, message =
            try
                addBasicComponents()
                addJobComponentAndUses()
                if hmiInfos.Count <> 0 then addGlobalButtons
                true, null
            with
                | ex -> false, ex.Message
        let body = hmiInfos.Values |> List.ofSeq
        { from = "hmi"; success = success; body = body; error = message; }