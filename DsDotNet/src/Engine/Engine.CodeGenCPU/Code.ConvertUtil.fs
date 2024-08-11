namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open Dual.Common.Core.FS
open System

[<AutoOpen>]
module CodeConvertUtil =


    let private getOriginCalls(vr:RealVertexTagManager, initialType:InitialType) =
        let origins = vr.OriginInfo.CallInitials
        origins
            |> filter (fun (_, init) -> init = initialType)
            |> map fst

    let getOriginIOExprs(vr:RealVertexTagManager, initialType:InitialType) =
        getOriginCalls(vr, initialType)
            .Where(fun c-> not(c.IsAnalog))  //test ahn  Analog input도 범위로 입력 추가 필요 
            .Select(fun c-> c.End)

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

                if xs.Where(fun f -> f.GetPure() :? Real).Count() > 1 then 
                    let error = String.Join("\r\n", (xs.Select(fun f->f.DequotedQualifiedName)))
                    failwithlog $"리셋은 하나의 작업에서 가능합니다. \r\n(복수 작업 :\r\n {error})"

                xs.Select(fun f ->
                    match getPure f with
                    | :? Real    as r  -> r.V.GP
                    | :? Call as c(* when c.IsOperator*) -> c.V.ET
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

