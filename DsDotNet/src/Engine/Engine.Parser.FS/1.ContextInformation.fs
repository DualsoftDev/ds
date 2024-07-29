namespace rec Engine.Parser.FS

open System.Collections.Generic
open System.Diagnostics

open Engine.Core
open Dual.Common.Core.FS
open type Engine.Parser.dsParser
open Antlr4.Runtime
open type DsParser

[<AutoOpen>]
module ContextInformationModule =
    [<DebuggerDisplay("{FullName}({ContextType.Name})")>]
    type NamedContextInformation =
        {
            System: string option
            Flow: string option
            Parenting: string option
            Names: string list
            ContextType: System.Type
        }

    type ObjectContextInformation =
        {
            System: DsSystem
            Flow: Flow option
            Parenting: Real option
            NamedContextInformation: NamedContextInformation
        }

    type NamedContextInformation with
        // 단일 이름인 경우, Combine() 을 수행하면 특수 기호 포함된 경우, quotation 부호가 강제로 붙어서 향후의 처리에 문제가 되어서 따로 처리
        member x.GetRawName() = getRawName x.Names false
        member x.Tuples = x.System, x.Flow, x.Parenting, x.Names

        member x.NameComponents =
            [
                if x.System.IsSome then
                    yield x.System.Value
                if x.Flow.IsSome then
                    yield x.Flow.Value
                if x.Parenting.IsSome then
                    yield x.Parenting.Value
                if
                    x.ContextType.IsOneOf(
                        typedefof<SystemContext>,
                        typedefof<FlowBlockContext>,
                        typedefof<ParentingBlockContext>
                    )
                then
                    ()
                else
                    yield! x.Names
            ]

        member x.FullName = x.NameComponents.ToArray().CombineQuoteOnDemand()

    type ObjectContextInformation with

        member x.Tuples = x.Flow, x.Parenting, x.NamedContextInformation.Names


