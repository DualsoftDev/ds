namespace PLC.CodeGen.LSXGI
open System
open System.Collections.Generic
open Engine.Common.FS

[<AutoOpen>]
module XgiPrologModule =
    /// name -> comment -> device -> kind -> address -> plcType -> SymbolInfo
    let mutable fwdCreateSymbol =
        let dummy (name:string) (comment:string) (device:string) (kind:int) (address:string) (plcType:string) : SymbolInfo =
            failwith "Should be reimplemented."
        dummy

