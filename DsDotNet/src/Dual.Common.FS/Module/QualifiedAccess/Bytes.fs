namespace Dual.Common

open System
open Microsoft.FSharp.Reflection
open System.Collections.Generic
open System.Runtime.CompilerServices
open Dual.Common

module private BytesExtModule =
    /// toggle 값이 Some true 이면 주어진 byte array 를 toggle
    let tog toggle bs =
        match toggle with
        | Some true -> bs |> Array.rev
        | _ -> bs



open BytesExtModule

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



    [<Extension>] static member ToggleEndian(n:uint16) = BitConverter.ToUInt16(BitConverter.GetBytes(n).GetReversed(), 0);
    [<Extension>] static member ToggleEndian(n:uint32) = BitConverter.ToUInt32(BitConverter.GetBytes(n).GetReversed(), 0);
    [<Extension>] static member ToggleEndian(n:uint64) = BitConverter.ToUInt64(BitConverter.GetBytes(n).GetReversed(), 0);

[<RequireQualifiedAccess>]
module Bytes =
    let fromUInt16(n:uint16) = BitConverter.GetBytes(n)
    let toUInt16 bs         = BitConverter.ToUInt16(bs, 0)
    let toUInt32 (b0, b1, b2, b3) = BitConverter.ToUInt32([|b0; b1; b2; b3|], 0)


    let getString() = (None:string option) 
    let test = getString() |? "This will be used if the result of getString() is None.";;

    //let x = Some 1
    //x |? 2


