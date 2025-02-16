namespace Dual.PLC.Common.FS

open System
open System.Runtime.CompilerServices

[<AutoOpen>]
module Connect =

    type ConnectState =
        | Connected
        | ConnectFailed
        | Reconnected
        | ReconnectFailed
        | Disconnected

    type ConnectChangedEventArgs = 
        { 
            Ip: string
            State: ConnectState 
        }
