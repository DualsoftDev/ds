namespace Engine.Parser.FS

open Antlr4.Runtime.Tree
open Engine.Core

[<AutoOpen>]
module ParserUtil =
    let dummyDeviceLoader (param:DeviceLoadParameters) : Device =
        failwith "Should be reimplemented."

    let dummyExternalSystemLoader (param:DeviceLoadParameters) : ExternalSystem =
        failwith "Should be reimplemented."

    let dummyParserFqdn (text:string) : string list =
        failwith "Should be reimplemented."

    let mutable fwdLoadDevice = dummyDeviceLoader
    let mutable fwdLoadExternalSystem = dummyExternalSystemLoader
    let mutable fwdParseFqdn = dummyParserFqdn
    //let mutable fwdParseExpression = dummyParserFqdn

    let getText (x:IParseTree) = x.GetText()

