namespace Engine.Parser.FS

open Antlr4.Runtime.Tree
open Engine.Core
open Dual.Common.Core.FS

[<AutoOpen>]
module ParserUtil =
    let mutable internal fwdLoadDevice =
        let dummy (_param: DeviceLoadParameters) : Device = failwithlog "Should be reimplemented." in dummy

    let mutable internal fwdLoadExternalSystem =
        let dummy (_param: DeviceLoadParameters) : ExternalSystem = failwithlog "Should be reimplemented." in dummy

    let mutable internal fwdParseFqdn =
        let dummy (_text: string) : string [] = failwithlog "Should be reimplemented." in dummy

    let getText (x: IParseTree) = x.GetText()

    let mutable runtimeTarget = WINDOWS
