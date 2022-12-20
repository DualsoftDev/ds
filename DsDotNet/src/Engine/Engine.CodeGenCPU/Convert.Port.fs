[<AutoOpen>]
module Engine.CodeGenCPU.ConvertPort

open Engine.CodeGenCPU
open Engine.Core

type VertexMemoryManager with
    ///Port bit 수식 만들기
    member v.CreateStartPortRung(autoTag:DsTag<bool>): Statement =
        let startForce = tag2expr v.StartForce
        let startTag   = tag2expr v.StartTag
        let autoTag    = tag2expr autoTag
        v.StartPort <== ((startForce <&&> (!!)autoTag) <||> startTag)

    member v.CreateResetPortRung(autoTag:DsTag<bool>): Statement =
        let resetForce = tag2expr v.ResetForce
        let resetTag   = tag2expr v.ResetTag
        let autoTag    = tag2expr autoTag
        v.ResetPort <== ((resetForce <&&> (!!)autoTag) <||> resetTag)

    member v.CreateEndPortRung(autoTag:DsTag<bool>, resetTag:DsTag<bool>): Statement =
        let endForce = tag2expr v.EndForce
        let endTag   = tag2expr v.EndTag
        let resetTag = tag2expr resetTag
        let autoTag  = tag2expr autoTag

        let set  = ((endForce <&&> (!!)autoTag) <||> endTag)
        let rst  = (!!)resetTag
        let relay = v.EndPort

        FuncExt.GetRelayRung(set, rst, relay)