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

    type CpuTestSample(target) =

        let LoadSampleSystem()  =
            let systemRepo   = ShareableSystemRepository ()
            let referenceDir = @$"{__SOURCE_DIRECTORY__}/../../UnitTest.Model/UnitTestExample/dsSimple"
            let sys = parseText systemRepo referenceDir Program.CpuTestText
            RuntimeDS.System <- sys
            RuntimeDS.Package <- RuntimePackage.PC
            applyTagManager (sys, Storages(), target)
            checkCausalModel sys
            sys

        let sys               = LoadSampleSystem()
        let vertices          = sys.GetVertices()
        let flow              = sys.Flows.Find(fun f->f.Name = "MyFlow")
        let realInFlow        = flow.Graph.Vertices.First(fun f->f.Name = "Seg1") :?> Real
        //let callInFlow        = flow.Graph.Vertices.First(fun f->f.Name = "Ap") :?> Call
        let callInReal        = realInFlow.Graph.Vertices.First(fun f->f.Name = "Am") :?> Call

        let aliasCallInReal   = realInFlow.Graph.Vertices.First(fun f->f.Name = "aliasCallInReal") :?> Alias
        //let aliasCallInFlow   = flow.Graph.Vertices.First(fun f->f.Name = "aliasCallInFlow") :?> Alias
        let aliasRealInFlow   = flow.Graph.Vertices.First(fun f->f.Name = "aliasRealInFlow") :?> Alias
        let aliasRealExInFlow = flow.Graph.Vertices.First(fun f->f.Name = "aliasRealExInFlow") :?> Alias

        let callTypeAll       = vertices.OfType<Call>().Cast<Vertex>()
        let coinTypeAll       = vertices.Except(vertices.OfType<Real>().Cast<Vertex>()).Where(fun f->f.Parent.GetCore() :? Real)
        let realTypeAll       = vertices.OfType<Real>()
        let vertexAll         = vertices
        do
            
            RuntimeGeneratorModule.clearNFullSlotHwSlotDataTypes()
            DsAddressModule.assignAutoAddress(sys, 0, 100000) target
            
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
        member x.Reals  =  realTypeAll.Select(getVM)
        member x.Calls  =  callTypeAll.Select(getVM)
        member x.Apis  =  sys.ApiItems.Select(getAM)
        member x.ApiCoinsSet  =  sys.GetApiCoinsSet().Select(fun (api,coins)->(getAM(api),coins))

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

