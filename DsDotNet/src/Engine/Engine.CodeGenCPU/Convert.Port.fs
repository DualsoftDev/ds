[<AutoOpen>]
module Engine.CodeGenCPU.ConvertPort

open System.Linq
open System.Runtime.CompilerServices
open Engine.CodeGenCPU
open Engine.Core

[<AutoOpen>]
[<Extension>]
type StatementPort =

    ///Port bit 수식 만들기
    [<Extension>] 
    static member CreateStartPort(v:VertexM, autoTag:DsTag<bool>) =
        let startForce = tag <| v.StartForce
        let startTag   = tag <| v.StartTag
        let autoTag    = tag <| autoTag
        v.StartPort <== ((startForce <&&> (!!)autoTag) <||> startTag)
    
    [<Extension>] 
    static member CreateResetPort(v:VertexM, autoTag:DsTag<bool>) =
        let resetForce = tag <| v.ResetForce
        let resetTag   = tag <| v.ResetTag
        let autoTag    = tag <| autoTag
        v.ResetPort <== ((resetForce <&&> (!!)autoTag) <||> resetTag)
    
    [<Extension>] 
    static member CreateEndPort(v:VertexM, autoTag:DsTag<bool>, resetTag:DsTag<bool>) =
        let endForce = tag <| v.EndForce
        let endTag   = tag <| v.EndTag
        let resetTag = tag <| resetTag
        let autoTag  = tag <| autoTag
 
        let set  = ((endForce <&&> (!!)autoTag) <||> endTag)
        let rst  = (!!)resetTag
        let relay = v.EndPort

        FuncExt.GetRelayStatement(set, rst, relay)