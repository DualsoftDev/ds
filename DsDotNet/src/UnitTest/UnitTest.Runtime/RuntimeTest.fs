namespace UnitTest.Runtime

open System
open Xunit
open System.Reflection
open System.IO
open Engine.Cpu
open Engine.Core
open Engine.Runtime
open Engine.Import.Office
open Engine.Info
open DsWebApp.Simulator

module RuntimeTest = 
    let testPPT =  @$"{__SOURCE_DIRECTORY__}../../../UnitTest/UnitTest.Model/ImportOfficeExample/exportDS/testA/testMy/my.pptx"
    RuntimeDS.IP  <- "192.168.9.100"
    RuntimeDS.Package <- RuntimePackage.Simulation
    let zipPath = ImportPPT.GetRuntimeZipFromPPT testPPT
    let runtimeModel = new RuntimeModel(zipPath)

    [<Fact>]
    let ``Runtime Create Test`` () = 
        
        runtimeModel.HMIPackage.Devices.Length > 0   |> Assert.True

    [<Fact>]
    let ``Runtime Running Test`` () = 

        let systems = [| runtimeModel.System|]
        let commonAppSettings = DSCommonAppSettings.Load(Path.Combine(AppContext.BaseDirectory, "CommonAppSettings.json"));

        let mci = ModelCompileInfo(runtimeModel.JsonPath, runtimeModel.JsonPath)
        DBLogger.InitializeLogWriterOnDemandAsync(commonAppSettings, systems, mci) |> ignore
        DsSimulator.Do(runtimeModel.Cpu) |> ignore

        runtimeModel.HMIPackage.Devices.Length > 0   |> Assert.True
        
  