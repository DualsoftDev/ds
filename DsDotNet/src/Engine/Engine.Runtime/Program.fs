namespace Engine.Runtime

open Engine.Core
open Engine.Parser.FS

module testMain =

    [<EntryPoint>]
    let main _ = 
        let testFile = @$"{__SOURCE_DIRECTORY__}../../../UnitTest/UnitTest.Model/ImportOfficeExample/exportDS.dsz"
        let testRuntimeModel = new RuntimeModel(testFile, (WINDOWS))
        let _ = testRuntimeModel.HMIPackage

        0