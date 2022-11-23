namespace UnitTest.Engine


open Engine.Core
open Engine.Common.FS
open NUnit.Framework


[<AutoOpen>]
module LoadConfigTestModule =
    type LoadConfigTest() =
        do Fixtures.SetUpTest()

        [<Test>]
        member __.``LoadConfigTest test`` () =
            TestLoadConfig.testme()

            let cfg =
                {   DsFilePaths = [
                        @"F:\Git\ds\DsDotNet\src\UnitTest\UnitTest.Engine\Libraries\cylinder.ds"
                        @"F:\Git\ds\DsDotNet\src\UnitTest\UnitTest.Engine\Libraries\station.ds" ] }
            let fp = @"test-model-config.json"
            saveConfig fp cfg
            let cfg2 = loadConfig fp
            cfg === cfg2




