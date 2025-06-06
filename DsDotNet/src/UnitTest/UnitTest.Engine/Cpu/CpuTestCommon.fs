namespace T.CPU
open Dual.Common.UnitTest.FS

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Dual.Common.Core.FS
open Engine.Cpu
open Engine.CodeGenCPU
open System.Linq

[<AutoOpen>]
module CpuTestUtil =

    type CpuTestSample(hwCPU:HwCPU) =
        let cnf = createModelConfigReplaceHwCPU (createDefaultModelConfig(), hwCPU)
        let LoadSampleSystem()  =
            let systemRepo   = ShareableSystemRepository ()
            let referenceDir = @$"{__SOURCE_DIRECTORY__}/../../UnitTest.Model/UnitTestExample/dsSimple"
            let sys = parseText systemRepo referenceDir Program.CpuTestText
            RuntimeDS.ReplaceSystem sys
            applyTagManager (sys, Storages(),  cnf)
            checkCausalModel sys
            sys

        let sys               = LoadSampleSystem()
        let vertices          = sys.GetVertices()
        let flow              = sys.Flows.OrderBy(fun d->d.Name).First()
        let realInFlow        = flow.Graph.Vertices.OrderBy(fun d->d.Name).OfType<Real>().First()
        let callInReal        = realInFlow.Graph.Vertices.OrderBy(fun d->d.Name).OfType<Call>().First()

        let aliasCallInReal   = realInFlow.Graph.Vertices.OfType<Alias>().First()
        let aliasRealInFlow   = flow.Graph.Vertices.OfType<Alias>().First()
        let aliasRealExInFlow = flow.Graph.Vertices.OfType<Alias>()
                                    .First(fun f->f.IsExFlowReal = true)

        let callTypeAll       = vertices.OfType<Call>().Cast<Vertex>()
        let coinTypeAll       = vertices.Except(vertices.OfType<Real>().Cast<Vertex>()).Where(fun f->f.Parent.GetCore() :? Real)
        let realTypeAll       = vertices.OfType<Real>()
        let vertexAll         = vertices
        do

            //DsAddressModule.assignAutoAddress(sys, 0, 100000, defaultHwTarget) 

            sys.GenerationIO(cnf.RuntimeMode, cnf.HwTarget.HwIO)
            sys.GenerationOrigins()

        member x.Sys    =  sys
        member x.Flows  =  sys.Flows
        member x.RInF   =  realInFlow.V
        //member x.CInF   =  callInFlow.V
        member x.CInR   =  callInReal.V
        member x.ACinR  =  aliasCallInReal.V
        //member x.ACInF  =  aliasCallInFlow.V
        member x.ARInF  =  aliasRealInFlow.V
        member x.AREInF =  aliasRealExInFlow.V
        member x.Coins  =  coinTypeAll.Select(getVM)
        member x.Reals  =  realTypeAll.Select(getVMReal)
        member x.Calls  =  callTypeAll.Select(getVM)
        member x.Apis  =  sys.ApiItems.Select(getAM)

        member x.InRealCalls  =  callTypeAll.Where(fun f->f.Parent.GetCore() :? Real).OfType<Call>()
        member x.AbleVertexInFlows    =  callTypeAll
                                                .Where(fun f->f.Parent.GetCore() :? Flow)
                                                .Where(fun f-> not(f :? Call ))
                                                 |> Seq.append ([x.ARInF.Vertex])
                                                 |> Seq.append ([x.AREInF.Vertex])

        member x.ALL    =  vertexAll.Select(getVM)

    let doCheck (commentedStatement:CommentedStatement) =
        let st = commentedStatement.Statement

        st.GetSourceStorages()
        |> Seq.filter(fun f-> not <| f.Name.StartsWith("_"))
        |> Seq.iter(fun f->f.BoxedValue <- true)

        st.Do()
        st.GetTargetStorages().Head.BoxedValue === st.GetTargetStorages().Head.BoxedValue

    let doChecks (commentedStatements:CommentedStatement seq) = commentedStatements.Iter(doCheck)

