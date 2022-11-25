namespace UnitTest.Engine

open System.Linq
open Engine.Core
open NUnit.Framework
open Engine.Parser.FS


[<AutoOpen>]
module LoadConfigTestModule =
    type LoadConfigTest() =
        do
            Fixtures.SetUpTest()

        let libdir = @$"{__SOURCE_DIRECTORY__}\..\Libraries"
        let configFile = @"test-model-config.json"

        let loadConfigTest() =
            let cfg =
                {   DsFilePaths = [
                        $@"{libdir}\cylinder.ds"
                        $@"{libdir}\station.ds" ] }
            ModelLoader.SaveConfig configFile cfg
            let cfg2 = ModelLoader.LoadConfig configFile
            cfg === cfg2
            cfg

        [<Test>]
        member __.``LoadModelFromConfigTest`` () =
            let config = loadConfigTest()
            let model = ModelLoader.LoadFromConfig configFile
            model.Systems.Length === 2
            model.Systems.Select(fun s -> s.Name) |> SeqEq ["Cylinder"; "Station"]

        [<Test>]
        member __.``LoadSharedDevicesTest`` () =

            DsSystem.ClearExternalSystemCaches()

            let mySysText = """
[sys] L = {
    [external file="station.ds" ip="localhost"] A;
    [external file="station.ds" ip="localhost"] B;
}
"""

            let helper = ModelParser.ParseFromString2(mySysText, ParserOptions.Create4Simulation(libdir, "ActiveCpuName"))
            let system = helper.TheSystem

            validateGraphOfSystem system

            let caches = DsSystem.ExternalSystemCaches
            caches.Count() === 1

            let exs = system.Devices.Select(fun d -> d.ReferenceSystem).ToArray()
            exs.Length === 2
            exs[0] === exs[1]
            exs.Distinct().Count() === 1
            system.Devices.Select(fun d -> d.Name) |> Seq.sort |> SeqEq ["A"; "B"]
            exs[0].Name === "Station"

            let generated = system.ToDsText();
            ()