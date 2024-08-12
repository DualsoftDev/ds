namespace Engine.Runtime

open Engine.Core
open Engine.Parser.FS

module testMain =

    [<EntryPoint>]
    let main _ = 
        let testFile = @$"{__SOURCE_DIRECTORY__}../../../UnitTest/UnitTest.Model/ImportOfficeExample/exportDS.Zip"
        RuntimeDS.Package <- RuntimePackage.PC
        let testRuntimeModel = new RuntimeModel(testFile, (WINDOWS))
        let _ = testRuntimeModel.HMIPackage

        0