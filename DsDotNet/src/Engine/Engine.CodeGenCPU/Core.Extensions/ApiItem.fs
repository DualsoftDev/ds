namespace rec Engine.CodeGenCPU

open Engine.Core
open ConvertCoreExtUtils

[<AutoOpen>]
module ConvertCpuApiItem =
    

    type ApiItem with
        member a.PS     = getAM(a).PS
        member a.PE     = getAM(a).PE
        ///sensorLinking
        member a.SL1     = getAM(a).SL1
        ///sensorLinked
        member a.SL2     = getAM(a).SL2
    
        member a.RxETs       = a.RXs |> Seq.map getVMReal |> Seq.map(fun f->f.ET)
        member a.TxSTs       = a.TXs |> Seq.map getVMReal |> Seq.map(fun f->f.ST)

