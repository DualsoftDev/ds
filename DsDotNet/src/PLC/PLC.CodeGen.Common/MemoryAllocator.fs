module MemoryAllocator

open PLC.CodeGen.Common.NewIEC61131

let createMemoryAllocator (typ:string) (availableByteRange:int*int) =
    let startByte, endByte = availableByteRange
    /// optional fragmented bit Position
    let mutable ofBit:int option = None  // Some (startByte * 8)
    /// optional framented byte Position
    let mutable ofByte:int option = None
    let mutable byteIndex = startByte
    let rec getAddress (reqMemType:Size) =
        let adjustFragmentedByte() =
            match ofByte with
            | Some index ->
                if index = byteIndex - 1 then
                    ofByte <- None
                elif index < byteIndex - 1 then
                    ofByte <- Some (byteIndex - 1)
                else
                    failwith "ERROR"
            | None -> ()

        match reqMemType with
        | X ->
            let bit =
                match ofBit, ofByte with
                | Some bit, _ when bit % 8 = 7 ->
                    ofBit <- None
                    bit
                | Some bit, _ ->
                    ofBit <- Some (bit + 1)
                    bit
                | None, Some byte ->
                    let bit = byte * 8
                    ofBit <- Some (bit + 1)
                    adjustFragmentedByte()
                    bit
                | None, None ->
                    let bit = byteIndex * 8
                    ofBit <- Some (bit + 1)
                    byteIndex <- byteIndex + 1
                    bit
            $"%%{typ}{reqMemType}{bit}"


        | (B | Size.W | D | L) ->
            let byteSize = reqMemType.ToInteger() / 8
            let byte =
                match ofByte with
                | Some fByte when (byteIndex - fByte) > byteSize ->
                    ofByte <- Some (fByte + byteSize)
                    fByte
                | Some fByte when (byteIndex - fByte) = byteSize ->
                    ofByte <- None
                    byteIndex <- byteIndex + 1
                    fByte
                | _ ->
                    let byte =
                        if byteIndex % byteSize = 0 then
                            let byte = byteIndex
                            byteIndex <- byteIndex + byteSize
                            byte
                        else
                            ofByte <- Some byteIndex
                            byteIndex <- (byteIndex + byteSize) / byteSize * byteSize
                            byteIndex
                    //ofByte <- Some (byte + byteSize)
                    byte
            $"%%{typ}{reqMemType}{byte/byteSize}"
    getAddress