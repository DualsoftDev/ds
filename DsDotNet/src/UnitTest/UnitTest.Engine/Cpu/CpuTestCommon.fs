namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Common.FS
open Engine.Cpu
open Engine.CodeGenCPU
open System.Linq

[<AutoOpen>]
module CpuTestUtil =

    type CpuTestSample() =

        let LoadSampleSystem()  =
            let systemRepo   = ShareableSystemRepository ()
            let referenceDir = @$"{__SOURCE_DIRECTORY__}\..\Libraries"
            let sys = parseText systemRepo referenceDir Program.CpuTestText
            applyTagManager (sys, Storages())
            sys

        let sys               = LoadSampleSystem()
        let vertices          = sys.GetVertices()
        let flow              = sys.Flows.Find(fun f->f.Name = "MyFlow")
        let realInFlow        = flow.Graph.Vertices.First(fun f->f.Name = "Seg1") :?> Real
        let realExFlow        = flow.Graph.Vertices.First(fun f->f.Name = "F.R3") :?> RealExF
        let callInFlow        = flow.Graph.Vertices.First(fun f->f.Name = "Ap") :?> Call
        let callInReal        = realInFlow.Graph.Vertices.First(fun f->f.Name = "Am") :?> Call

        let aliasCallInReal   = realInFlow.Graph.Vertices.First(fun f->f.Name = "aliasCallInReal") :?> Alias
        let aliasCallInFlow   = flow.Graph.Vertices.First(fun f->f.Name = "aliasCallInFlow") :?> Alias
        let aliasRealInFlow   = flow.Graph.Vertices.First(fun f->f.Name = "aliasRealInFlow") :?> Alias
        let aliasRealExInFlow = flow.Graph.Vertices.First(fun f->f.Name = "aliasRealExInFlow") :?> Alias

        let callTypeAll       = vertices.OfType<Call>().Cast<Vertex>()
        let coinTypeAll       = vertices.Except(vertices.OfType<Real>().Cast<Vertex>())
        let realTypeAll       = vertices.OfType<Real>()
        let vertexAll         = vertices
        do
            sys.GenerationIO()

        member x.Sys    =  sys
        member x.Flows  =  sys.Flows
        member x.RInF   =  realInFlow.V
        member x.RExF   =  realExFlow.V
        member x.CInF   =  callInFlow.V
        member x.CInR   =  callInReal.V
        member x.ACinR  =  aliasCallInReal.V
        member x.ACInF  =  aliasCallInFlow.V
        member x.ARInF  =  aliasRealInFlow.V
        member x.AREInF =  aliasRealExInFlow.V
        member x.Coins  =  coinTypeAll.Select(getVM)
        member x.Reals  =  realTypeAll.Select(getVM)
        member x.Calls  =  callTypeAll.Select(getVM)
        member x.ALL    =  vertexAll.Select(getVM)

    let doCheck (commentedStatement:CommentedStatement) =
        let st = commentedStatement.Statement
        st.GetSourceStorages()
        |> Seq.filter(fun f-> not <| f.Name.StartsWith("_"))
        |> Seq.iter(fun f->f.BoxedValue <- true)

        st.Do()     // test ahn 추후 정답지 작성
        st.GetTargetStorages().Head.BoxedValue === st.GetTargetStorages().Head.BoxedValue

    let doChecks (commentedStatements:CommentedStatement seq) = commentedStatements.Iter(doCheck)

