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
open Engine.TestSimulator
open System.Text.Json
open System.Text.Json.Serialization

module RuntimeTest = 
    let testPPT =  @$"{__SOURCE_DIRECTORY__}../../../UnitTest/UnitTest.Model/ImportOfficeExample/exportDS/testA/testMy/my.pptx"
    RuntimeDS.IP  <- "192.168.9.100"
    RuntimeDS.Package <- RuntimePackage.Simulation
    let zipPath = ImportPPT.GetRuntimeZipFromPPT testPPT
    let runtimeModel = new RuntimeModel(zipPath)

   
    [<Obsolete("테트스 성공/실패가 random 임.  수정 필요")>]
    [<Fact>]
    let ``Runtime Running Test`` () = 

        (*시뮬레이션 구동 테스트*)
        let systems = [| runtimeModel.System|]
        let commonAppSettings = DSCommonAppSettings.Load(Path.Combine(AppContext.BaseDirectory, "CommonAppSettings.json"));
        let mci = ModelCompileInfo(runtimeModel.JsonPath, runtimeModel.JsonPath)
        DBLogger.InitializeLogWriterOnDemandAsync(commonAppSettings, systems, mci).Wait()
        DsSimulator.Do(runtimeModel.Cpu) |> Assert.True //값변경있으면서 구동하면 true


        (*DB 로깅 구동 테스트*)
        let info =  runtimeModel.System.GetInfo()
        let options = JsonSerializerOptions()
        options.NumberHandling <- JsonNumberHandling.AllowNamedFloatingPointLiterals
        let json = JsonSerializer.Serialize(info, options)
        let data = JsonSerializer.Deserialize(json, options)      
        info.Name = runtimeModel.System.Name |> Assert.True

        
  