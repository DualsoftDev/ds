namespace T

open NUnit.Framework

open Engine.Core
open Dual.Common.Core.FS
open Engine.Parser.FS

[<AutoOpen>]
module SystemBuilderTest =
    ()
//    type SystemBuilderTest() =
//        do
//            Fixtures.SetUpTest()

//        let libdir = @$"{__SOURCE_DIRECTORY__}/../Libraries"
//        let compare = compare libdir
//        let compareExact x = compare x x
//        [<Test>]
//        member __.``Builder test`` () =
//            let opt =
//                option {
//                    if true then
//                        return 1
//                    else
//                        return! None
//                }
//            //let sys2 =
//            //    system {
//            //        //if true then
//            //        //    ()
//            //        //else
//            //        //    ()
//            //        let! f = get_flow "F1"
//            //        name "MySystem"
//            //    }


//            let system =
//                let sysCore = DsSystem("MySystem", "localhost")
//                withSystem sysCore {
//                    name "MySystem"
//                    path [libdir]
//                    device "A" "cylinder.ds"
//                    device "B" "cylinder.ds"
//                    //call "Ap" [ { Api=("A", "+"); Tx="%Q1"; Rx="%I1" } ]
//                    //call "Am" [ { Api=("A", "-"); Tx="%Q2"; Rx="%I2" } ]

//                    //let flowCore = Flow.Create("F1", sysCore)
//                    //let flow = withFlow flowCore {
//                    //    real "R1"
//                    //    real "R2"
//                    //}
//                    //create_flow flow


//                    //withFlow (Flow.Create("F3", sysCore)) {
//                    //    real "R1"
//                    //    real "R2"
//                    //}

//                    flow "F2"
//                    //{
//                    //    real "R1"
//                    //    real "R2"
//                    //}


//                    //create_flow "F2"

//                    //create_flow_from_obj (
//                    //    //flow (getNull<DsSystem>()) {
//                    //    flow "F1" sysCore {
//                    //        name "F1"
//                    //    })
//                }

//            let generated = system.ToDsText(true)
//            logDebug $"{generated}"
//            let answer = """
//[sys] MySystem = {
//    [flow] F1 = {
//            R1; // island
//    }
//    [jobs] = {
//        Ap = { A."+"(%Q1, %I1); }
//        Am = { A."-"(%Q2, %I2); }
//    }
//    [device file="Z:\ds\DsDotNet\src\UnitTest\UnitTest.Engine\Model\..\Libraries\cylinder.ds"] A;
//    [device file="Z:\ds\DsDotNet\src\UnitTest\UnitTest.Engine\Model\..\Libraries\cylinder.ds"] B;
//}
//"""
//            ()


//        //[<Test>]
//        //member __.``XMeta Builder test`` () =
//        //    let sys =
//        //        system {
//        //            name "My"
//        //            let f2 = flow { name "F2" }
//        //            let xxx = f2.Aliases
//        //            add_flow f2
//        //            let n = "F"
//        //            add_flow (
//        //                flow {
//        //                    name "F1"
//        //                })
//        //        }
//        //    ()