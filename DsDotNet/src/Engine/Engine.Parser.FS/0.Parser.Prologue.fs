namespace Engine.Parser.FS

open System.Collections.Generic
open System.Diagnostics

open Engine.Core
open Engine.Common.FS
open type Engine.Parser.dsParser
open Antlr4.Runtime.Tree

[<AutoOpen>]
module ParserUtil =
    let dummyDeviceLoader (theSystem:DsSystem) (absoluteFilePath:string, simpleFilePath:string) (loadedName:string) : Device =
        failwith "Should be reimplemented."

    let dummyExternalSystemLoader (theSystem:DsSystem) (absoluteFilePath:string, simpleFilePath:string) (loadedName:string) : ExternalSystem =
        failwith "Should be reimplemented."

    let mutable fwdLoadDevice = dummyDeviceLoader
    let mutable fwdLoadExternalSystem = dummyExternalSystemLoader

    let getText (x:IParseTree) = x.GetText()

