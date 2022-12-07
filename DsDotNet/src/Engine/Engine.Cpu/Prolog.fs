namespace rec Engine.Cpu

open System.Collections.Generic
open System.Linq
open System.Diagnostics
open Engine.Core


[<AutoOpen>]
module Prolog =
    type TagBit(name, initValue) =
        inherit Tag<bool>(name, initValue)

    type SegmentStatus(name) =
        let tagCS = TagBit($"{name}_S", false)  // Command Start
        let tagCR = TagBit($"{name}_R", false)  // Command Reset
        let tagCE = TagBit($"{name}_E", false)  // Command End


        let exprCS = tag <| tagCS
        let exprCR = tag <| tagCR
        let exprCE = tag <| tagCE

        // ---------------------------------------------------   // S R E
        let condSR =      ( (!!) exprCS                  <&&> (!!) exprCE )  // 0 X 0
                     <||> (      exprCS <&&>      exprCR <&&> (!!) exprCE )  // 1 1 0
        let condSG =             exprCS <&&> (!!) exprCR <&&> (!!) exprCE    // 1 0 0
        let condSF =                         (!!) exprCR <&&>      exprCE    // X 0 1
        let condSH =                              exprCR <&&>      exprCE    // X 1 1

        let tagSR = TagBit($"{name}_SR", false) // Status Ready
        let tagSG = TagBit($"{name}_SG", false) // Status Going
        let tagSF = TagBit($"{name}_SF", false) // Status Finished
        let tagSH = TagBit($"{name}_SH", false) // Status Homing

        let assignSR = tagSR <== condSR
        let assignSG = tagSG <== condSG
        let assignSF = tagSF <== condSF
        let assignSH = tagSH <== condSH

        let assignStatements = [assignSR; assignSG; assignSF; assignSH]

        let tagOrigin = TagBit($"{name}_O", false)
        let tagPause  = TagBit($"{name}_P", false)
        let tagTX     = TagBit($"{name}_TX", false)
        let tagRX     = TagBit($"{name}_RX", false)
        let exprOrigin = tag <| tagOrigin
        let exprPause  = tag <| tagPause
        let exprTX     = tag <| tagTX
        let exprRX     = tag <| tagRX

        let singleScan() =
            for a in assignStatements do
                a.Do()

        do singleScan()


        member _.TagSR  = tagSR
        member _.TagSG  = tagSG
        member _.TagSF  = tagSF
        member _.TagSH  = tagSH

        member _.TagCS  = tagCS
        member _.TagCR  = tagCR
        member _.TagCE  = tagCE

        member _.TagOrigin = tagOrigin
        member _.TagPause  = tagPause
        member _.TagTX     = tagTX
        member _.TagRX     = tagRX

        member _.ExprOrigin = exprOrigin
        member _.ExprPause  = exprPause
        member _.ExprTX     = exprTX
        member _.ExprRX     = exprRX


        member _.ExprCS  = exprCS
        member _.ExprCR  = exprCR
        member _.ExprCE  = exprCE


        member _.SingleScan() = singleScan()
        member _.Status4 =
            match tagSR.Value, tagSG.Value, tagSF.Value, tagSH.Value with
            | true, false, false, false -> Status4.Ready
            | false, true, false, false -> Status4.Going
            | false, false, true, false -> Status4.Finish
            | false, false, false, true -> Status4.Homing
            | _ -> failwith "ERROR"


