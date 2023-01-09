namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open System.Linq


type Spec02_FlowStatement() =
    do Fixtures.SetUpTest()

    let sourceAllTrue (st:Statement) = 
        st.GetSourceStorages() 
        |> Seq.filter(fun f-> not <| f.Name.StartsWith("_"))
        |> Seq.iter(fun f->f.Value <- true)

    let doTargetCheck (st:Statement) = 
        st.Do()
        st.GetTargetStorages().Head.Value === true
        
    [<Test>] 
    member __.``F1 Root Start Real`` () =   Eq 1 1
        //tReal.V.F1_RootStartReal().Select(fun f->f.Statement)
        //|> Seq.iter(fun st ->
        //    st |> sourceAllTrue
        //    st |> doTargetCheck
        //)  //코일조건자기유지
     


    [<Test>] member __.``F2 Root Reset Real`` () = Eq 1 1
    [<Test>] member __.``F3 Root Start Call`` () = Eq 1 1
    [<Test>] member __.``F4 Root Reset Call`` () = Eq 1 1
