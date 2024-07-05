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
    RuntimeDS.Package <- RuntimePackage.PCSIM
    let pptParms:PPTParams = {TargetType = WINDOWS; AutoIOM = true; CreateBtnLamp = true}

    let zipPath = ImportPPT.GetRuntimeZipFromPPT (testPPT, pptParms)
    let runtimeModel = new RuntimeModel(zipPath, pptParms.TargetType)

   
    [<Fact>]
    let ``Runtime Running Test`` () = 
        
        (*시뮬레이션 구동 테스트*)
        let systems = [| runtimeModel.System|]
        let commonAppSettings = DSCommonAppSettings.Load(Path.Combine(AppContext.BaseDirectory, "CommonAppSettings.json"));

        // 기존 db log 가 삭제되는 것을 방지하기 위해서 test 용으로 따로 database 설정
        commonAppSettings.LoggerDBSettings.ConnectionPath <- Path.Combine(AppContext.BaseDirectory, "TmpLogger.sqlite3")

        let mci = ModelCompileInfo(runtimeModel.JsonPath, runtimeModel.JsonPath)
        let cleanExistingDb = true      //DB TAGKind 코드변경 반영하기 위해 이전 DB 있으면 삭제
        DBLogger.InitializeLogWriterOnDemandAsync(commonAppSettings, systems, mci, cleanExistingDb).Wait()
        DsSimulator.Do(runtimeModel.Cpu, 3000) |> Assert.True //값변경있으면서 구동하면 true


        (*DB 로깅 구동 테스트*)
        let info =  runtimeModel.System.GetInfo()
        let options = JsonSerializerOptions()
        options.NumberHandling <- JsonNumberHandling.AllowNamedFloatingPointLiterals
        let json = JsonSerializer.Serialize(info, options)
        let data = JsonSerializer.Deserialize(json, options)      
        info.Name = runtimeModel.System.Name |> Assert.True

        
  