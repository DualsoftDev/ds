namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS


type Spec01_PortStatement() =
    do Fixtures.SetUpTest()

    let sourceAllTrue (st:Statement) = 
        st.GetSourceStorages() 
        |> Seq.filter(fun f-> not <| f.Name.StartsWith("_"))
        |> Seq.iter(fun f->f.Value <- true)

    let doTargetCheck (st:Statement) = 
        st.Do()
        st.GetTargetStorages().Head.Value === true
        
    [<Test>] 
    member __.``P1 Real Start Port`` () = 
        let st = tReal.V.P1_RealStartPort().Statement
        st |> sourceAllTrue
        st |> doTargetCheck

    [<Test>] 
    member __.``P2 Real Reset Port`` () = 
        let st = tReal.V.P2_RealResetPort().Statement
        st |> sourceAllTrue
        st |> doTargetCheck

    [<Test>] 
    member __.``P3 Real End Port`` () = 
        let st = tReal.V.P3_RealEndPort().Statement
        st |> sourceAllTrue
        st |> doTargetCheck

    [<Test>] 
    member __.``P4 Call Start Port`` () = 
        let st = tCall.V.P4_CallStartPort().Statement
        st |> sourceAllTrue
        st |> doTargetCheck

    [<Test>] 
    member __.``P5 Call Reset Port`` () = 
        let st = tCall.V.P5_CallResetPort().Statement
        st |> sourceAllTrue
        st |> doTargetCheck

    [<Test>] 
    member __.``P6 Call End Port`` () = 
        let st = tCall.V.P6_CallEndPort().Statement
        st |> sourceAllTrue
        st |> doTargetCheck
        