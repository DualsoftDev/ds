namespace Engine.CodeGenHMI

open System.Collections.Generic
open Engine.Core
open Dual.Common.Core.FS

[<AutoOpen>]
module HmiGenModule =
    type Type =
        | None = 0
        | System = 1
        | Flow = 2
        | Real = 3
        | Job = 4
        | Device = 5
        | Interface = 6
        | Button = 7

    type ButtonType =
        | None = 0
        | Start = 1
        | Reset = 2
        | On = 3
        | Off = 4
        | Drive = 5
        | Test = 6
        | Emergency = 7
        | Auto = 8
        | Manual = 9
        | Clear = 10
        | Stop = 11
        | Home = 12
        | Ready = 13

    type Info =
        { name: string
          parent: string
          category: Type
          button_type: ButtonType
          used_in: ResizeArray<string>
          targets: ResizeArray<string> }

    type HmiCode(model: Model) =
        let buttons =
            [ "AUTO", ButtonType.Auto
              "MANUAL", ButtonType.Manual
              "DRIVE", ButtonType.Drive
              "STOP", ButtonType.Stop
              "CLEAR", ButtonType.Clear
              "EMSTOP", ButtonType.Emergency
              "TEST", ButtonType.Test
              "HOME", ButtonType.Home
              "READY", ButtonType.Ready ]

        let genInfo name category buttonType parent =
            { name = name
              parent = parent
              category = category
              button_type = buttonType
              used_in = new ResizeArray<string>(0)
              targets = new ResizeArray<string>(0) }

        let addButton btnName systemName buttonType (hmiInfos: Dictionary<string, Info>) =
            let info = genInfo btnName Type.Button buttonType systemName
            hmiInfos.Add(btnName, info)

        let addBasicComponents (hmiInfos: Dictionary<string, Info>) =
            let addSystemFlowReal (systemFlowReal: obj) =
                let target, parent, category =
                    match systemFlowReal with
                    | :? DsSystem ->
                        let sys = (systemFlowReal :?> DsSystem)
                        sys.QualifiedName, null, Type.System
                    | :? Flow ->
                        let flow = (systemFlowReal :?> Flow)
                        flow.QualifiedName, flow.System.Name, Type.Flow
                    | :? Real ->
                        let real = (systemFlowReal :?> Real)
                        real.QualifiedName, real.Flow.QualifiedName, Type.Real
                    | _ -> null, null, Type.None

                let info = genInfo target category ButtonType.None parent
                hmiInfos.Add(target, info)

            let addFlowButton btnName systemName flowNames buttonType =
                addButton btnName systemName buttonType hmiInfos

                for flow in flowNames do
                    hmiInfos[btnName].targets.Add(flow)

            let addGroupButtons (system: DsSystem) (buttonsInFlow: ButtonDef seq) btnType =
                for btn in buttonsInFlow do
                    let flowNames =
                        [ for flow in btn.SettingFlows do
                              flow.QualifiedName ]

                    addFlowButton btn.Name system.Name flowNames btnType

            let addUnionButtons (system: DsSystem) (flow: Flow) =
                let flowName = $"{flow.QualifiedName}"

                for name, btnType in buttons do
                    let btnName = $"{name}of__{flowName}"
                    addFlowButton btnName system.Name [ flowName ] btnType

            let sys  = model.System
            addSystemFlowReal sys
            let groupBtnCombiner = addGroupButtons sys
            groupBtnCombiner sys.AutoButtons ButtonType.Auto
            groupBtnCombiner sys.ManualButtons ButtonType.Manual
            groupBtnCombiner sys.DriveButtons ButtonType.Drive
            groupBtnCombiner sys.StopButtons ButtonType.Stop
            groupBtnCombiner sys.ClearButtons ButtonType.Clear
            groupBtnCombiner sys.EmergencyButtons ButtonType.Emergency
            groupBtnCombiner sys.TestButtons ButtonType.Test
            groupBtnCombiner sys.HomeButtons ButtonType.Home
            groupBtnCombiner sys.ReadyButtons ButtonType.Ready

            for flow in sys.Flows do
                addSystemFlowReal flow
                addUnionButtons sys flow

                for rootSeg in flow.Graph.Vertices do
                    match rootSeg with
                    | :? Real -> addSystemFlowReal rootSeg
                    | _ -> ()

            hmiInfos

        let addGlobalButtons (hmiInfos: Dictionary<string, Info>) =
            let flowNames =
                hmiInfos
                |> Seq.filter (fun info -> info.Value.category = Type.Flow)
                |> Seq.map (fun info -> info.Value.name)

            for button, btnType in buttons do
                addButton button null btnType hmiInfos

                match btnType with
                | (ButtonType.Test | ButtonType.Drive) ->
                        for sp in model.System.StartPoints do
                            hmiInfos[button].targets.Add(sp.QualifiedName)
                | (ButtonType.Auto | ButtonType.Manual | ButtonType.Emergency | ButtonType.Stop | ButtonType.Clear | ButtonType.Home | ButtonType.Ready) ->
                    for flow in flowNames do
                        hmiInfos[button].targets.Add(flow)
                | _ -> failwithlog "type error"

            hmiInfos

        let addJobComponentAndUses (hmiInfos: Dictionary<string, Info>) =
            let addUses (system: DsSystem) (flow: Flow) (vertex: Vertex) =
                let addToUsedIn nowVertex target =
                    if not <| hmiInfos[nowVertex].used_in.Contains(target) then
                        hmiInfos[nowVertex].used_in.Add(target)

                let jobName (call: Call) =
                    $"{system.Name}.{call.TargetJob.Name}"


                let aliasName (alias: Alias) =
                    match alias.TargetWrapper with
                    | DuAliasTargetCall c -> jobName c
                    | DuAliasTargetReal r -> r.QualifiedName
                    | DuAliasTargetRealExFlow f -> f.QualifiedName

                let vertName =
                    match vertex with
                    | :? Call as c -> jobName c
                    | :? Real as r -> r.QualifiedName
                    | :? RealOtherFlow as f -> f.QualifiedName
                    | :? Alias as a -> aliasName a
                    | _ -> null

                addToUsedIn vertName system.Name
                addToUsedIn vertName flow.QualifiedName

                match vertex.Parent with
                | DuParentReal pr -> addToUsedIn vertName pr.QualifiedName
                | _ -> ()

            let addInterface (api: ApiItem) (usedIn: string) =
                if not <| hmiInfos.ContainsKey(api.QualifiedName) then
                    let info =
                        genInfo api.QualifiedName Type.Interface ButtonType.None api.System.QualifiedName

                    info.used_in.Add(usedIn)
                    hmiInfos.Add(api.QualifiedName, info)

            let addApiGroup (system: DsSystem) (job: Job) =
                let jobName = $"{system.Name}.{job.Name}"

                if not <| hmiInfos.ContainsKey(jobName) then
                    let info = genInfo jobName Type.Job ButtonType.None system.Name
                    hmiInfos.Add(jobName, info)

                for dvc in job.DeviceDefs do
                    let api = dvc.ApiItem
                    let device = dvc.ApiItem.System.Name

                    if not <| hmiInfos.ContainsKey(device) then
                        let info = genInfo device Type.Device ButtonType.None system.Name
                        hmiInfos.Add(device, info)

                    addInterface api jobName

            let sys  = model.System 
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

            hmiInfos

        let generate () =
            let hmiInfos = new Dictionary<string, Info>()

            let success, message =
                try
                    hmiInfos
                    |> addBasicComponents
                    |> addJobComponentAndUses
                    |> addGlobalButtons
                    |> ignore

                    true, null
                with ex ->
                    false, ex.Message

            let body = hmiInfos.Values |> List.ofSeq

            { from = "modeler"
              success = success
              body = body
              error = message }

        member x.Generate() = generate ()
