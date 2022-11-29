namespace Engine.Cpu

open System.Diagnostics
open System
open System.Text.RegularExpressions
open Engine.Core

[<AutoOpen>]
module DsMemoryModule = 

    /// DsMemory DsBit/DsDotBit 를 관리하는 컨테이어 
    type DsMemory (name:string)  =
        
        let mutable memory:Memory = Memory(0uy)

        let readyTag   = DsBit($"{name}(R)",  false , memory, Monitor.R)
        let goingTag   = DsBit($"{name}(G)",  false , memory, Monitor.G)
        let finishTag  = DsBit($"{name}(F)",  false , memory, Monitor.F)
        let homingTag  = DsBit($"{name}(H)",  false , memory, Monitor.H)
        let originTag  = DsBit($"{name}(0G)", false , memory, Monitor.Origin)
        let pauseTag   = DsBit($"{name}(PS)", false , memory, Monitor.Pause)
        let errorTxTag = DsBit($"{name}(E1)", false , memory, Monitor.ErrorTx)
        let errorRxTag = DsBit($"{name}(E2)", false , memory, Monitor.ErrorRx)

        let startTag   = DsDotBit($"{name}[{StartIndex}]" , false, memory)
        let resetTag   = DsDotBit($"{name}[{ResetIndex}]" , false, memory)
        let endTag     = DsDotBit($"{name}[{EndIndex}]"   , false, memory)
        let relayTag   = DsDotBit($"{name}[{RelayIndex}]" , false, memory)
        
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

     