namespace UnitTest.Engine

open System.Linq
open Engine.Core
open NUnit.Framework


[<AutoOpen>]
module LoadConfigTestModule =
    type LoadConfigTest() =
        do
            Fixtures.SetUpTest()

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
            model.Systems.Select(fun s -> s.Name) |> SeqEq ["Cylinder"; "Station"]

