namespace DsXgComm

open System
open XGCommLib
open Dual.Common.Core.FS

// XGTTag 클래스
type XGTTag(tagName: string, dataTypeSize: int, bitOffset: int) =
    let mutable currentValue: obj = null // Value 필드의 백킹 필드
    let mutable writeValue: obj Option = None // writeValue 필드의 백킹 필드
    let mutable isError: obj = false;       //tag의 에러 여부 표시
    let step = 100   //스텝제어 전용 전체 스텝수
    // 속성 정의
    member x.TagName = tagName
    member x.DataType = dataTypeSize //1, 8 , 16, 32, 64 bit
    member x.Device = 
            let dev = tagName.TrimStart('%')
            if dev.StartsWith("ZR") 
            then dev.Substring(0, 2)
            else dev.Substring(0, 1)

    member x.StartByteOffset = x.LWordOffset * 8 + x.BitOffset % 64 / 8
    member x.BitOffset = 
            if tagName.StartsWith("S") 
            then 
                bitOffset / step * 16
            else bitOffset
    
    member x.LWordTag =
        if tagName.StartsWith("%") then
            $"%%{x.Device}L{x.BitOffset / 64}"
        else
            $"{x.Device}L{x.BitOffset / 64}"

    member val LWordOffset = -1 with get, set

    //for write property
    member x.MemType = if dataTypeSize = 1 then 'X'  else 'B'
    //for write property
    member x.Size = if dataTypeSize = 1 then x.BitOffset % 8 else dataTypeSize/8

    // Value 속성
    member x.Value = currentValue
    member x.Error = 
        match isError with
            | :? bool as value -> value  
            | _ -> false
    member x.SetWriteValue(value: obj) = writeValue <- Some value
    member x.ClearWriteValue() = writeValue <- None
    member x.GetWriteValue() = writeValue
    member x.setError(value: bool) = isError <- value :> obj

    // 태그의 값을 버퍼로부터 읽어오는 메서드
    member x.UpdateValue(buf: byte[]) :bool =
        let newValue =
            match x.DataType with
            | 1 -> // Bit
                if x.Device = "S" then
                    let lw = BitConverter.ToUInt16(buf, x.StartByteOffset) |> int
                    (lw = (bitOffset % step)) :> obj
                else
                    let lw = BitConverter.ToUInt64(buf, x.LWordOffset * 8)
                    (lw &&& (1UL <<< x.BitOffset%64) <> 0UL) :> obj

            | 8 -> buf.[x.StartByteOffset] :> obj // Byte
            | 16 -> BitConverter.ToUInt16(buf, x.StartByteOffset) :> obj // UInt16
            | 32 -> BitConverter.ToUInt32(buf, x.StartByteOffset) :> obj // UInt32
            | 64 -> BitConverter.ToUInt64(buf, x.StartByteOffset) :> obj // UInt64
            | _ -> failwith $"Unsupported DataTypeSize {x.DataType} for tag {x.TagName}"

        if currentValue <> newValue then
            currentValue <- newValue
            //logDebug $"Tag change detected: {x.TagName} = {newValue}"
            true
        else 
            false

    