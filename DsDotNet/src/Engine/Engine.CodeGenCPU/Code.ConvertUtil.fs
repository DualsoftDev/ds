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
        getOriginCalls(vr, initialType).Select(fun d-> d.End)

 
   
    [<AutoOpen>]
    [<Extension>]
    type CodeConvertUtilExt =


        [<Extension>]
        static member GetStartCausals(xs:Vertex seq, usingRoot:bool) =
                xs.Select(fun f->
                match f with
                | :? Real    as r  -> r.V.F
                | :? Call as c  -> c.V.F
                | :? Alias   as a  -> if usingRoot then getPure(a.V.Vertex).V.F else a.V.F
                | _ -> failwithlog $"Error {getFuncName()}"
                ).Distinct()
        //리셋 원인
        [<Extension>]
        static member GetResetCausals(xs:Vertex seq) =
                xs.Select(fun f ->
                    match getPure f with
                    | :? Real    as r  -> r.V.GG
                    | :? Call as c when c.IsOperator -> c.V.ET
                    | _ -> failwithlog $"Error {getFuncName()}"
                ).Distinct()

        [<Extension>]
        static member GetStartDAGAndCausals  (v:Vertex) =
            let tags = getStartEdgeSources(v).GetStartCausals(false)
            tags.ToAndElseOff()

        [<Extension>]
        static member GetResetRootAndCausals  (v:Vertex) =
            let tags = getResetEdgeSources(v).GetResetCausals()
            tags.ToAndElseOff()

        [<Extension>]
        static member GetStartRootAndCausals  (v:Vertex) =
            let tags = getStartEdgeSources(v).GetStartCausals(true)
            tags.ToAndElseOff()

