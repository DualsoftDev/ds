namespace Engine.CodeGen

open System.Collections.Generic
open Engine.Core

[<AutoOpen>]
module HmiGenModule =
    type Category =
        | None       = 0
        | System     = 1
        | Flow       = 2
        | Real       = 3
        | Device     = 4
        | ApiGroup   = 5
        | Interface  = 6
        | Button     = 7
    and ButtonType =
        | None       = 0
        | Start      = 8
        | Reset      = 9
        | On         = 10
        | Off        = 11
        | Run        = 12
        | Emergency  = 13
        | Auto       = 14
        | Clear      = 15

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
                (system:DsSystem)
                (buttonsInFlow:Dictionary<string, HashSet<Flow>>)
                buttonType =
            for KeyValue(btnName, btnValue) in buttonsInFlow do
                let flowNames = [ for flow in btnValue do flow.QualifiedName ]
                addFlowButton btnName system.Name flowNames buttonType

        let addUnionButtons
                (system:DsSystem) (flow:Flow)
                (btnTargetMap:Dictionary<ButtonType, ResizeArray<string>>) =
            let flowName = $"{flow.QualifiedName}"
            let btnNames = [
                "EMSTOPof", ButtonType.Emergency;
                "ATMANUof", ButtonType.Auto;
                "CLEAR_of", ButtonType.Clear;
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
                "RUN", ButtonType.Run;
                "EMSTOP", ButtonType.Emergency;
                "ATMANU", ButtonType.Auto;
                "CLEAR", ButtonType.Clear;
            ]

            for button, btnType in buttons do
                addButton button null btnType
                match btnType with
                | ButtonType.Run ->
                    for sys in model.Systems do
                        for sp in sys.StartPoints do
                            hmiInfos[button].targets.Add(sp.QualifiedName)
                | ButtonType.Emergency | ButtonType.Auto | ButtonType.Clear ->
                    for flow in flowNames do hmiInfos[button].targets.Add(flow)
                | _ ->
                    failwith "type error"

        let addInterface (api:ApiItem) (usedIn:string) =
            if false = hmiInfos.ContainsKey(api.QualifiedName) then
                let info = 
                    genInfo
                        api.QualifiedName Category.Interface
                        ButtonType.None api.System.QualifiedName
                info.used_in.Add(usedIn)
                hmiInfos.Add(api.QualifiedName, info)

        let addDevice (apiGroup:JobDef seq) (usedIn:string) =
            for dvc in apiGroup do
                let api = dvc.ApiItem
                let device = dvc.ApiItem.System.Name
                if false = hmiInfos.ContainsKey(device) then
                    let info = 
                        genInfo device Category.Device ButtonType.None null
                    hmiInfos.Add(device, info)

                addInterface api usedIn

        let addApiGroup (system:DsSystem) (flow:Flow) (vertex:Vertex) = 
            let addToUsedIn deviceGroup target =
                if false = hmiInfos[deviceGroup].used_in.Contains(target) then
                    hmiInfos[deviceGroup].used_in.Add(target)

            let dvcGrp =
                match vertex with
                | :? Call as c ->
                    addDevice c.CallTarget.ApiItems c.CallTarget.Name
                    c.Name
                | :? Alias as a ->
                    match a.ApiTarget with
                    | AliasTargetReal r -> 
                        r.NameComponents[0]
                    | AliasTargetCall c -> 
                        addDevice c.CallTarget.ApiItems c.CallTarget.Name
                        c.Name
                    | _ ->
                        null
                | _ ->
                    null

            if hmiInfos.ContainsKey(dvcGrp) = false then
                let info = 
                    genInfo dvcGrp Category.ApiGroup ButtonType.None null
                hmiInfos.Add(dvcGrp, info)

            addToUsedIn dvcGrp system.Name
            addToUsedIn dvcGrp flow.QualifiedName

            match vertex.Parent with
            | ParentReal realParent -> 
                addToUsedIn dvcGrp realParent.QualifiedName
            | _ -> 
                ()

        let succeess, message = 
            try
                for sys in model.Systems do
                    let groupBtnCombiner = addGroupButtons sys
                    addSystemFlowReal sys
                    groupBtnCombiner sys.AutoButtons ButtonType.Auto
                    groupBtnCombiner sys.ResetButtons ButtonType.Clear
                    groupBtnCombiner sys.EmergencyButtons ButtonType.Emergency
                    let btnTgtMap =
                        new Dictionary<ButtonType, ResizeArray<string>>()
                    //for info in hmiInfos do
                    //    btnTgtMap.Add(
                    //        info.Value.botton_type, 
                    //        info.Value.targets
                    //    )
                    for flow in sys.Flows do
                        addSystemFlowReal flow
                        addUnionButtons sys flow btnTgtMap
                        for rootSeg in flow.Graph.Vertices do
                            match rootSeg with
                            | :? Real as real ->
                                addSystemFlowReal rootSeg
                                for vert in real.Graph.Vertices do
                                    addApiGroup sys flow vert
                            | :? Call as call ->
                                addApiGroup sys flow call
                            | :? Alias as alias ->
                                match alias.ApiTarget with
                                | AliasTargetReal rt ->
                                    addSystemFlowReal rt
                                | AliasTargetCall ct ->
                                    addApiGroup sys flow ct
                                | _ ->
                                    ()
                            | _ ->
                                printfn "unknown type has detected"
                if hmiInfos.Count <> 0 then addGlobalButtons
                true, null
            with
                | ex -> false, ex.Message
        let body = hmiInfos.Values |> List.ofSeq

        { from = "hmi"; succeed = succeess; body = body; error = message; }

    //[<EntryPoint>]
    //let main argv =
    //    let helper =
    //        ModelParser.ParseFromString2(Program.EveryScenarioText,
    //        ParserOptions.Create4Simulation());
    //    let model = helper.Model;
    //    let json = GenHmiCode(model)
    //    printfn "%A" (json.ToString())
    //    0