namespace T
open Dual.UnitTest.Common.FS

open System.Linq
open Engine.Core
open NUnit.Framework
open Engine.Parser.FS


[<AutoOpen>]
module LoadConfigTestModule =
    type LoadConfigTest() =
        inherit EngineTestBaseClass()

        let dsFileDir = PathManager.combineFullPathDirectory ([|@$"{__SOURCE_DIRECTORY__}"; "../../UnitTest.Model/UnitTestExample"|])
        let configFile = PathManager.getFullPath  ( @"test-model-config.json"|>DsFile) (dsFileDir.ToDirectory())
        
        let loadConfigTest() =
            let cfg =
                {   DsFilePath =  $@"{dsFileDir}dsFolder/lib/Cylinder/Double.ds" 
                    HWIP =  RuntimeDS.IP
                    }
            ModelLoader.SaveConfig configFile cfg
            let cfg2 = ModelLoader.LoadConfig configFile
            cfg === cfg2
            cfg

        [<Test>]
        member __.``LoadModelFromConfigTest`` () =
            let config = loadConfigTest()

            let model = ParserLoader.LoadFromConfig configFile
            model.System.Name === "Double"


        [<Test>]
        member __.``LoadFolderModelFromConfigTest`` () =
            let configPath = PathManager.getFullPath ( @"dsFolder.json"|>DsFile) (dsFileDir.ToDirectory())

            let model = ParserLoader.LoadFromConfig configPath
            model.LoadingPaths.Length === 9
            model.System.Name === "Factory"


        [<Test>]
        member __.``LoadSharedDevices Singleton Test`` () =

            let systemRepo = ShareableSystemRepository()

            let mySysText = """
[sys] L = {
    [external file="dsFolder/sub/sub/sub/RH.ds"] A;
    [external file="dsFolder/sub/sub/sub/RH.ds"] B;
}
"""

//file="station.ds"
//[sys] Station = {
    //[flow] F = {
    //    Vp > Pp > Sp;
    //    Vm > Pm > Sm;
    //} ....

            let system = parseText systemRepo dsFileDir mySysText
            validateGraphOfSystem system

            systemRepo.Count === 1

            let exs = system.LoadedSystems.Select(fun d -> d.ReferenceSystem).ToArray()
            exs.Length === 2
            exs[0] === exs[1]
            exs.Distinct().Count() === 1
            system.LoadedSystems.Select(fun d -> d.Name) |> Seq.sort |> SeqEq ["A"; "B"]
            exs[0].Name === "RH"


            let findFromLoaded = tryFindLoadedSystem system  "A" |> Option.get
            let findReferenceSystem = tryFindReferenceSystem system  "RH" |> Option.get
            findFromLoaded.Name =!= findReferenceSystem.Name

            let generated = system.ToDsText(true);
            compare systemRepo dsFileDir mySysText generated
            ()