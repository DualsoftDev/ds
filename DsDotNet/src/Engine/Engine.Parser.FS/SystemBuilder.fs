namespace Engine.Parser.FS

open System
open System.Collections.Generic
open System.Linq
open System.Runtime.CompilerServices
open System.Diagnostics
open Engine.Common.FS
open Engine.Core
open System.IO

[<AutoOpen>]
module SystemBuilderModule =
    type CallSpec = { Api:string*string; Tx:string; Rx:string }

    type SystemBuilder internal (system:DsSystem) =
        let paths = ResizeArray<string>()
        member __.Zero() = system
        member __.Yield(_) = system

        member __.ReturnFrom(m) = m
        member __.Run(f) = f()
        member __.Bind(m, f) = f m
        member __.Delay(f: unit -> _) = f
        member x.Combine(m1, m2) = x.Bind(m1, fun () -> m2)
        member inline __.For (s, f) = s

        //member this.TryWith(delayedExpr, handler) =
        //    try this.Run(delayedExpr)
        //    with exn -> handler exn
        //member this.TryFinally(delayedExpr, compensation) =
        //    try this.Run(delayedExpr)
        //    finally compensation()
        //member this.Using(resource:#IDisposable, body) =
        //    this.TryFinally(this.Delay(fun ()->body resource), fun () -> match box resource with null -> () | _ -> resource.Dispose())
        //[<CustomOperation("get_flow")>]
        //member __.GetFlow(sys, flowName) = theSystem.FindFlow(flowName)


        [<CustomOperation("this")>]
        member x.GetThis(sys) =
            assert(x = sys)
            sys

        //member inline __.For (sys, f) = sys//f sys
        [<CustomOperation("name")>]
        member __.SetName(sys, name) =
          (sys :> INamed).Name <- name
          sys

        [<CustomOperation("flow")>]
        member __.CreateFlow(sys, name) =
          let flow = Flow.Create(name, sys)
          sys
        [<CustomOperation("create_flow")>]
        member __.CreateFlow(sys, flow:Flow) =
          //let flow = Flow.Create(name, sys)
          sys
        //[<CustomOperation("create_flow")>]
        //member __.CreateFlow(sys, flow:Flow) =
        //  //flow.System <- theSystem
        //  sys


        [<CustomOperation("path")>]
        member __.SetPath(sys, path) =
            paths.AddRange(path)
            sys

        [<CustomOperation("device")>]
        member __.LoadDevice(sys, loadedName, simpleFilePath) =
            let absoluteFilePath =
                [
                    yield simpleFilePath;
                    yield! paths.Select(fun p -> p + "\\" + simpleFilePath)
                ] |> List.find (fun f -> File.Exists(f))

            let device = system.LoadDeviceAs(loadedName, absoluteFilePath, simpleFilePath)
            sys
        [<CustomOperation("call")>]
        member __.CreateCall(sys, callName, callSpecs:CallSpec list) =
            let apiItems = [
                for cs in callSpecs do
                    let dev, apiName = cs.Api
                    let apiExported = system.ApiUsages.Find(nameComponentsEq [dev; apiName])
                    ApiCallDef(apiExported, cs.Tx, cs.Rx, dev)
            ]
            let apiGroup = ApiCall(callName, apiItems)
            system.ApiGroups.Add apiGroup
            sys


    type FlowBuilder internal (flow:Flow) =
        let system = flow.System
        member __.Zero() = flow
        member __.Yield(_) = flow
        //member __.Yield(_) = system

        member __.ReturnFrom(m) = m
        member __.Run(f) = f()
        member __.Bind(m, f) = f m
        member __.Delay(f: unit -> _) = f
        member x.Combine(m1, m2) = x.Bind(m1, fun () -> m2)
        member inline __.For (s, f) = s

        [<CustomOperation("real")>]
        member __.CreateReal(sys, realName) =
            let real = Real.Create(realName, flow)
            flow

    let withSystem system = SystemBuilder(system)
    let withFlow flow = FlowBuilder(flow)



[<AutoOpen>]
module MetaBuilder =

    type LoadDeviceSpec = {
        LoadedName:string
        Path:string
    }

    type ApiItem = {
        Device:string
        ApiName:string
        Tx:string
        Rx:string
    }
    type CallMeta = {
        CallKey:string
        ApiItems:ApiItem list
    }

    type VSpec =
        | Group of Fqdn list
        | Fqdn

    type Edge = {
        Source: VSpec
        Symbol: string
        Target: VSpec
    }

    type AliasMeta = {
        AliasKey: Fqdn
        Mnemonics: string list
    }
    type FlowMeta = {
        Name:string
        Aliases:AliasMeta list
        Reals: string list
        Calls: Fqdn list
        Edges: Edge list
    }

    type SystemMeta = {
        Name:string;
        Paths:string list;
        Devices:LoadDeviceSpec list;
        Calls:CallMeta list;
        Flows:FlowMeta list;
    }


    type FlowMetaBuilder internal () =
        let flow:FlowMeta =
            { Name = ""
              Aliases = []
              Reals = []
              Calls = []
              Edges = [] }
        member __.Zero() = flow
        member __.Yield(_) = flow

        member __.ReturnFrom(m) = m
        member __.Run(f) = f()
        member __.Bind(m, f) = f m
        member __.Delay(f: unit -> _) = f
        member x.Combine(m1, m2) = x.Bind(m1, fun () -> m2)
        member inline __.For (s, f) = s
        [<CustomOperation("name")>]
        member __.SetName(sys:FlowMeta, name) =
            { sys with Name = name }

    type SystemMetaBuilder internal () =
        let system =
            { Name = ""
              Paths = []
              Devices = []
              Calls = []
              Flows = [] }
        member __.Zero() = system
        member __.Yield(_) = system

        member __.ReturnFrom(m) = m
        member __.Run(f) = f()
        member __.Bind(m, f) = f m
        member __.Delay(f: unit -> _) = f
        member x.Combine(m1, m2) = x.Bind(m1, fun () -> m2)
        member inline __.For (s, f) = s

        [<CustomOperation("this")>]
        member x.GetThis(sys):SystemMeta = sys

        [<CustomOperation("name")>]
        member __.SetName(sys:SystemMeta, name) =
            { sys with Name = name }

        [<CustomOperation("path")>]
        member __.SetPath(sys, path) =
            { sys with Paths = sys.Paths @ [path] }

        [<CustomOperation("device")>]
        member __.LoadDevice(sys, loadedName, simpleFilePath) =
            { sys with Devices = sys.Devices @ [{LoadedName = loadedName; Path = simpleFilePath}] }

        [<CustomOperation("add_flow")>]
        member __.AddFlow(sys, flow:FlowMeta) =
            { sys with Flows = sys.Flows +++ flow }

        //[<CustomOperation("flow")>]
        //member __.CreateFlow(sys, loadedName, simpleFilePath) =
        //    { sys with Devices = sys.Devices @ [{LoadedName = loadedName; Path = simpleFilePath}] }

        //[<CustomOperation("call")>]
        //member __.CreateCall(sys, callName, callSpecs:CallSpec list) =
        //    { sys with Calls = sys.Calls @ [{CallKey = callName; ApiItems = callSpecs |> List.map (fun cs -> {Device = cs.Api.Device; ApiName = cs.Api.ApiName; Tx = cs.Tx; Rx = cs.Rx})}] }

    let system = SystemMetaBuilder()
    let flow = FlowMetaBuilder()