namespace Engine.Parser.FS

open Antlr4.Runtime.Tree
open Engine.Core

[<AutoOpen>]
module ParserUtil =
    let mutable internal fwdLoadDevice         = let dummy (param:DeviceLoadParameters) : Device =         failwith "Should be reimplemented." in dummy
    let mutable internal fwdLoadExternalSystem = let dummy (param:DeviceLoadParameters) : ExternalSystem = failwith "Should be reimplemented." in dummy
    let mutable internal fwdParseFqdn          = let dummy (text:string) : string list =                   failwith "Should be reimplemented." in dummy

    let getText (x:IParseTree) = x.GetText()

