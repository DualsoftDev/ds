namespace Engine.CodeGen

open System.Collections.Generic
open Engine.Core
open Newtonsoft.Json
open Engine.Parser

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
        initializer:Info list
    }

    let GenHmiCpuText(model:CoreModule.Model) = 
        let genInfo 
                (name:string) (category:Category)
                (buttonType:ButtonType) (parent:string) = 
            {
                name = name; 
                category = category; 
                botton_type = buttonType; 
                parent = parent; 
                used_in = new ResizeArray<string>(0); 
                targets = new ResizeArray<string>(0);
            }
            
        let addSystemFlowReal
                (systemFlowReal:obj)
                (hmiInfos:Dictionary<string, Info>) =
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
                    hmiInfos.ContainsKey(target) = false then
                hmiInfos.Add(
                    target, genInfo target category ButtonType.None parent
                )
                
        let addButton 
                (btnName:string) (systemName:string)
                (buttonType:ButtonType) (hmiInfos:Dictionary<string, Info>) =
            hmiInfos.Add(
                btnName,
                genInfo btnName Category.Button buttonType systemName
            )

        let addFlowButton
                (btnName:string) (systemName:string) (flowNames:string list)
                (buttonType:ButtonType) (hmiInfos:Dictionary<string, Info>) =
            if hmiInfos.ContainsKey(btnName) = false then
                addButton btnName systemName buttonType hmiInfos
                for flow in flowNames do hmiInfos.[btnName].targets.Add(flow)

        let addGroupButtons
                (system:DsSystem) (hmiInfos:Dictionary<string, Info>)
                (buttonsInFlow:Dictionary<string, ResizeArray<Flow>>) 
                (buttonType:ButtonType) =
            for btn in buttonsInFlow do
                let btnName = btn.Key
                let flowNames = [ for flow in btn.Value do flow.QualifiedName ]
                addFlowButton btnName system.Name flowNames buttonType hmiInfos

        let addUnionButtons
                (system:DsSystem) (flow:Flow)
                (hmiInfos:Dictionary<string, Info>) 
                (btnTargetMap:Dictionary<ButtonType, ResizeArray<string>>) =
            let flowName = $"{flow.QualifiedName}"
            let btnNames = [
                "EMSTOPof", ButtonType.Emergency;
                "ATMANUof", ButtonType.Auto;
                "CLEAR_of", ButtonType.Clear;
            ]
            for name, btnType in btnNames do
                let btnName = $"{name}__{flowName}"
                if not (btnTargetMap.ContainsKey(btnType)) || 
                        not (btnTargetMap.[btnType].Contains(flowName)) then
                    addFlowButton 
                        btnName system.Name [flowName] btnType hmiInfos

        let addGlobalButtons 
                (model:Model) (hmiInfos:Dictionary<string, Info>) =
            let getFlowNames (hmiInfos:Dictionary<string, Info>) = 
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
            let flows = getFlowNames hmiInfos
            for button, btnType in buttons do
                addButton button null btnType hmiInfos
                match btnType with
                | ButtonType.Run ->
                    for sys in model.Systems do
                        for sp in sys.StartPoints do 
                            hmiInfos.[button].targets.Add(sp.QualifiedName)
                | ButtonType.Emergency
                | ButtonType.Auto
                | ButtonType.Clear ->
                    for flow in flows do
                        hmiInfos.[button].targets.Add(flow)
                | _ ->
                    failwith "type error"
                    
                        
        let addInterface 
                (api:ApiItem) (hmiInfos:Dictionary<string, Info>) =
            if hmiInfos.ContainsKey(api.QualifiedName) = false then
                hmiInfos.Add(
                    api.QualifiedName,
                    genInfo 
                        api.QualifiedName 
                        Category.Interface 
                        ButtonType.None 
                        api.System.QualifiedName
                )
            
        let addDevice
                (model:Model) (system:DsSystem) (flow:Flow)
                (hmiInfos:Dictionary<string, Info>) (call:Vertex) = 
            let addToUsedIn 
                    (hmiInfos:Dictionary<string, Info>)
                    (device:string) (target:string) = 
                if hmiInfos.[device].used_in.Contains(target) = false then
                    hmiInfos.[device].used_in.Add(target)

            let (device, api) = 
                match call with
                | :? Call as c -> 
                    c.ApiItem.System.Name, Some(c.ApiItem)
                | :? Alias as a -> 
                    a.AliasKey.[0], Some(model.FindApiItem a.AliasKey)
                | _ -> 
                    null, None

            if hmiInfos.ContainsKey(device) = false then
                hmiInfos.Add(
                    device,
                    genInfo device Category.Device ButtonType.None null
                )

            addInterface api.Value hmiInfos
            addToUsedIn hmiInfos device system.Name
            addToUsedIn hmiInfos device flow.QualifiedName

            match call.Parent with
            | Real realParent -> 
                addToUsedIn hmiInfos device realParent.QualifiedName
            | _ -> 
                ()

        let hmiInfos = new Dictionary<string, Info>()
        for sys in model.Systems do
            // if sys.Name = "My" then // to check
            if sys.Active then
                addSystemFlowReal sys hmiInfos
                addGroupButtons sys hmiInfos sys.AutoButtons ButtonType.Auto
                addGroupButtons sys hmiInfos sys.ResetButtons ButtonType.Clear
                addGroupButtons sys hmiInfos sys.EmergencyButtons ButtonType.Emergency
                let btnTgtMap = 
                    new Dictionary<ButtonType, ResizeArray<string>>()
                for info in hmiInfos do
                    btnTgtMap.Add(info.Value.botton_type, info.Value.targets)
                for flow in sys.Flows do
                    addSystemFlowReal flow hmiInfos
                    addUnionButtons sys flow hmiInfos btnTgtMap
                    for rootSeg in flow.Graph.Vertices do
                        match rootSeg with
                        | :? Real as real -> 
                            addSystemFlowReal rootSeg hmiInfos
                            for call in real.Graph.Vertices do
                                addDevice model sys flow hmiInfos call
                        | :? Call as call -> 
                            addDevice model sys flow hmiInfos call
                        | :? Alias as alias -> 
                            match alias.Target with
                            | RealTarget rt -> 
                                addSystemFlowReal rt hmiInfos
                            | CallTarget ct -> 
                                addDevice model sys flow hmiInfos alias
                            | NullTarget ->
                                printfn $"alias target error of {alias}"
                        | _ -> 
                            printfn "unknown type has detected"
        
        addGlobalButtons model hmiInfos

        let initializer = {
                mode = "init";
                initializer = [ for info in hmiInfos.Values do yield info ];
            }
        let settings = JsonSerializerSettings()
        settings.Converters.Add(Converters.StringEnumConverter())
        JsonConvert.SerializeObject(
            initializer, 
            // Formatting.Indented, // to visual check
            settings
        )

    [<EntryPoint>]
    let main argv = 
        let helper = 
            ModelParser.ParseFromString2(Program.EveryScenarioText, 
            ParserOptions.Create4Simulation());
        let model = helper.Model;
        let json = GenHmiCpuText(model)
        printfn "%A" (json.ToString())
        0