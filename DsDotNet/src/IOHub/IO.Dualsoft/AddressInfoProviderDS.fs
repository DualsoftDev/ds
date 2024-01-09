namespace IO.Dualsoft

open System
open IO.Spec
open System.Collections.Generic

type AddressInfoProviderDS() =
    
    interface IAddressInfoProvider with
        member _.GetAddressInfo(address: string, memoryType: string byref, offset: int byref, contentBitLength: int byref): bool =

            let mutable addr = address.ToLower().TrimStart('%')
            if addr.[0] = 'i' || addr.[0] = 'q' || addr.[0] = 'm' then
                memoryType <- addr.[0].ToString()
                addr <- addr.[2..]
                offset <- int addr
                contentBitLength <-
                    match addr.[1] with
                    | 'x' -> 1
                    | 'b' -> 8
                    | 'w' -> 16
                    | 'd' -> 32
                    | 'l' -> 64
                    | _ -> raise (Exception($"Unknown content bit size: {addr.[1]}"))
                true
            else
                false

        member _.GetTagName(memoryType: string, offset: int, contentBitLength: int): string =
            let dataType =
                match contentBitLength with
                | 1 -> "x"
                | 8 -> "b"
                | 16 -> "w"
                | 32 -> "dw"
                | 64 -> "lw"
                | _ -> raise (Exception($"Unknown content bit size: {contentBitLength}"))

            $"{memoryType}{dataType}{offset}"
