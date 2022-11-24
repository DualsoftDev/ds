namespace UnitTest.Engine


open Engine.Core
open Engine.Common.FS
open NUnit.Framework


[<AutoOpen>]
module LoadConfigTestModule =
    type LoadConfigTest() =
        inherit TestBase()

        let configFile = @"test-model-config.json"

        let loadConfigTest() =
            let cfg =
                {   DsFilePaths = [
                        @"F:\Git\ds\DsDotNet\src\UnitTest\UnitTest.Engine\Libraries\cylinder.ds"
                        @"F:\Git\ds\DsDotNet\src\UnitTest\UnitTest.Engine\Libraries\station.ds" ] }
            ModelLoader.saveConfig configFile cfg
            let cfg2 = ModelLoader.loadConfig configFile
            cfg === cfg2
            cfg

        [<Test>]
        member __.``LoadModelFromConfigTest`` () =
            let config = loadConfigTest()
            let model = ModelLoader.loadFromConfig configFile
            model.Systems.Length === 2

