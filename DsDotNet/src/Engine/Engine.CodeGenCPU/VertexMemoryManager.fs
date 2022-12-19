namespace Engine.CodeGenCPU

open System.Diagnostics
open System
open System.Text.RegularExpressions
open Engine.Core

[<AutoOpen>]
module VertexMemoryManagerModule =

    /// Vertex Manager : 소속되어 있는 DsBit 를 관리하는 컨테이어
    type VertexMemoryManager (v:Vertex)  =
        let name = v.QualifiedName
        let bit name flag = DsBit(name, false, v, flag)

        let readyTag   = bit $"{name}(R)"  TagFlag.R
        let goingTag   = bit $"{name}(G)"  TagFlag.G
        let finishTag  = bit $"{name}(F)"  TagFlag.F
        let homingTag  = bit $"{name}(H)"  TagFlag.H
        let originTag  = bit $"{name}(0G)" TagFlag.Origin
        let pauseTag   = bit $"{name}(PA)" TagFlag.Pause
        let errorTxTag = bit $"{name}(E1)" TagFlag.ErrorTx
        let errorRxTag = bit $"{name}(E2)" TagFlag.ErrorRx

        let relayTag   = bit $"{name}(RE)" TagFlag.Relay

            //port 값을 자동으로 변경
        let endTag     = bit $"{name}(ET)" TagFlag.ET
        let resetTag   = bit $"{name}(RT)" TagFlag.RT
        let startTag   = bit $"{name}(ST)" TagFlag.ST

            //자동일 경우 tag 값에 의해 수동일때 force 값에 의해 변경
        let endPort    = bit $"{name}(EP)" TagFlag.EP
        let resetPort  = bit $"{name}(RP)" TagFlag.RP
        let startPort  = bit $"{name}(SP)" TagFlag.SP

            //port 값을 수동으로 강제 변경
        let endForce   = bit $"{name}(EP)" TagFlag.EF
        let resetForce = bit $"{name}(RP)" TagFlag.RF
        let startForce = bit $"{name}(SP)" TagFlag.SF

        interface IVertexMemoryManager with
            member x.Vertex = v

        member x.Name  = name

        member x.StartTag = startTag
        member x.ResetTag = resetTag
        member x.EndTag   = endTag

        member x.StartPort = startPort
        member x.ResetPort = resetPort
        member x.EndPort = endPort

        member x.StartForce = startForce
        member x.ResetForce = resetForce
        member x.EndForce   = endForce


        member x.Relay = relayTag

        member x.Ready  = readyTag
        member x.Going  = goingTag
        member x.Finish = finishTag
        member x.Homing = homingTag

        member x.Origin  =  originTag
        member x.Pause   =  pauseTag
        member x.ErrorTx =  errorTxTag
        member x.ErrorRx =  errorRxTag

