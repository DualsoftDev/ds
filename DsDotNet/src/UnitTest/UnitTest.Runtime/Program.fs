
open Engine.Core
open Engine.Parser.FS
open Engine.Runtime

module testMain =

    [<EntryPoint>]
    let main _ = 
        RuntimeDS.Package <- RuntimePackage.StandardPC
        
        //test me ds.zip 
        let testFile = @$"{__SOURCE_DIRECTORY__}../../../UnitTest/UnitTest.Model/ImportOfficeExample/exportDS.Zip"
        let testRuntimeModel = new RuntimeModel(testFile)

        //test me dualsoft.json 
        let testFile = @"C:\Users\안승훈\videos\dualsoft.json"
        let model = ParserLoader.LoadFromConfig testFile
     

        0