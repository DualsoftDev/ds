namespace Dual.PLC.Common.FS

open System

type PlcTerminal
    (
        tag: PlcTagBase,
        ?terminalType: TerminalType
    ) =

    let terminalTypeRaw = defaultArg terminalType TerminalType.Empty

    /// 내부 태그 객체
    member _.Tag = tag

    /// 접점/코일 타입
    member _.TerminalType = terminalTypeRaw
    member _.Address    = tag.Address
    member _.Name       = tag.Name
    member _.DataType   = tag.DataType

    /// 기본 출력 형식
    override _.ToString() =
        $"[{tag.ReadWriteType}] {tag.Name} @ {tag.Address} ({tag.DataType}) <{terminalTypeRaw}>"
