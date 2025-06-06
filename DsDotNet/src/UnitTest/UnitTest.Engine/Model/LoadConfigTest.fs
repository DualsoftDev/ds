namespace T
open Dual.Common.UnitTest.FS

open System.Linq
open Engine.Core
open NUnit.Framework
open Engine.Parser.FS


[<AutoOpen>]
module LoadConfigTestModule =
    type LoadConfigTest() =
        inherit EngineTestBaseClass()

        let dsFileDir = PathManager.combineFullPathDirectory ([|@$"{__SOURCE_DIRECTORY__}"; "../../UnitTest.Model/UnitTestExample/dsSimple"|])
        let configFile = PathManager.getFullPath  ( @"modelConfig.json"|>DsFile) (dsFileDir.ToDirectory())
        
      
        [<Test>]
        member __.``LoadModelFromConfigTest`` () =

            let model = ParserLoader.LoadFromConfig configFile HwCPU.WINDOWS  
            model.System.Name === "Factory"




        [<Test>]
        member __.``LoadSharedDevices Singleton Test`` () =

            let systemRepo = ShareableSystemRepository()

            let mySysText = """
[sys] L = {
    [external file="sub/sub/sub/RH.ds"] A;
    [external file="sub/sub/sub/RH.ds"] B;
}
"""



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

            let generated = system.ToDsText(true, false);
            compare systemRepo dsFileDir mySysText generated
            ()