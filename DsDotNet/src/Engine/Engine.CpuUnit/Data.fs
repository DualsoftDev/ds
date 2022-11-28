namespace Engine.Cpu

open System.Collections.Concurrent
open System.Diagnostics
open System

[<AutoOpen>]
module DataModule =

    [<DebuggerDisplay("{Data}")>]
    type Data<'T>(data:'T) = 
        interface IData 
        member val Data = data  with get, set
        override x.ToString() = data.ToString()

      //지원 value type : bool, int, byte, single, double, string
      //미지원 value type : uint, int64, ... 지원 기준외 등등
    let ToData (x:obj) = 
        match x with
        | :? bool   as ds -> Data(ds) :> IData
        | :? int    as ds -> Data(ds) :> IData
        | :? byte   as ds -> Data(ds) :> IData
        | :? double as ds -> Data(ds) :> IData
        | :? single as ds -> Data(ds) :> IData
        | :? string as ds -> Data(ds) :> IData
        | _ ->
                failwith $"error {x.GetType().Name} : vaildType [ bool, byte, int, single, double, string ]" 

    let ToValue (x:obj) =
        match x with
        | :? Data<bool>     as ds -> ds.Data |> unbox
        | :? Data<int>      as ds -> ds.Data |> unbox
        | :? Data<byte>     as ds -> ds.Data |> unbox
        | :? Data<double>   as ds -> ds.Data |> unbox
        | :? Data<single>   as ds -> ds.Data |> unbox
        | :? Data<string>   as ds -> ds.Data |> unbox
        | _ ->
                failwith "error" 

    //json type Deserialize
    let getData(dataType:string, value:string)  = 
        match dataType with
        |"Boolean"-> Convert.ToBoolean(value) |> ToData
        |"Int32"  -> Convert.ToInt32(value)   |> ToData
        |"Byte  " -> Convert.ToByte(value)    |> ToData
        |"Double" -> Convert.ToDouble(value)  |> ToData
        |"Single" -> Convert.ToSingle(value)  |> ToData
        |"String" -> Convert.ToDouble(value)  |> ToData
        |_ -> failwith "error"