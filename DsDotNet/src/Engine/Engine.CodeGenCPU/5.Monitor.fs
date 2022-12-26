[<AutoOpen>]
module Engine.CodeGenCPU.ConvertMonitor

open System.Linq
open Engine.Core
open Engine.CodeGenCPU

type VertexMemoryManager with
   
    member Real.M1_OriginMonitor(): Statement  = 
        ConvertTypeCheck Real.Vertex (ConvertType.CvtCall ||| ConvertType.CvtAlias)

        let real = Real.Vertex :?> Real
        let jobDefInfos = OriginHelper.GetOriginsWithJobDefs real.Graph

        Real.Origin <== Real.OG 

    