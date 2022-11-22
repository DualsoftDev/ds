namespace Engine.Parser.FS

open System
open System.Collections.Generic
open System.Linq
open System.Runtime.CompilerServices
open System.Diagnostics
open Engine.Common.FS
open Engine.Core

[<AutoOpen>]
module SystemBuilderModule =
    type CallSpec = { Api:string*string; Tx:string; Rx:string }

    type SystemBuilder internal () =
        let mutable theSystem = getNull<DsSystem>()
        let thePath = ResizeArray<string>()
        member __.Zero() = theSystem
        member __.Yield(_) =
            theSystem <- DsSystem.Create(null, null)
            theSystem

        member __.ReturnFrom(m) = m
        member __.Run(f) = f()
        member __.Bind(m, f) = f m
        member __.Delay(f: unit -> _) = f
        member inline __.Combine (a, b) = a//Awaitable.combine a b
        //member inline __.Combine a, b) = Awaitable.combine (AsyncWorkflow a) b
        //member inline __.Combine (a, b) = Awaitable.combine (DotNetTask a) b
        //member inline __.While (g, a) = Awaitable.doWhile g a
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



        //member inline __.For (sys, f) = sys//f sys
        [<CustomOperation("name")>]
        member __.SetName(sys, name) =
          (sys :> INamed).Name <- name
          sys

        [<CustomOperation("flow")>]
        member __.CreateFlow(sys, name) =
          let flow = Flow.Create(name, sys)
          sys
        //[<CustomOperation("create_flow")>]
        //member __.CreateFlow(sys, flow:Flow) =
        //  //flow.System <- theSystem
        //  sys


        [<CustomOperation("real")>]
        member __.CreateReal(sys, flowName, realName) =
            let flow = theSystem.FindFlow(flowName)
            let real = Real.Create(realName, flow)
            sys

        [<CustomOperation("path")>]
        member __.SetPath(sys, path) =
            thePath.AddRange(path)
            sys

        [<CustomOperation("device")>]
        member __.LoadDevice(sys, loadedName, filePath) =
            let device = theSystem.LoadDeviceAs(loadedName, filePath, filePath)
            sys
        [<CustomOperation("call")>]
        member __.CreateCall(sys, callName, callSpecs:CallSpec list) =
            let apiItems = [
                for cs in callSpecs do
                    let dev, apiName = cs.Api
                    let apiExported = theSystem.ApiItems.Find(nameComponentsEq [dev; apiName])
                    ApiItem(apiExported, cs.Tx, cs.Rx)
            ]
            let call = Call(callName, apiItems)
            theSystem.Calls.Add call
            sys

    type FlowBuilder internal (system:DsSystem) =
        let mutable theFlow = getNull<Flow>()
        member __.Zero() = theFlow
        member __.Yield(_) =
            theFlow <- Flow.Create(null, system)
            theFlow

    let system = SystemBuilder()
    //let flow = FlowBuilder(theSystem)




