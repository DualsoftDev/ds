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
    static member CreateStartPort(v:VertexMemoryManager, autoTag:DsTag<bool>) =
        let startForce = tag2expr <| v.StartForce
        let startTag   = tag2expr <| v.StartTag
        let autoTag    = tag2expr <| autoTag
        v.StartPort <== ((startForce <&&> (!!)autoTag) <||> startTag)
    
    [<Extension>] 
    static member CreateResetPort(v:VertexMemoryManager, autoTag:DsTag<bool>) =
        let resetForce = tag2expr <| v.ResetForce
        let resetTag   = tag2expr <| v.ResetTag
        let autoTag    = tag2expr <| autoTag
        v.ResetPort <== ((resetForce <&&> (!!)autoTag) <||> resetTag)
    
    [<Extension>] 
    static member CreateEndPort(v:VertexMemoryManager, autoTag:DsTag<bool>, resetTag:DsTag<bool>) =
        let endForce = tag2expr <| v.EndForce
        let endTag   = tag2expr <| v.EndTag
        let resetTag = tag2expr <| resetTag
        let autoTag  = tag2expr <| autoTag
 
        let set  = ((endForce <&&> (!!)autoTag) <||> endTag)
        let rst  = (!!)resetTag
        let relay = v.EndPort

        FuncExt.GetRelayStatement(set, rst, relay)