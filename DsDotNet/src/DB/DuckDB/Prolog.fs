namespace rec Engine.Cpu

open Engine.Core


[<AutoOpen>]
module Prolog =
    type TagBit(name, initValue) =
        inherit Tag<bool>(name, initValue)

    type SegmentStatus(name) =
        let tagCS = TagBit($"{name}_S", false)  // Command Start
        let tagCR = TagBit($"{name}_R", false)  // Command Reset
        let tagCE = TagBit($"{name}_E", false)  // Sensor End

        let tagS4R = TagBit($"{name}_SR", false) // Status Ready
        let tagS4G = TagBit($"{name}_SG", false) // Status Going
        let tagS4F = TagBit($"{name}_SF", false) // Status Finished
        let tagS4H = TagBit($"{name}_SH", false) // Status Homing

        let tagOrigin = TagBit($"{name}_O", false)
        let tagPause  = TagBit($"{name}_P", false)
        let tagTX     = TagBit($"{name}_TX", false)
        let tagRX     = TagBit($"{name}_RX", false)

        let exprCS = tag <| tagCS
        let exprCR = tag <| tagCR
        let exprCE = tag <| tagCE

        // ---------------------------------------------------                // S R E
        let condS4R =      ( (!!) exprCS                  <&&> (!!) exprCE )  // 0 X 0
                      <||> (      exprCS <&&>      exprCR <&&> (!!) exprCE )  // 1 1 0
        let condS4G =             exprCS <&&> (!!) exprCR <&&> (!!) exprCE    // 1 0 0
        let condS4F =                         (!!) exprCR <&&>      exprCE    // X 0 1
        let condS4H =                              exprCR <&&>      exprCE    // X 1 1

        let assignS4R = tagS4R <== condS4R
        let assignS4G = tagS4G <== condS4G
        let assignS4F = tagS4F <== condS4F
        let assignS4H = tagS4H <== condS4H

        let assignStatements = [assignS4R; assignS4G; assignS4F; assignS4H]

        let exprOrigin = tag <| tagOrigin
        let exprPause  = tag <| tagPause
        let exprTX     = tag <| tagTX
        let exprRX     = tag <| tagRX

        let singleScan() =
            for a in assignStatements do
                a.Do()

        do singleScan()


        member _.TagS4R = tagS4R
        member _.TagS4G = tagS4G
        member _.TagS4F = tagS4F
        member _.TagS4H = tagS4H

        member _.TagCS = tagCS
        member _.TagCR = tagCR
        member _.TagCE = tagCE

        member _.TagOrigin = tagOrigin
        member _.TagPause  = tagPause
        member _.TagTX     = tagTX
        member _.TagRX     = tagRX

        member _.ExprOrigin = exprOrigin
        member _.ExprPause  = exprPause
        member _.ExprTX     = exprTX
        member _.ExprRX     = exprRX


        member _.ExprCS = exprCS
        member _.ExprCR = exprCR
        member _.ExprCE = exprCE


        member _.SingleScan() = singleScan()
        member _.Status4 =
            match tagS4R.Value, tagS4G.Value, tagS4F.Value, tagS4H.Value with
            | true, false, false, false -> Status4.Ready
            | false, true, false, false -> Status4.Going
            | false, false, true, false -> Status4.Finish
            | false, false, false, true -> Status4.Homing
            | _ -> failwith "ERROR"


