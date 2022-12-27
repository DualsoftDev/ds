[<AutoOpen>]
module Engine.CodeGenCPU.ConvertMonitor

open System.Linq
open Engine.Core
open Engine.CodeGenCPU

type VertexManager with
   
    member Real.M1_OriginMonitor(): Statement  = 

        let real = Real.Vertex :?> Real
        let jobDefInfos = OriginHelper.GetOriginsWithJobDefs real.Graph

        Real.Origin <== Real.OG 

    