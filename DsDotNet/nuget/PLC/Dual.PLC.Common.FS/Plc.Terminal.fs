namespace Dual.PLC.Common.FS

open System

type PlcTerminal
    (
        ?name: string,
        ?address: string,
        ?dataType: PlcDataSizeType,
        ?comment: string,
        ?outputFlag: bool,
        ?initialValue: obj,
        ?terminalType: TerminalType
    ) =

    // 내부 상태
    let mutable value = defaultArg initialValue null
    let mutable writeValue: option<obj> = None

    let nameRaw       = defaultArg name ""
    let addressRaw    = defaultArg address ""
    let dataTypeRaw   = defaultArg dataType PlcDataSizeType.Bit
    let commentRaw    = defaultArg comment ""
    let outputFlag    = defaultArg outputFlag false

    new () = PlcTerminal()


    interface IPlcTag with
        member _.Name = nameRaw
        member this.DataType = dataTypeRaw
        member _.Comment = commentRaw

    interface IPlcTagReadWrite with
        member _.Address = addressRaw
        member _.Value
            with get() = value
            and set(v) = value <- v
        member _.SetWriteValue(v) = writeValue <- Some v
        member _.ClearWriteValue() = writeValue <- None
        member _.GetWriteValue() = writeValue

    interface IPlcTerminal with
        member _.TerminalType = defaultArg terminalType TerminalType.Empty


    member x.Name         = (x:>IPlcTerminal).Name
    member x.Address      = (x:>IPlcTerminal).Address
    member x.DataType     = (x:>IPlcTerminal).DataType
    member x.TerminalType = (x:>IPlcTerminal).TerminalType
    member x.OutputFlag   = outputFlag


