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
    | Emergency = 9
    | Auto      = 10
    
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
                
        let addButtons 
                (system:DsSystem)
                (hmiInfos:Dictionary<string, Info>)
                (buttonsInFlow:Dictionary<string, ResizeArray<Flow>>) 
                (buttonType:ButtonType) =
            for btn in buttonsInFlow do
                let btnName = btn.Key
                let flows = btn.Value
                if hmiInfos.ContainsKey(btnName) = false then
                    hmiInfos.Add(
                        btnName,
                        genInfo btnName Category.Button buttonType system.Name
                    )
                    for flow in flows do
                        hmiInfos.[btnName].targets.Add(flow.QualifiedName)
                        
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
                (model:Model)
                (system:DsSystem) (flow:Flow)
                (hmiInfos:Dictionary<string, Info>) (call:Vertex) = 
            let addToUsedIn 
                    (hmiInfos:Dictionary<string, Info>)
                    (device:string) (target:string) = 
                if hmiInfos.[device].used_in.Contains(target) = false then
                    hmiInfos.[device].used_in.Add(target)

            let (device, api) = 
                match call with
                | :? Call as c -> c.ApiItem.System.Name, Some(c.ApiItem)
                | :? Alias as a -> a.AliasKey.[0], Some(model.FindApiItem a.AliasKey)
                | _ -> null, None

            if hmiInfos.ContainsKey(device) = false then
                hmiInfos.Add(
                    device,
                    genInfo device Category.Device ButtonType.None null
                )

            addInterface api.Value hmiInfos
            addToUsedIn hmiInfos device system.Name
            addToUsedIn hmiInfos device flow.QualifiedName

            let parent = call.Parent.Core
            if parent :? Real then
                addToUsedIn hmiInfos device parent.QualifiedName


        let hmiInfos = new Dictionary<string, Info>()
        for sys in model.Systems do
            //if sys.Name = "My" then // to check
            if sys.Active then
                addSystemFlowReal sys hmiInfos
                addButtons sys hmiInfos sys.AutoButtons ButtonType.Auto
                addButtons sys hmiInfos sys.StartButtons ButtonType.Start
                addButtons sys hmiInfos sys.ResetButtons ButtonType.Reset
                addButtons sys hmiInfos sys.EmergencyButtons ButtonType.Emergency
                for flow in sys.Flows do
                    addSystemFlowReal flow hmiInfos
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

        let settings = JsonSerializerSettings()
        settings.Converters.Add(Converters.StringEnumConverter())
        JsonConvert.SerializeObject(
            {
                mode = "init";
                initializer = [ for info in hmiInfos.Values do yield info ];
            }, 
            // Formatting.Indented, // to visual check
            settings
        )

    [<EntryPoint>]
    let main argv = 
        let helper = 
            ModelParser.ParseFromString2(Program.EveryScenarioText, 
            ParserOptions.Create4Simulation());
        let model = helper.Model;
        let xxx = model.ToDsText();
        let json = GenHmiCpuText(model)
        printfn "%A" (json.ToString())

        0