namespace Engine.Parser.FS

open Antlr4.Runtime.Tree
open Engine.Core

[<AutoOpen>]
module ParserUtil =
    let private dummyDeviceLoader (param:DeviceLoadParameters) : Device =
        failwith "Should be reimplemented."

    let private dummyExternalSystemLoader (param:DeviceLoadParameters) : ExternalSystem =
        failwith "Should be reimplemented."

    let private dummyParserFqdn (text:string) : string list =
        failwith "Should be reimplemented."

    let mutable internal fwdLoadDevice = dummyDeviceLoader
    let mutable internal fwdLoadExternalSystem = dummyExternalSystemLoader
    let mutable internal fwdParseFqdn = dummyParserFqdn
    //let mutable fwdParseExpression = dummyParserFqdn

    let getText (x:IParseTree) = x.GetText()

