namespace PLC.Mapper.FS

open System
open System.ComponentModel
open System.Diagnostics
open System.Runtime.Serialization
open System.Text.Json.Serialization
open System.Collections.Generic

[<AutoOpen>]
module MapperTagModule = ()

    //type IMapperTag = interface end

    //type Choice =
    //    | Stage
    //    | Discarded
    //    | Chosen
    //    | Categorized

    //[<DebuggerDisplay("{Stringify()}")>]
    //[<DataContract>]
    //type PlcTerminalBase(?flow, ?device, ?action) =
    //    new () = PlcTerminalBase("", "", "") 

    //    interface IMapperTag

    //    [<DataMember>] member val FlowName   = defaultArg flow ""   with get, set
    //    [<DataMember>] member val DeviceName = defaultArg device "" with get, set
    //    [<DataMember>] member val ActionName = defaultArg action "" with get, set
    //    [<DataMember>] member val Choice     = Choice.Stage         with get, set
    //    [<JsonIgnore>] [<Browsable(false)>]
    //    member val Temporary : obj = null with get, set

    //    member x.Set(flow, device, action) =
    //        x.FlowName   <- flow
    //        x.DeviceName <- device
    //        x.ActionName <- action

    //    member x.TryGet() =
    //        if x.FlowName <> "" && x.DeviceName <> "" && x.ActionName <> "" then Some x else None

    //    member x.GetTuples() = x.FlowName, x.DeviceName, x.ActionName

    //    abstract member Stringify: unit -> string
    //    abstract member Csvify: unit -> string
    //    abstract member OnDeserialized: unit -> unit
    //    abstract member OnSerializing: unit -> unit

    //    default x.Stringify() = $"{x.FlowName}:{x.DeviceName}:{x.ActionName}"
    //    default x.Csvify()    = $"{x.FlowName},{x.DeviceName},{x.ActionName}"
    //    default x.OnDeserialized() = ()
    //    default x.OnSerializing()  = ()

    //    [<OnDeserialized>] member x.OnDeserializedMethod(_: StreamingContext) = x.OnDeserialized()
    //    [<OnSerializing>]  member x.OnSerializingMethod(_: StreamingContext)  = x.OnSerializing()

    ////[<DebuggerDisplay("{Stringify()}")>]
    ////[<DataContract>]
    ////type PlcTerminal(?variable, ?address, ?dataType, ?comment, ?outputFlag) =
    ////    inherit PlcTerminalBase()

    ////    let v = defaultArg variable ""

    ////    [<DataMember>] member val Variable    = v                            with get, set
    ////    [<DataMember>] member val Address     = defaultArg address ""        with get, set
    ////    [<DataMember>] member val DataType    = defaultArg dataType ""       with get, set
    ////    [<DataMember>] member val Comment     = defaultArg comment ""        with get, set
    ////    [<DataMember>] member val OutputFlag  = defaultArg outputFlag false  with get, set


    ////    new () = PlcTerminal()
    ////    member x.ToSystemDataType() : string option =
    ////        match x.DataType.Trim().ToLower() with
    ////        // Bool
    ////        | "boolean" | "bool" | "bit" -> Some "bool"

    ////        // Char
    ////        | "char" -> Some "char"

    ////        // Float / Real
    ////        | "float32" | "single"  -> Some "single"
    ////        | "float64" | "double" | "real" -> Some "double"

    ////        // Signed integers
    ////        | "int8"  | "sbyte"            -> Some "sByte"
    ////        | "int16" | "short"            -> Some "int16"
    ////        | "int32" | "int"              -> Some "int32"
    ////        | "int64" | "long"             -> Some "int64"

    ////        // Unsigned integers
    ////        | "uint8"  | "byte"                      -> Some "byte"
    ////        | "uint16" | "ushort" | "word"           -> Some "uInt16"
    ////        | "uint32" | "uint"   | "dword"          -> Some "uInt32"
    ////        | "uint64" | "ulong"  | "lword"          -> Some "uInt64"

    ////        // String
    ////        | "string" | "text" | "wstring" -> Some "string"

    ////        // Not recognized
    ////        | _ -> None


    ////    override x.Stringify() =
    ////        $"{x.Variable} = {base.Stringify()}, {x.Address}, {x.DataType}, {x.Comment}"

      