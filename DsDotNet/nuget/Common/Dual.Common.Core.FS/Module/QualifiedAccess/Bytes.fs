namespace Dual.Common.Core.FS

open System
open Microsoft.FSharp.Reflection
open System.Collections.Generic
open System.Runtime.CompilerServices
open Dual.Common

[<AutoOpen>]
module BytesExtModule =     // Zmq.Client.fs 에서 사용
    /// toggle 값이 Some true 이면 주어진 byte array 를 toggle
    let tog toggle bs =
        match toggle with
        | Some true -> bs |> Array.rev
        | _ -> bs
    let (|?) = defaultArg
    let reverseBytesOnDemand (reverse:bool) (bytes:byte[]) =
        if reverse then Array.rev bytes else bytes

    /// bytes를 m bytes 씩 모아서 m bytes 내에서 byte 순서를 뒤집은 후, 전체 하나의 byte array 로 반환하는 함수
    let reverseBytesInChunks (bytes: byte[]) chunkSize =
        bytes
        |> Seq.chunkBySize chunkSize
        |> Seq.map Array.rev
        |> Seq.concat
        |> Array.ofSeq


[<Extension>]
type BytesExt =
    [<Extension>] static member ToUInt16(bs, ?startIndex, ?toggleEndian) = BitConverter.ToUInt16(bs |> tog toggleEndian, startIndex |? 0)
    [<Extension>] static member ToUInt32(bs, ?startIndex, ?toggleEndian) = BitConverter.ToUInt32(bs |> tog toggleEndian, startIndex |? 0)
    [<Extension>] static member ToUInt64(bs, ?startIndex, ?toggleEndian) = BitConverter.ToUInt64(bs |> tog toggleEndian, startIndex |? 0)

    [<Extension>] static member ToUInt16((b0, b1)) = BitConverter.ToUInt16([|b0; b1|], 0)
    [<Extension>] static member ToUInt32((b0, b1, b2, b3)) = BitConverter.ToUInt32([|b0; b1; b2; b3|], 0)
    [<Extension>] static member ToUInt64((b0, b1, b2, b3, b4, b5, b6, b7)) = BitConverter.ToUInt64([|b0; b1; b2; b3; b4; b5; b6; b7|], 0)


    [<Extension>] static member ToBytes(n:uint16, ?toggleEndian) = BitConverter.GetBytes(n) |> tog toggleEndian
    [<Extension>] static member ToBytes(n:uint32, ?toggleEndian) = BitConverter.GetBytes(n) |> tog toggleEndian
    [<Extension>] static member ToBytes(n:uint64, ?toggleEndian) = BitConverter.GetBytes(n) |> tog toggleEndian



    [<Extension>] static member ToggleEndian(n:uint16) = BitConverter.ToUInt16(BitConverter.GetBytes(n) |> Array.rev, 0);
    [<Extension>] static member ToggleEndian(n:uint32) = BitConverter.ToUInt32(BitConverter.GetBytes(n) |> Array.rev, 0);
    [<Extension>] static member ToggleEndian(n:uint64) = BitConverter.ToUInt64(BitConverter.GetBytes(n) |> Array.rev, 0);

[<RequireQualifiedAccess>]
module Bytes =
    let fromUInt16(n:uint16) = BitConverter.GetBytes(n)
    let toUInt16 bs         = BitConverter.ToUInt16(bs, 0)
    let toUInt32 (b0, b1, b2, b3) = BitConverter.ToUInt32([|b0; b1; b2; b3|], 0)


    let getString() = (None:string option)
    let test = getString() |? "This will be used if the result of getString() is None.";;

    //let x = Some 1
    //x |? 2

    type ByteConverter() =
        static member ToBytes<'T> (value: 'T, reverse:bool) =
            match box value with
            | :? byte as v -> [| v |] // 이미 바이트이므로 배열로 변환
            | :? uint16 as v -> System.BitConverter.GetBytes(v) |> reverseBytesOnDemand reverse
            | :? uint32 as v -> System.BitConverter.GetBytes(v) |> reverseBytesOnDemand reverse
            | :? uint64 as v -> System.BitConverter.GetBytes(v) |> reverseBytesOnDemand reverse
            | :? int32  as v -> System.BitConverter.GetBytes(v) |> reverseBytesOnDemand reverse
            | _ -> failwithf "Type %O is not supported" typeof<'T>

        static member ToBytes<'T> (value: 'T) = ByteConverter.ToBytes<'T>(value, false)

        static member ToBytes<'T> (values: 'T[], reverse:bool) =
            values
            |> Array.collect (fun value ->
                match box value with
                | :? byte as v -> [| v |] // 이미 바이트이므로 배열로 변환
                | :? uint16 as v -> System.BitConverter.GetBytes(v) |> reverseBytesOnDemand reverse
                | :? uint32 as v -> System.BitConverter.GetBytes(v) |> reverseBytesOnDemand reverse
                | :? uint64 as v -> System.BitConverter.GetBytes(v) |> reverseBytesOnDemand reverse
                | :? int32  as v -> System.BitConverter.GetBytes(v) |> reverseBytesOnDemand reverse
                | _ -> failwithf "Type %O is not supported" typeof<'T>)
        static member ToBytes<'T> (values: 'T[]) = ByteConverter.ToBytes<'T>(values, false)

        static member BytesToTypeArray<'T> (bytes: byte[], reverse:bool) : 'T[] =
            if sizeof<'T> = 0 || bytes.Length % sizeof<'T> <> 0 then
                failwithf "The length of the byte array should be a multiple of %d for %O conversion." sizeof<'T> typeof<'T>

            let bytes = if reverse then reverseBytesInChunks bytes sizeof<'T> else bytes
            Array.init (bytes.Length / sizeof<'T>) (fun i ->
                let value : obj =
                    match typedefof<'T> with
                    | t when t = typedefof<byte>   -> box (bytes[i])
                    | t when t = typedefof<uint16> -> box (System.BitConverter.ToUInt16(bytes, i * sizeof<'T>))
                    | t when t = typedefof<int32>  -> box (System.BitConverter.ToInt32(bytes, i * sizeof<'T>))
                    | t when t = typedefof<uint32> -> box (System.BitConverter.ToUInt32(bytes, i * sizeof<'T>))
                    | t when t = typedefof<uint64> -> box (System.BitConverter.ToUInt64(bytes, i * sizeof<'T>))
                    | _ -> failwithf "Type %O is not supported for conversion from bytes" typeof<'T>
                value)
            |> Array.map (fun v -> unbox<'T> v)  // unbox to the target type

        static member BytesToTypeArray<'T> (bytes: byte[]) : 'T[] = ByteConverter.BytesToTypeArray<'T>(bytes, false)
