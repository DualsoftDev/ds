namespace Engine.CodeGenCPU

open System.Diagnostics
open System
open System.Text.RegularExpressions
open Engine.Core

[<AutoOpen>]
module VertexMModule =

    /// Vertex Manager : 소속되어 있는 DsBit 를 관리하는 컨테이어
    type VertexM (v:Vertex)  =
        let name = v.QualifiedName

        let readyTag   = DsBit($"{name}(R)",  false ,v ,TagFlag.R)    
        let goingTag   = DsBit($"{name}(G)",  false ,v ,TagFlag.G)
        let finishTag  = DsBit($"{name}(F)",  false ,v ,TagFlag.F)
        let homingTag  = DsBit($"{name}(H)",  false ,v ,TagFlag.H)
        let originTag  = DsBit($"{name}(0G)", false ,v ,TagFlag.Origin)
        let pauseTag   = DsBit($"{name}(PA)", false ,v ,TagFlag.Pause)
        let errorTxTag = DsBit($"{name}(E1)", false ,v ,TagFlag.ErrorTx)
        let errorRxTag = DsBit($"{name}(E2)", false ,v ,TagFlag.ErrorRx)
                                                       
        let relayTag   = DsBit($"{name}(RE)" ,false ,v ,TagFlag.Relay)  

            //port 값을 자동으로 변경 
        let endTag     = DsBit($"{name}(ET)" ,false ,v ,TagFlag.ET)
        let resetTag   = DsBit($"{name}(RT)" ,false ,v ,TagFlag.RT) 
        let startTag   = DsBit($"{name}(ST)" ,false ,v ,TagFlag.ST)
        
            //자동일 경우 tag 값에 의해 수동일때 force 값에 의해 변경
        let endPort    = DsBit($"{name}(EP)" ,false ,v ,TagFlag.EP)
        let resetPort  = DsBit($"{name}(RP)" ,false ,v ,TagFlag.RP)
        let startPort  = DsBit($"{name}(SP)" ,false ,v ,TagFlag.SP)
        
            //port 값을 수동으로 강제 변경
        let endForce   = DsBit($"{name}(EP)" ,false ,v ,TagFlag.EF)
        let resetForce = DsBit($"{name}(RP)" ,false ,v ,TagFlag.RF) 
        let startForce = DsBit($"{name}(SP)" ,false ,v ,TagFlag.SF) 
        
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

