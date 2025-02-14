namespace Dual.Common.Core.FS

open System
open System.Numerics
open Newtonsoft.Json
open Dual.Common.Base.FS

[<AutoOpen>]
module private ObjectHolderImpl =
    let getUInt64(value:obj) =
        match value with
        | :? int64  -> value :?> int64  |>  uint64 |> box
        | :? int32  -> value :?> int32  |>  uint64 |> box
        | :? uint32 -> value :?>            uint64 |> box
        | :? uint64 -> value :?>            uint64 |> box
        | :? bigint -> value :?> bigint |>  uint64 |> box
        | _ -> value :?>               uint64 |> box

    let getInt16(value:obj) =
        match value with
        | :? int16 -> value
        | :? int64 -> value :?> int64 |>      int16 |> box
        | :? int32 -> value :?> int32 |>      int16 |> box
        | _ -> failwith "Error"
    let getInt32(value:obj) =
        match value with
        | :? int32 -> value
        | :? int16 -> value :?> int16 |>      int32 |> box
        | :? int64 -> value :?> int64 |>      int32 |> box
        | _  -> failwith "Error"


    let getUInt16(value:obj) =
        match value with
        | :? uint16 -> value
        | :? int64 -> value :?> int64 |>      uint16 |> box
        | :? int32 -> value :?> int32 |>      uint16 |> box
        | :? int16 -> value :?> int16 |>      uint16 |> box
        | _        -> failwith "Error"

    let getUInt32(value:obj) =
        let v = value
        match v with
        | :? uint32 -> v
        | :? int64 -> v :?> int64 |>  uint32 |> box
        | :? int32 -> v :?> int32 |>  uint32 |> box
        | :? int16 -> v :?> int16 |>  uint32 |> box
        | _ -> failwith "Error"



    let getInt64(value:obj) =
        match value with
        | :? int64 -> value
        | :? int32 -> value :?> int32 |>      int64 |> box
        | _ -> failwith "Error"

    let getDecimal(value:obj) =
        match value with
        | :? decimal -> value
        | :? double  -> value :?> double |> decimal |> box
        | _  -> failwith "Error"

    let getSingle(value:obj) =
        match value with
        | :? single  -> value
        | :? double  -> value :?> double |> single |> box
        | _  -> failwith "Error"

    let getChar(value:obj) =
        match value with
        | :? string as s when s.Length = 1 -> s[0] |> box
        | :? char -> value |> box
        | _  -> failwith "Error"

    let getByte(value:obj) =
        match value with
        | :? int64 -> value :?> int64   |> byte |> box
        | :? byte -> value :?>             byte |> box
        | _  -> failwith "Error"

    ()

[<AutoOpen>]
module rec ObjectHolderTypeModule =
    type ObjectHolderType =
        | Undefined
        | Bool
        | Char
        | Byte
        | Int16
        | Int32
        | Int64
        | UInt16
        | UInt32
        | UInt64
        | Double
        | Single
        | Decimal
        | BigInt
        | String
        member x.Stringify() =
            match x with
            | ObjectHolderType.Undefined -> "Undefined"
            | ObjectHolderType.Bool      -> "Bool"
            | ObjectHolderType.Char      -> "Char"
            | ObjectHolderType.Byte      -> "Byte"
            | ObjectHolderType.Int16     -> "Int16"
            | ObjectHolderType.Int32     -> "Int32"
            | ObjectHolderType.Int64     -> "Int64"
            | ObjectHolderType.UInt16    -> "UInt16"
            | ObjectHolderType.UInt32    -> "UInt32"
            | ObjectHolderType.UInt64    -> "UInt64"
            | ObjectHolderType.Double    -> "Double"
            | ObjectHolderType.Single    -> "Single"
            | ObjectHolderType.Decimal   -> "Decimal"
            | ObjectHolderType.BigInt    -> "BigInt"
            | ObjectHolderType.String    -> "String"

        member x.CreateObjectHolder(value:string) : ObjectHolder =
            match x with
            | ObjectHolderType.Bool      -> ObjectHolder.Create(x, Parse.TryBool(value) |> Option.toNullable|> box)
            //| ObjectHolderType.Char      -> value.[0] |> box
            //| ObjectHolderType.Byte      -> byte.Parse value |> box
            //| ObjectHolderType.Int16     -> Int16.Parse value |> box
            | ObjectHolderType.Int32     -> ObjectHolder.Create(x, Parse.TryInt(value) |> Option.toNullable|> box)
            //| ObjectHolderType.Int64     -> Int64.Parse value |> box
            //| ObjectHolderType.UInt16    -> UInt16.Parse value |> box
            //| ObjectHolderType.UInt32    -> UInt32.Parse value |> box
            //| ObjectHolderType.UInt64    -> UInt64.Parse value |> box
            | ObjectHolderType.Double    -> ObjectHolder.Create(x, Parse.TryDouble(value) |> Option.toNullable|> box)
            //| ObjectHolderType.Single    -> ObjectHolder.Create(x, Parse.TrySingle(value) |> Option.toNullable|> box)
            //| ObjectHolderType.Decimal   -> decimal.Parse value |> box
            //| ObjectHolderType.BigInt    -> bigint.Parse value |> box
            | ObjectHolderType.String    -> ObjectHolder.Create(x, value |> box)
            | _ -> failwith "Error"


        member x.GetDefaultValue(): obj =
            match x with
            | ObjectHolderType.Bool      -> false |> box
            | ObjectHolderType.Char      -> '\000' |> box
            | ObjectHolderType.Byte      -> 0uy |> box
            | ObjectHolderType.Int16     -> 0s |> box
            | ObjectHolderType.Int32     -> 0 |> box
            | ObjectHolderType.Int64     -> 0L |> box
            | ObjectHolderType.UInt16    -> 0us |> box
            | ObjectHolderType.UInt32    -> 0u |> box
            | ObjectHolderType.UInt64    -> 0UL |> box
            | ObjectHolderType.Double    -> 0.0 |> box
            | ObjectHolderType.Single    -> 0.0f |> box
            | ObjectHolderType.Decimal   -> 0.0M |> box
            | ObjectHolderType.BigInt    -> 0I |> box
            | ObjectHolderType.String    -> "" |> box
            | _ -> failwith "Error"

        member x.ToSystemType(): Type =
            match x with
            | ObjectHolderType.Bool      -> typedefof<bool>
            | ObjectHolderType.Char      -> typedefof<char>
            | ObjectHolderType.Byte      -> typedefof<byte>
            | ObjectHolderType.Int16     -> typedefof<int16>
            | ObjectHolderType.Int32     -> typedefof<int32>
            | ObjectHolderType.Int64     -> typedefof<int64>
            | ObjectHolderType.UInt16    -> typedefof<uint16>
            | ObjectHolderType.UInt32    -> typedefof<uint32>
            | ObjectHolderType.UInt64    -> typedefof<uint64>
            | ObjectHolderType.Double    -> typedefof<double>
            | ObjectHolderType.Single    -> typedefof<single>
            | ObjectHolderType.Decimal   -> typedefof<decimal>
            | ObjectHolderType.BigInt    -> typedefof<bigint>
            | ObjectHolderType.String    -> typedefof<string>
            | _ -> failwith "Error"


        static member FromSystemType(typ:Type): ObjectHolderType =
            match typ.Name.ToLower() with
            | "boolean" -> ObjectHolderType.Bool
            | "char"   -> ObjectHolderType.Char
            | "byte"   -> ObjectHolderType.Byte
            | "int16"  -> ObjectHolderType.Int16
            | "int32"  -> ObjectHolderType.Int32
            | "int64"  -> ObjectHolderType.Int64
            | "uint16" -> ObjectHolderType.UInt16
            | "uint32" -> ObjectHolderType.UInt32
            | "uint64" -> ObjectHolderType.UInt64
            | "double" -> ObjectHolderType.Double
            | "single" -> ObjectHolderType.Single
            | "decimal"-> ObjectHolderType.Decimal
            | "bigint" -> ObjectHolderType.BigInt
            | "string" -> ObjectHolderType.String
            | _ -> failwith "Error"

        static member GetTypeFromObj(value:obj) =
            match value with
            | :? bool -> ObjectHolderType.Bool
            | :? char -> ObjectHolderType.Char
            | :? Int16 -> ObjectHolderType.Int16
            | :? Int32 -> ObjectHolderType.Int32
            | :? Int64 -> ObjectHolderType.Int64
            | :? byte -> ObjectHolderType.Byte
            | :? UInt16 -> ObjectHolderType.UInt16
            | :? UInt32 -> ObjectHolderType.UInt32
            | :? UInt64 -> ObjectHolderType.UInt64
            | :? double -> ObjectHolderType.Double
            | :? Single -> ObjectHolderType.Single
            | :? bigint -> ObjectHolderType.BigInt
            | :? decimal -> ObjectHolderType.Decimal
            | :? string -> ObjectHolderType.String
            | null -> ObjectHolderType.Undefined
            | _ -> failwith "ERROR"

        member x.GetTrueValue(): obj =
            match x with
            | ObjectHolderType.Bool      -> true |> box
            //| ObjectHolderType.Char      -> '\0' |> box
            | ObjectHolderType.Byte      -> 1uy |> box
            | ObjectHolderType.Int16     -> 1s |> box
            | ObjectHolderType.Int32     -> 1 |> box
            | ObjectHolderType.Int64     -> 1L |> box
            | ObjectHolderType.UInt16    -> 1us |> box
            | ObjectHolderType.UInt32    -> 1u |> box
            | ObjectHolderType.UInt64    -> 1UL |> box
            | ObjectHolderType.Double    -> 1.0 |> box
            | ObjectHolderType.Single    -> 1.0f |> box
            | ObjectHolderType.Decimal   -> 1.0M |> box
            | ObjectHolderType.BigInt    -> 1I |> box
            | ObjectHolderType.String    -> "T" |> box
            | _ -> failwith "Error"

        static member TryParse(value:string) : ObjectHolderType option =
            match value.ToLower() with
            //| "undefined" -> Some ObjectHolderType.Undefined
            | "bool"      -> Some ObjectHolderType.Bool
            | "char"      -> Some ObjectHolderType.Char
            | "byte"      -> Some ObjectHolderType.Byte
            | "int16"     -> Some ObjectHolderType.Int16
            | ("int"|"int32") -> Some ObjectHolderType.Int32
            | "int64"     -> Some ObjectHolderType.Int64
            | "uint16"    -> Some ObjectHolderType.UInt16
            | "uint32"    -> Some ObjectHolderType.UInt32
            | "uint64"    -> Some ObjectHolderType.UInt64
            | "double"    -> Some ObjectHolderType.Double
            | "single"    -> Some ObjectHolderType.Single
            | "decimal"   -> Some ObjectHolderType.Decimal
            | "bigInt"    -> Some ObjectHolderType.BigInt
            | "string"    -> Some ObjectHolderType.String
            | _           -> failwith "Error"


    /// Object 를 serialize / deserialize 할 때, 해당 object 의 type 을 유지하기 위한 wrapper class
    /// Newtonsoft.Json 을 이용 (System.Text.Json 은 동작하지 않는다)
    ///
    /// see UnitTest.Common/ObjectHolderTest.fs
    ///
    type ObjectHolder private (value:obj, typ:ObjectHolderType) =

        [<JsonConstructor>] private new () = ObjectHolder(null, ObjectHolderType.Undefined)

        [<JsonProperty>] member val private _rawValue = value with get, set

        //[<JsonIgnore>]
        //member x.RawValue
        //    with get() = x._rawValue
        //    and set v =
        //        if ObjectHolderType.GetTypeFromObj v <> x.Type then
        //            failwith "ERROR: Type mismatch."
        //        x._rawValue <- v

        // setter for JSON
        [<JsonProperty>] member val Type = typ with get, set

        [<JsonIgnore>] member x.Value with get() = x.GetValue() and set v = x.SetValue v


        member x.SetValue(v:obj) =
            if ObjectHolderType.GetTypeFromObj v <> x.Type then
                failwith "ERROR: Type mismatch."
            x._rawValue <- v

        member x.GetValue(): obj =
            let v = x._rawValue
            if v = null then
                null
            else
                match x.Type with
                | ObjectHolderType.Bool      -> v :?> bool |> box
                | ObjectHolderType.Char      -> v |> getChar
                | ObjectHolderType.Byte      -> v |> getByte
                | ObjectHolderType.Int16     -> v |> getInt16
                | ObjectHolderType.Int32     -> v |> getInt32
                | ObjectHolderType.Int64     -> v |> getInt64
                | ObjectHolderType.UInt16    -> v |> getUInt16
                | ObjectHolderType.UInt32    -> v |> getUInt32
                | ObjectHolderType.UInt64    -> v |> getUInt64
                | ObjectHolderType.Double    -> v :?> double |> box
                | ObjectHolderType.Single    -> v |> getSingle
                | ObjectHolderType.Decimal   -> v |> getDecimal
                | ObjectHolderType.BigInt    -> v :?> bigint |> box
                | ObjectHolderType.String    -> v :?> string |> box
                | ObjectHolderType.Undefined when isNull v  -> null
                | _ -> failwith "Error"

        static member CreateFromObject(value:obj) =
            let valueType = ObjectHolderType.GetTypeFromObj(value)
            ObjectHolder(value, valueType)

        static member Create(typ:ObjectHolderType, value:obj) = ObjectHolder(value, typ)
        static member Create(typ:Type, value:obj) = ObjectHolder(value, ObjectHolderType.FromSystemType typ)

        //member x.Serialize():string = JsonConvert.SerializeObject x
        //static member Deserialize(str:string) = JsonConvert.DeserializeObject<ObjectHolder> str
        member x.Serialize():string = EmJson.ToJson x
        static member Deserialize(str:string) = EmJson.FromJson<ObjectHolder> str

