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
        member __.``Builder test`` () =
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
                system {
                    name "MySystem"
                    path [libdir]
                    device "A" "cylinder.ds"
                    device "B" "cylinder.ds"
                    call "Ap" [ { Api=("A", "+"); Tx="%Q1"; Rx="%I1" } ]
                    call "Am" [ { Api=("A", "-"); Tx="%Q2"; Rx="%I2" } ]

                    flow "F1"
                    real "F1" "R1"
                }

            let generated = system.ToDsText()
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
