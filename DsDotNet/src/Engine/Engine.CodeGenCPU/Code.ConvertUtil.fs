namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open Dual.Common.Core.FS

[<AutoOpen>]
module CodeConvertUtil =


    let private getOriginCalls(vr:VertexMReal, initialType:InitialType) =
        let origins = vr.OriginInfo.CallInitials
        origins
            |> filter (fun (_, init) -> init = initialType)
            |> map fst

    let getOriginIOExprs(vr:VertexMReal, initialType:InitialType) =
        getOriginCalls(vr, initialType).Select(fun d-> d.EndActionOnlyIO)

    let getOriginSimPlanEnds(vr:VertexMReal, initialType:InitialType) =
        getOriginCalls(vr, initialType).Select(fun c-> c.EndPlan)

   
    [<AutoOpen>]
    [<Extension>]
    type CodeConvertUtilExt =


        [<Extension>]
        static member GetStartCausals(xs:Vertex seq, usingRoot:bool) =
                xs.Select(fun f->
                match f with
                | :? Real    as r  -> r.V.F
                | :? RealExF as rf -> rf.Real.V.F
                | :? Call as c  -> c.V.F
                | :? Alias   as a  -> if usingRoot then getPure(a.V.Vertex).V.F else a.V.F
                | _ -> failwithlog $"Error {getFuncName()}"
                ).Distinct()
        //리셋 원인
        [<Extension>]
        static member GetResetCausals(xs:Vertex seq) =
                xs.Select(fun f ->
                    match getPure f with
                    | :? Real    as r  -> r.V.G
                    | :? RealExF as rf -> rf.Real.V.G
                    | :? Call as c when c.IsOperator -> c.V.ET
                    | _ -> failwithlog $"Error {getFuncName()}"
                ).Distinct()
   

        [<Extension>]
        static member GetWeakStartRootAndCausals  (v:Vertex) =
            let tags = getStartWeakEdgeSources(v).GetStartCausals(true)
            tags.ToAndElseOff()

        [<Extension>]
        static member GetWeakStartDAGAndCausals  (v:Vertex) =
            let tags = getStartWeakEdgeSources(v).GetStartCausals(false)
            tags.ToAndElseOff()

        [<Extension>]
        static member GetWeakResetRootAndCausals  (v:Vertex) =
            let tags = getResetWeakEdgeSources(v).GetResetCausals()
            tags.ToAndElseOff()

        [<Extension>]
        static member GetStrongStartRootAndCausals  (v:Vertex) =
            let tags = getStartStrongEdgeSources(v).GetStartCausals(true)
            tags.ToAndElseOff()

        [<Extension>]
        static member GetStrongResetRootAndCausals  (v:Vertex) =
            let tags = getResetStrongEdgeSources(v).GetResetCausals()
            tags.ToAndElseOff()

