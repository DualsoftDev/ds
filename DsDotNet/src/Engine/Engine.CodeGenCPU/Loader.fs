namespace Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Engine.Common.FS

[<AutoOpen>]
module CpuLoader =

    let ConvertSystem(sys:DsSystem) =
        [
            for f in sys.Flows do
            for r in f.Graph.Vertices.OfType<Real>() do
                yield! createRungsForReal(r, r.Graph)
                yield! createRungsForRoot(r, f.Graph)
        ]

        //dicM.Clear()

        //let aliasSet = ConcurrentDictionary<Alias, Vertex>() //Alias, target
        //let vertices = sys.GetVertices() @ sys.ReferenceSystems.SelectMany(fun s-> s.GetVertices())

        ////모든 인과 대상 Node 메모리화
        //vertices.ForEach(fun v ->
        //    match v with
        //        | :? Real as r ->
        //            dicM.TryAdd(r, VertexMemoryManager(v)) |> verifyM $"Duplicated name [{v.QualifiedName}]"
        //        | :? Call as c ->
        //            dicM.TryAdd(c, VertexMemoryManager(v)) |> verifyM $"Duplicated name [{v.QualifiedName}]"
        //        | :? Alias as a ->
        //            match a.TargetWrapper with
        //            | DuAliasTargetReal r -> aliasSet.TryAdd(a, r)        |> verifyM $"Duplicated name [{v.QualifiedName}]"
        //            | DuAliasTargetRealEx rEx -> aliasSet.TryAdd(a, rEx)  |> verifyM $"Duplicated name [{v.QualifiedName}]"
        //            | DuAliasTargetCall c -> aliasSet.TryAdd(a, c)        |> verifyM $"Duplicated name [{v.QualifiedName}]"
        //        | _-> ()
        //)

        ////Alias 원본 메모리 매칭
        //aliasSet.ForEach(fun alias->
        //    let vertex = vertices.First(fun f->f  =alias.Value)
        //    dicM.TryAdd(alias.Key, dicM.[vertex]) |> ignore
        //)
        //let statements =
        //    sys.Flows.SelectMany(fun flow->
        //    flow.Graph.Vertices
        //        .Where(fun w->w :? Real).Cast<Real>()
        //        .SelectMany(fun r->
        //            createRungsForReal(dicM[r], r.Graph)
        //            @
        //            createRungsForRoot(r, flow.Graph)
        //        )
        //    )

        //statements, dicM

    ///DsSystem 규격으로 cpu code 불러 로딩하기
    let LoadStatements(system:DsSystem) = ConvertSystem(system)

    ///DsSystem 규격으로 cpu code 불러 Text으로 리턴
    let LoadStatementsForText(system:DsSystem) =
        [
            for (desc_, statement) in ConvertSystem(system) ->
                statement.ToText()
        ].JoinWith("\r\n")

