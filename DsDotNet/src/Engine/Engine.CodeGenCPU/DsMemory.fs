namespace Engine.CodeGenCPU

open System.Diagnostics
open System
open System.Text.RegularExpressions
open Engine.Core

[<AutoOpen>]
module DsMemoryModule =

    /// DsMemory DsBit/DsDotBit 를 관리하는 컨테이어
    type DsMemory (name:string)  =

        let mutable memory:Memory = Memory(0uy)

        let readyTag   = DsBit($"{name}(R)",  false , memory, TagFlag.R)    // 0
        let goingTag   = DsBit($"{name}(G)",  false , memory, TagFlag.G)
        let finishTag  = DsBit($"{name}(F)",  false , memory, TagFlag.F)
        let homingTag  = DsBit($"{name}(H)",  false , memory, TagFlag.H)
        let originTag  = DsBit($"{name}(0G)", false , memory, TagFlag.Origin)
        let pauseTag   = DsBit($"{name}(PS)", false , memory, TagFlag.Pause)
        let errorTxTag = DsBit($"{name}(E1)", false , memory, TagFlag.ErrorTx)
        let errorRxTag = DsBit($"{name}(E2)", false , memory, TagFlag.ErrorRx)

        let endTag     = DsBit($"{name}(ET)" ,false , memory, EndIndex)     // 0
        let resetTag   = DsBit($"{name}(RT)" ,false , memory, ResetIndex)   // 1
        let startTag   = DsBit($"{name}(ST)" ,false , memory, StartIndex)   // 2
        let relayTag   = DsBit($"{name}(RE)" ,false , memory, RelayIndex)   // 3
        
        

        member x.Byte  = memory.Value
        member x.Name  = name

        member x.Start = startTag
        member x.Reset = resetTag
        member x.End   = endTag
        member x.Relay = relayTag

        member x.Ready  = readyTag
        member x.Going  = goingTag
        member x.Finish = finishTag
        member x.Homing = homingTag

        member x.Origin  =  originTag
        member x.Pause   =  pauseTag
        member x.ErrorTx =  errorTxTag
        member x.ErrorRx =  errorRxTag

