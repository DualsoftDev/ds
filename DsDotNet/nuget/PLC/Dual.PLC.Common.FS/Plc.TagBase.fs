namespace Dual.PLC.Common.FS

[<AbstractClass>]
type PlcTagBase(address: string, sizeBits: int) =
    let mutable value: obj = null
    let mutable writeValue: option<obj> = None

    /// 주소
    member _.Address = address

    /// 데이터 타입
    member _.DataType = PlcDataSizeType.FromBitSize(sizeBits)

    /// 현재 값
    member this.Value
        with get() = value
        and set(v) = value <- v

    /// 쓰기용 값
    member this.SetWriteValue(v: obj) = writeValue <- Some v
    member this.ClearWriteValue() = writeValue <- None
    member this.GetWriteValue() = writeValue

    abstract member UpdateValue: byte[] -> bool

    interface IPlcTag with
        member _.Name = address
        member this.DataType = this.DataType
        member _.Comment = ""

    interface IPlcTagReadWrite with
        member this.Value
            with get() = this.Value
            and set(v) = this.Value <- v
        member x.Address = x.Address
        member this.SetWriteValue(v) = this.SetWriteValue(v)
        member this.ClearWriteValue() = this.ClearWriteValue()
        member this.GetWriteValue() = this.GetWriteValue()
