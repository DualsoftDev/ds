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
            parseText systemRepo referenceDir Program.CpuTestText
        

        let sys                = LoadSampleSystem()
        let flow               = sys.Flows.Find(fun f->f.Name = "MyFlow") 
        let realInFlow         = flow.Graph.Vertices.First(fun f->f.Name = "Seg1") :?> Real
        let realExFlow         = flow.Graph.Vertices.First(fun f->f.Name = "F.R3") :?> RealEx
        let callInFlow         = flow.Graph.Vertices.First(fun f->f.Name = "Ap") :?> Call
        let callInReal         = realInFlow.Graph.Vertices.First(fun f->f.Name = "Am") :?> Call
        
        let aliasCallInReal    = realInFlow.Graph.Vertices.First(fun f->f.Name = "Seg1Alias1") :?> Alias
        let aliasCallInFlow    = flow.Graph.Vertices.First(fun f->f.Name = "AmAlias1") :?> Alias
        let aliasRealInFlow    = flow.Graph.Vertices.First(fun f->f.Name = "Seg2AliasInFlow") :?> Alias
        let aliasRealExInFlow  = flow.Graph.Vertices.First(fun f->f.Name = "exAlias") :?> Alias

        let coinTypeAll        = sys.GetVertices().OfType<Call>().Cast<Vertex>() @ (sys.GetVertices().OfType<Alias>().Cast<Vertex>())
        let realTypeAll        = sys.GetVertices().OfType<Real>() 
        let vertexAll          = sys.GetVertices()
        do
            sys.GenerationButtonIO()
            sys.GenerationLampIO()
            sys.GenerationJobIO()

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
        member x.ALL    =  vertexAll.Select(getVM)      


       
    let doCheck (commentedStatement:CommentedStatement) = 
        let st = commentedStatement.Statement
        st.GetSourceStorages() 
        |> Seq.filter(fun f-> not <| f.Name.StartsWith("_"))
        |> Seq.iter(fun f->f.Value <- true)

        st.Do()     // test ahn 추후 정답지 작성
        st.GetTargetStorages().Head.Value === st.GetTargetStorages().Head.Value
        
    let doChecks (commentedStatements:CommentedStatement seq) = commentedStatements.Iter(doCheck)
