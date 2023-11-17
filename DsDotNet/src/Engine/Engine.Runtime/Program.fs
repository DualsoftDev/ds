namespace Engine.Runtime

module testMain =

    [<EntryPoint>]
    let main _ = 
        let testFile = @$"{__SOURCE_DIRECTORY__}../../../UnitTest/UnitTest.Model/ImportOfficeExample/exportDS.Zip"
        let testRuntimeModel = new RuntimeModel(testFile)
        let _ = testRuntimeModel.HMITagPackage
        0