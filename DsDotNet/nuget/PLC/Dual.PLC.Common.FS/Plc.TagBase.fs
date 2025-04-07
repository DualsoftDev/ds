namespace Dual.PLC.Common.FS

open System

[<AbstractClass>]
type PlcTagBase(name: string, address: string, dataType: PlcDataSizeType, 
                ?comment: string, ?initialValue: obj) =

    do
        match initialValue with
        | Some v ->
            let systemType = PlcTagExt.ToSystemDataType(v.GetType().Name)
            if systemType <> dataType then
                printfn $"⚠️ 초기값 타입 불일치: {v.GetType().Name} ≠ {dataType}"  // 로그만 출력
        | _ -> ()

    let mutable value     = defaultArg initialValue null
    let mutable writeVal  = None: obj option

    
    member _.Name     = name
    member _.Address  = address
    member _.DataType = dataType
    
    member this.Value
        with get() = value
        and set(v) = value <- v

    member _.Comment  = defaultArg comment ""

    member this.SetWriteValue(v: obj) = writeVal <- Some v
    member this.ClearWriteValue()     = writeVal <- None
    member this.GetWriteValue()       = writeVal

    abstract member ReadWriteType: ReadWriteType
    abstract member UpdateValue: byte[] -> bool
    default _.UpdateValue _ = false

    override this.ToString() =
        $"[{this.ReadWriteType}] {name} @ {address} ({dataType})"
