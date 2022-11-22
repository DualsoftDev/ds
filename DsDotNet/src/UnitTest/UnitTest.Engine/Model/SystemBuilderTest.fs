namespace UnitTest.Engine


open System.Linq
open Engine
open Engine.Core
open Engine.Common.FS
open NUnit.Framework
open Engine.Parser.FS
open System.Text.RegularExpressions



[<AutoOpen>]
module SystemBuilderTest =
    type SystemBuilderTest() =
        inherit TestBase()

        let libdir = @$"{__SOURCE_DIRECTORY__}\..\Libraries"
        let compare = compare libdir
        let compareExact x = compare x x
        [<Test>]
        member __.``XBuilder test`` () =
            let opt =
                option {
                    if true then
                        return 1
                    else
                        return! None
                }
            //let sys2 =
            //    system {
            //        //if true then
            //        //    ()
            //        //else
            //        //    ()
            //        let! f = get_flow "F1"
            //        name "MySystem"
            //    }


            let system =
                let sysCore = DsSystem.Create("MySystem", "localhost")
                withSystem sysCore {
                    name "MySystem"
                    path [libdir]
                    device "A" "cylinder.ds"
                    device "B" "cylinder.ds"
                    call "Ap" [ { Api=("A", "+"); Tx="%Q1"; Rx="%I1" } ]
                    call "Am" [ { Api=("A", "-"); Tx="%Q2"; Rx="%I2" } ]

                    //let flowCore = Flow.Create("F1", sysCore)
                    //let flow = withFlow flowCore {
                    //    real "R1"
                    //    real "R2"
                    //}
                    //create_flow flow


                    //withFlow (Flow.Create("F3", sysCore)) {
                    //    real "R1"
                    //    real "R2"
                    //}

                    flow "F2"
                    //{
                    //    real "R1"
                    //    real "R2"
                    //}


                    //create_flow "F2"

                    //create_flow_from_obj (
                    //    //flow (getNull<DsSystem>()) {
                    //    flow "F1" sysCore {
                    //        name "F1"
                    //    })
                }

            let generated = system.ToDsText()
            logDebug $"{generated}"
            let answer = """
[sys] MySystem = {
    [flow] F1 = {
            R1; // island
    }
    [calls] = {
        Ap = { A."+"(%Q1, %I1); }
        Am = { A."-"(%Q2, %I2); }
    }
    [device file="F:\Git\ds\DsDotNet\src\UnitTest\UnitTest.Engine\Model\..\Libraries\cylinder.ds"] A;
    [device file="F:\Git\ds\DsDotNet\src\UnitTest\UnitTest.Engine\Model\..\Libraries\cylinder.ds"] B;
}
"""
            ()


        //[<Test>]
        //member __.``XMeta Builder test`` () =
        //    let sys =
        //        system {
        //            name "My"
        //            let f2 = flow { name "F2" }
        //            let xxx = f2.Aliases
        //            add_flow f2
        //            let n = "F"
        //            add_flow (
        //                flow {
        //                    name "F1"
        //                })
        //        }
        //    ()