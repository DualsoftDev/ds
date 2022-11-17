namespace Engine.Parser.FS

open Antlr4.Runtime.Tree
open Engine.Core

[<AutoOpen>]
module ParserUtil =
    let dummyDeviceLoader (system:DsSystem) (absoluteFilePath:string, simpleFilePath:string) (loadedName:string) : Device =
        failwith "Should be reimplemented."

    let dummyExternalSystemLoader (system:DsSystem) (absoluteFilePath:string, simpleFilePath:string) (loadedName:string) : ExternalSystem =
        failwith "Should be reimplemented."

    let mutable fwdLoadDevice = dummyDeviceLoader
    let mutable fwdLoadExternalSystem = dummyExternalSystemLoader

    let getText (x:IParseTree) = x.GetText()

