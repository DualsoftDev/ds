namespace PLC.CodeGen.Common

[<AutoOpen>]
module MemoryAllocator =

    type PLCMemoryAllocatorType = unit -> string
    type PLCMemoryAllocator = {
        BitAllocator  : PLCMemoryAllocatorType
        ByteAllocator : PLCMemoryAllocatorType
        WordAllocator : PLCMemoryAllocatorType
        DWordAllocator: PLCMemoryAllocatorType
        LWordAllocator: PLCMemoryAllocatorType
    }

    /// 주어진 memory type 에서 주소를 할당하 하는 함수 제공
    /// typ: {"M", "I", "Q"} 등이 가능하나 주로 "M"
    /// availableByteRange: 할당 가능한 [시작, 끝] byte 의 range
    let createMemoryAllocator (typ:string) (availableByteRange:int*int) : PLCMemoryAllocator =
        let startByte, endByte = availableByteRange
        /// optional fragmented bit Position
        let mutable ofBit:int option = None  // Some (startByte * 8)
        /// optional framented byte Position
        let mutable ofByte:(int*int) option = None
        let mutable byteIndex = startByte
        let getAddress (reqMemType:char) =
            match reqMemType with
            | 'X' ->
                let bit =
                    match ofBit, ofByte with
                    | Some bit, _ when bit % 8 = 7 ->   // 마지막 fragment bit 을 쓰는 상황
                        ofBit <- None
                        bit
                    | Some bit, _ ->                    // 마지막이 아닌 여유 fragment bit 을 쓰는 상황
                        ofBit <- Some (bit + 1)
                        bit
                    | None, Some (s, e) ->
                        let bit = s * 8
                        ofBit <- Some (bit + 1)
                        ofByte <- if s = e then None else Some(s+1, e)
                        bit
                    | None, None ->
                        let bit = byteIndex * 8
                        ofBit <- Some (bit + 1)
                        byteIndex <- byteIndex + 1
                        bit
                if bit / 8 > endByte then
                    failwith "ERROR: Limit exceeded."

                $"%%{typ}{reqMemType}{bit}"


            | ('B' | 'W' | 'D' | 'L') ->
                let byteSize =
                    match reqMemType with
                    | 'B' -> 1
                    | 'W' -> 2
                    | 'D' -> 4
                    | 'L' -> 8
                    | _ -> failwith "ERROR"
                let byte =
                    match ofByte with
                    | Some (fs, fe) when (fe - fs) > byteSize ->     // fragmented bytes 로 해결하고도 남는 상황
                        ofByte <- Some (fs + byteSize, fe)
                        fs
                    | Some (fs, fe) when (fe - fs) = byteSize ->     // fragmented bytes 를 전부 써서 해결 가능한 상황
                        ofByte <- None
                        fs
                    | _ ->                                           // fragmented bytes 로 부족한 상황.  fragment 는 건드리지 않고 새로운 영역에서 할당
                        let byte =
                            if byteIndex % byteSize = 0 then
                                let byte = byteIndex
                                byteIndex <- byteIndex + byteSize
                                byte
                            else
                                let newPosition = (byteIndex + byteSize) / byteSize * byteSize
                                ofByte <- Some (byteIndex, newPosition)
                                byteIndex <- newPosition + byteSize
                                newPosition
                        //ofByte <- Some (byte + byteSize)
                        byte
                if byte + byteSize > endByte then
                    failwith "ERROR: Limit exceeded."

                $"%%{typ}{reqMemType}{byte/byteSize}"
            | _ ->
                failwith "ERROR"
        {
            BitAllocator  = fun () -> getAddress 'X'
            ByteAllocator = fun () -> getAddress 'B'
            WordAllocator = fun () -> getAddress 'W'
            DWordAllocator= fun () -> getAddress 'D'
            LWordAllocator= fun () -> getAddress 'L'
        }
