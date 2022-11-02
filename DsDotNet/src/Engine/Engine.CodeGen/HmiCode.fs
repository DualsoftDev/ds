namespace Engine.CodeGen

open System.Collections.Generic
open Engine.Core
open Newtonsoft.Json

[<AutoOpen>]
module HmiGenModule =
    type Category =
        | None      = 0
        | System    = 1
        | Flow      = 2
        | Real      = 3
        | Device    = 4
        | Interface = 5
        | Button    = 6
    and ButtonType =
        | None      = 0
        | Start     = 7
        | Reset     = 8
        | On        = 9
        | Off       = 10
        | Run       = 11
        | Emergency = 12
        | Auto      = 13
        | Clear     = 14

    type Info = {
        name:string;
        category:Category;
        botton_type:ButtonType;
        parent:string;
        used_in:ResizeArray<string>;
        targets:ResizeArray<string>;
    }
    and Initialize = {
        mode:string;
        initializer:Info list;
    }

    let GenHmiCode(model:Model) =
        let hmiInfos = new Dictionary<string, Info>()

        let genInfo
                name category
                buttonType (parent:string) =
            {
                name = name;
                category = category;
                botton_type = buttonType;
                parent = parent;
                used_in = new ResizeArray<string>(0);
                targets = new ResizeArray<string>(0);
            }

        let addSystemFlowReal (systemFlowReal:obj) =
            let category =
                match systemFlowReal with
                | :? DsSystem -> Category.System
                | :? Flow -> Category.Flow
                | :? Real -> Category.Real
                | _ -> Category.None

            let target, parent =
                match category with
                | Category.System ->
                    let sys = (systemFlowReal :?> DsSystem)
                    sys.QualifiedName, null
                | Category.Flow ->
                    let flow = (systemFlowReal :?> Flow)
                    flow.QualifiedName, flow.System.Name
                | Category.Real ->
                    let real = (systemFlowReal :?> Real)
                    real.QualifiedName, real.Flow.QualifiedName
                | _ ->
                    null, null

            if target <> null &&
                    false = hmiInfos.ContainsKey(target) then
                let xxx = genInfo target category ButtonType.None parent
                hmiInfos.Add(target, xxx)

        let addButton btnName systemName buttonType =
            let info = genInfo btnName Category.Button buttonType systemName
            hmiInfos.Add(btnName, info)

        let addFlowButton btnName systemName flowNames buttonType =
            if hmiInfos.ContainsKey(btnName) = false then
                addButton btnName systemName buttonType
                for flow in flowNames do hmiInfos[btnName].targets.Add(flow)

        let addGroupButtons
                (system:DsSystem)
                (buttonsInFlow:Dictionary<string, ResizeArray<Flow>>)
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
                    addFlowButton
                        btnName system.Name [flowName] btnType

        let addGlobalButtons (model:Model) =
            let flowNames =
                hmiInfos
                |> Seq.filter(fun info ->
                    info.Value.category = Category.Flow
                )
                |> Seq.map(fun info -> info.Value.name)

            let buttons = [
                "RUN", ButtonType.Run;
                "EMSTOP", ButtonType.Emergency;
                "ATMANU", ButtonType.Auto;
                "CLEAR", ButtonType.Clear;
            ]
            let flows = flowNames
            for button, btnType in buttons do
                addButton button null btnType
                match btnType with
                | ButtonType.Run ->
                    for sys in model.Systems do
                        for sp in sys.StartPoints do
                            hmiInfos[button].targets.Add(sp.QualifiedName)
                | ButtonType.Emergency
                | ButtonType.Auto
                | ButtonType.Clear ->
                    for flow in flows do
                        hmiInfos[button].targets.Add(flow)
                | _ ->
                    failwith "type error"


        let addInterface (api:ApiItem) =
            if false = hmiInfos.ContainsKey(api.QualifiedName) then
                hmiInfos.Add(
                    api.QualifiedName,
                    genInfo
                        api.QualifiedName
                        Category.Interface
                        ButtonType.None
                        api.System.QualifiedName
                )

        let addDevice
                (model:Model) (system:DsSystem) (flow:Flow) (call:Vertex) =
            let addToUsedIn device target =
                if false = hmiInfos[device].used_in.Contains(target) then
                    hmiInfos[device].used_in.Add(target)

            let (device, api) =
                match call with
                | :? Call as c ->
                    c.ApiItem.System.Name, Some(c.ApiItem)
                | :? Alias as a ->
                    let aliasKey =
                        match a.Target with
                        | RealTarget r -> r.NameComponents
                        | CallTarget c -> c.ApiItem.NameComponents
                    aliasKey[0], Some(model.FindApiItem aliasKey)
                | _ ->
                    null, None

            if hmiInfos.ContainsKey(device) = false then
                hmiInfos.Add(
                    device,
                    genInfo device Category.Device ButtonType.None null
                )

            addInterface api.Value
            addToUsedIn device system.Name
            addToUsedIn device flow.QualifiedName

            match call.Parent with
            | Real realParent ->
                addToUsedIn device realParent.QualifiedName
            | _ ->
                ()

        for sys in model.Systems do
            // if sys.Name = "My" then // to check
            if sys.Active then
                let groupBtnCombiner = addGroupButtons sys
                addSystemFlowReal sys
                groupBtnCombiner sys.AutoButtons ButtonType.Auto
                groupBtnCombiner sys.ResetButtons ButtonType.Clear
                groupBtnCombiner sys.EmergencyButtons ButtonType.Emergency
                let btnTgtMap =
                    new Dictionary<ButtonType, ResizeArray<string>>()
                for info in hmiInfos do
                    btnTgtMap.Add(info.Value.botton_type, info.Value.targets)
                for flow in sys.Flows do
                    addSystemFlowReal flow
                    addUnionButtons sys flow btnTgtMap
                    for rootSeg in flow.Graph.Vertices do
                        match rootSeg with
                        | :? Real as real ->
                            addSystemFlowReal rootSeg
                            for call in real.Graph.Vertices do
                                addDevice model sys flow call
                        | :? Call as call ->
                            addDevice model sys flow call
                        | :? Alias as alias ->
                            match alias.Target with
                            | RealTarget rt ->
                                addSystemFlowReal rt
                            | CallTarget ct ->
                                addDevice model sys flow alias
                        | _ ->
                            printfn "unknown type has detected"

        addGlobalButtons model

        let initializer = {
            mode = "init";
            initializer = hmiInfos.Values |> List.ofSeq;
        }
        let settings = JsonSerializerSettings()
        settings.Converters.Add(Converters.StringEnumConverter())
        JsonConvert.SerializeObject(
            initializer,
            // Formatting.Indented, // to visual check
            settings
        )

    //[<EntryPoint>]
    //let main argv =
    //    let helper =
    //        ModelParser.ParseFromString2(Program.EveryScenarioText,
    //        ParserOptions.Create4Simulation());
    //    let model = helper.Model;
    //    let json = GenHmiCode(model)
    //    printfn "%A" (json.ToString())
    //    0
