namespace UnitTest.Runtime

open System
open Xunit
open System.Reflection
open System.IO
open Engine.Cpu
open Engine.Core
open Engine.Runtime
open Engine.Import.Office

module RuntimeTest = 
    
    [<Fact>]
    let ``Runtime Create Test`` () = 
        let testPPT = Path.GetFullPath @$"{__SOURCE_DIRECTORY__}../../../UnitTest/UnitTest.Model/ImportOfficeExample/exportDS/testA/testMy/my.pptx"
        RuntimeDS.IP  <- "192.168.9.100"

        let zipPath = ImportPPT.GetRuntimeZipFromPPT testPPT
        let testRuntimeModel = new RuntimeModel(zipPath)

        testRuntimeModel.HMITagPackage.RealBtns.Length > 0   |> Assert.True
  