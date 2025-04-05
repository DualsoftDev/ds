namespace PLC.CodeGen.Common

open Dual.Common.Base.FS
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Globalization
open System
open Dual.PLC.Common.FS
open XgtProtocol


[<AutoOpen>]
module LSEAddressPattern =


    let getXgiIOTextBySize (device:string, offset: int, bitSize:int, iSlot:int, sumBit:int) : string =
        if bitSize = 1
        then $"%%{device}X0.{iSlot}.{(offset-sumBit) % 64}"  //test ahn 아날로그 base 1로 일단 고정
        else
            match bitSize with  //test ahn  xgi 규격확인
            | 8 -> $"%%{device}B{offset+1024}"
            | 16 -> $"%%{device}W{offset+1024}"
            | 32 -> $"%%{device}D{offset+1024}"
            | 64 -> $"%%{device}L{offset+1024}"
            | _ -> failwithf $"Invalid size :{bitSize}"


    let getXgiMemoryTextBySize (device:string, offset: int, bitSize:int) : string =
        if bitSize = 1
        then $"%%{device}X{offset}"
        else
            if offset%8 = 0 
            then getXgiIOTextBySize (device, offset, bitSize, 0, 0)
            else failwithf $"Word Address는 8의 배수여야 합니다. {offset}"

    type LSEAddressPatternExt =
        [<Extension>] static member IsXGIAddress (x:string) = tryParseXgiTag x |> Option.isSome
        [<Extension>] static member IsXGKAddress (x:string) = tryParseXgkTag x |> Option.isSome
