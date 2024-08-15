namespace T.CPU
open Dual.UnitTest.Common.FS

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

    type CpuTestSample(target:PlatformTarget) =
        let targetNDrvier =  (target, PAIX_IO)
        let LoadSampleSystem()  =
            let systemRepo   = ShareableSystemRepository ()
            let referenceDir = @$"{__SOURCE_DIRECTORY__}/../../UnitTest.Model/UnitTestExample/dsSimple"
            let sys = parseText systemRepo referenceDir Program.CpuTestText
            RuntimeDS.System <- Some sys
            RuntimeDS.Package <- RuntimePackage.PC

            applyTagManager (sys, Storages(), targetNDrvier)
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

            RuntimeGeneratorModule.clearNFullSlotHwSlotDataTypes()
            DsAddressModule.assignAutoAddress(sys, 0, 100000) targetNDrvier

            sys.GenerationIO()
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
        //member x.ApiCallSet  =  sys.GetTaskDevCallSet().Select(fun (td,cs)->(td.ApiItems.Select(),cs))
        //member x.TaskDevCallSet  =  sys.GetTaskDevCallSet() //test ahn

        member x.InRealCalls  =  callTypeAll.Where(fun f->f.Parent.GetCore() :? Real).OfType<Call>()
        member x.AbleVertexInFlows    =  callTypeAll
                                                .Where(fun f->f.Parent.GetCore() :? Flow)
                                                .Where(fun f-> not(f :? Call ))
                                                 |> Seq.append ([x.ARInF.Vertex])
                                                 |> Seq.append ([x.AREInF.Vertex])

        member x.ALL    =  vertexAll.Select(getVM)

    let doCheck (commentedStatement:CommentedStatement) =
        let st = commentedStatement.Statement

        System.Diagnostics.Debug.WriteLine(st.ToText())

        st.GetSourceStorages()
        |> Seq.filter(fun f-> not <| f.Name.StartsWith("_"))
        |> Seq.iter(fun f->f.BoxedValue <- true)

        st.Do()
        st.GetTargetStorages().Head.BoxedValue === st.GetTargetStorages().Head.BoxedValue

    let doChecks (commentedStatements:CommentedStatement seq) = commentedStatements.Iter(doCheck)

