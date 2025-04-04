namespace Dual.PLC.Common.FS

type ConnectState =
    | Connected | ConnectFailed | Reconnected | ReconnectFailed | Disconnected

type ConnectChangedEventArgs = 
    { Ip: string; State: ConnectState }
    
type PlcTagValueChangedEventArgs = 
    { Ip: string; Tag: IPlcTagReadWrite }

/// PLC 연결 인터페이스
type IPlcConnector =
    abstract member IpOrStation: string
    abstract member IsConnected: bool
    abstract member Connect: unit -> unit
    abstract member ReConnect: unit -> unit
    abstract member Disconnect: unit -> unit
    abstract member Read: address: string * dataType: PlcDataSizeType -> obj
    abstract member Write: address: string * dataType: PlcDataSizeType * value: obj -> bool

    abstract member ConnectChanged: IEvent<ConnectChangedEventArgs>
    abstract member TagValueChanged: IEvent<PlcTagValueChangedEventArgs>
