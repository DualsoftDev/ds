namespace PLC.CodeGen.Common
open Engine.Core

[<AutoOpen>]
module MemoryAllocator =
    /// Unit -> address string 을 반환하는 함수 type
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
    /// availableByteRange: 할당 가능한 [시작, 끝] byte 의 range (reservedBytes 에 포함된 부분은 제외됨)
    /// reservedBytes: 회피 영역 - todo
    let createMemoryAllocator (typ:string) (availableByteRange:int*int) (reservedBytes:int list) : PLCMemoryAllocator =
        let startByte, endByte = availableByteRange
        /// optional fragmented bit position
        let mutable ofBit:int option = None  // Some (startByte * 8)
        /// optional framented byte [start, end) position
        let mutable ofByteRange:(int*int) option = None
        let mutable byteCursor = startByte

        let getAddress (reqMemType:char) =
            match reqMemType with
            | 'X' ->
                let bitIndex =
                    match ofBit, ofByteRange with
                    | Some bit, _ when bit % 8 = 7 ->   // 마지막 fragment bit 을 쓰는 상황
                        ofBit <- None
                        bit
                    | Some bit, _ ->                    // 마지막이 아닌 여유 fragment bit 을 쓰는 상황
                        ofBit <- Some (bit + 1)
                        bit
                    | None, Some (s, e) ->
                        let bit = s * 8
                        ofBit <- Some (bit + 1)
                        ofByteRange <- if s = e then None else Some(s+1, e)
                        bit
                    | None, None ->
                        let bit = byteCursor * 8
                        ofBit <- Some (bit + 1)
                        byteCursor <- byteCursor + 1
                        bit
                if bitIndex / 8 > endByte then
                    failwith "ERROR: Limit exceeded."

                $"%%{typ}{reqMemType}{bitIndex}"


            | ('B' | 'W' | 'D' | 'L') ->
                let byteSize =
                    match reqMemType with
                    | 'B' -> 1
                    | 'W' -> 2
                    | 'D' -> 4
                    | 'L' -> 8
                    | _ -> failwith "ERROR"
                let byteIndex =
                    match ofByteRange with
                    | Some (fs, fe) when (fe - fs) > byteSize ->     // fragmented bytes 로 해결하고도 남는 상황
                        ofByteRange <- Some (fs + byteSize, fe)
                        fs
                    | Some (fs, fe) when (fe - fs) = byteSize ->     // fragmented bytes 를 전부 써서 해결 가능한 상황
                        ofByteRange <- None
                        fs
                    | _ ->                                           // fragmented bytes 로 부족한 상황.  fragment 는 건드리지 않고 새로운 영역에서 할당
                        let byte =
                            if byteCursor % byteSize = 0 then
                                let byte = byteCursor
                                byteCursor <- byteCursor + byteSize
                                byte
                            else
                                let newPosition = (byteCursor + byteSize) / byteSize * byteSize
                                ofByteRange <- Some (byteCursor, newPosition)
                                byteCursor <- newPosition + byteSize
                                newPosition
                        //ofByte <- Some (byte + byteSize)
                        byte
                if byteIndex + byteSize > endByte then
                    failwith "ERROR: Limit exceeded."

                $"%%{typ}{reqMemType}{byteIndex/byteSize}"
            | _ ->
                failwith "ERROR"
        {
            BitAllocator  = fun () -> getAddress 'X'
            ByteAllocator = fun () -> getAddress 'B'
            WordAllocator = fun () -> getAddress 'W'
            DWordAllocator= fun () -> getAddress 'D'
            LWordAllocator= fun () -> getAddress 'L'
        }


    type System.Type with
        member x.GetByteSize() =
            match x.Name with
            | BOOL    -> failwith "ERROR"
            | CHAR    -> 1
            | FLOAT32 -> 4
            | FLOAT64 -> 8
            | INT16   -> 2
            | INT32   -> 4
            | INT64   -> 8
            | INT8    -> 1
            | STRING  -> failwith "ERROR"
            | UINT16  -> 2
            | UINT32  -> 4
            | UINT64  -> 8
            | UINT8   -> 1
            | _  -> failwith "ERROR"

        member x.GetMemorySizePrefix() =
            if x = typedefof<bool> then
                'X'
            else
                match x.GetByteSize() with
                | 1 -> 'B'
                | 2 -> 'W'
                | 4 -> 'D'
                | 8 -> 'L'
                | _ -> failwith "ERROR"