[<AutoOpen>]
module Engine.CodeGenCPU.ConvertPort

open Engine.CodeGenCPU
open Engine.Core

type VertexMemoryManager with
    ///Port bit 수식 만들기
    member v.CreateStartPort(autoTag:DsTag<bool>) =
        let startForce = tag2expr v.StartForce
        let startTag   = tag2expr v.StartTag
        let autoTag    = tag2expr autoTag
        v.StartPort <== ((startForce <&&> (!!)autoTag) <||> startTag)

    member v.CreateResetPort(autoTag:DsTag<bool>) =
        let resetForce = tag2expr v.ResetForce
        let resetTag   = tag2expr v.ResetTag
        let autoTag    = tag2expr autoTag
        v.ResetPort <== ((resetForce <&&> (!!)autoTag) <||> resetTag)

    member v.CreateEndPort(autoTag:DsTag<bool>, resetTag:DsTag<bool>) =
        let endForce = tag2expr v.EndForce
        let endTag   = tag2expr v.EndTag
        let resetTag = tag2expr resetTag
        let autoTag  = tag2expr autoTag

        let set  = ((endForce <&&> (!!)autoTag) <||> endTag)
        let rst  = (!!)resetTag
        let relay = v.EndPort

        FuncExt.GetRelayStatement(set, rst, relay)