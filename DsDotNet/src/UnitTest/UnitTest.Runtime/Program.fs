
open Engine.Core
open Engine.Parser.FS
open Engine.Runtime

module testMain =

    [<EntryPoint>]
    let main _ = 
        RuntimeDS.Package <- RuntimePackage.PC
        
        //test me ds.zip 
        let testFile = @$"{__SOURCE_DIRECTORY__}../../../UnitTest/UnitTest.Model/ImportOfficeExample/exportDS.dsz"
        let testRuntimeModel = new RuntimeModel(testFile, WINDOWS)

        //test me dualsoft.json 
        let testFile = @"C:\Users\안승훈\videos\dualsoft.json"
        let model = ParserLoader.LoadFromConfig testFile
     

        0