namespace Engine.Parser.FS

open System.Collections.Generic
open System.Diagnostics

open Engine.Core
open Engine.Common.FS
open type Engine.Parser.dsParser

[<AutoOpen>]
module ParserUtil =
    let dummyDeviceLoader (theSystem:DsSystem) (absoluteFilePath:string, simpleFilePath:string) (loadedName:string) : Device =
        failwith "Should be reimplemented."

    let dummyExternalSystemLoader (theSystem:DsSystem) (absoluteFilePath:string, simpleFilePath:string) (loadedName:string) : ExternalSystem =
        failwith "Should be reimplemented."

    let mutable fwdLoadDevice = dummyDeviceLoader
    let mutable fwdLoadExternalSystem = dummyExternalSystemLoader

    [<DebuggerDisplay("{FullName}({ContextType.Name})")>]
    type ContextInformation = {
        System: string option
        Flow: string option
        Parenting: string option
        Names: string list
        ContextType:System.Type
    } with
        static member Create(parserRuleContext, system:string option, flow, parenting, names) =
            {   ContextType = parserRuleContext.GetType();
                System = system; Flow = flow; Parenting = parenting; Names = names }

        static member CreateFullNameComparer() =
            {   new IEqualityComparer<ContextInformation> with
                    member _.Equals(x:ContextInformation, y:ContextInformation) = x.FullName = y.FullName
                    member _.GetHashCode(x:ContextInformation) = x.FullName.GetHashCode() }

        member x.Tuples = x.System, x.Flow, x.Parenting, x.Names
        member x.NameComponents = [
            if x.System.IsSome then yield x.System.Value
            if x.Flow.IsSome then yield x.Flow.Value
            if x.Parenting.IsSome then yield x.Parenting.Value
            if x.ContextType = typedefof<SystemContext>
                || x.ContextType = typedefof<FlowBlockContext>
                || x.ContextType = typedefof<ParentingContext>
                then
                ()
            else
                yield! x.Names
        ]
        member x.FullName = x.NameComponents.ToArray().Combine()

    let getParentWrapper (ci:ContextInformation) (flow:Flow option) (parenting:Real option) =
        match ci.Parenting with
        | Some prnt -> Real parenting.Value
        | None -> Flow flow.Value

