namespace Engine.Obsolete.CpuUnit

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
    let CheckVaildValue (x:obj) =
        let checkedValue =
            match x with
            | :? bool   -> x
            | :? int    -> x
            | :? byte   -> x
            | :? double -> x
            | :? single -> x
            | :? string -> x
            | _ ->
                    failwith $"error {x.GetType().Name} : vaildType [ bool, byte, int, single, double, string ]"
        checkedValue

    //json type Deserialize
    let getData(dataType:string, value:string)  =
        match dataType with
        |"Boolean"-> Convert.ToBoolean(value)  |> box
        |"Int32"  -> Convert.ToInt32(value)    |> box
        |"Byte  " -> Convert.ToByte(value)     |> box
        |"Double" -> Convert.ToDouble(value)   |> box
        |"Single" -> Convert.ToSingle(value)   |> box
        |"String" -> Convert.ToDouble(value)   |> box
        |_ -> failwith "error"