namespace T.CPU

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

[<AutoOpen>]
module RuntimeTestCommon =

    let getRuntimeModelForSim(pptPath) =
        RuntimeDS.Package <- PCSIM
        let helloDSPath = pptPath
        let pptParms:PptParams =  defaultPptParams()
        let modelConfig = createDefaultModelConfig() 
        let newCng = { modelConfig with HwDriver = pptParms.HwTarget.HwDrive.ToString()}

        let zipPath, sys = ImportPpt.GetRuntimeZipFromPpt (helloDSPath, pptParms, newCng)
        let runtimeModel = new RuntimeModel(zipPath, pptParms.HwTarget.Platform)

        (*시뮬레이션 구동 테스트*)
        let systems = [| runtimeModel.System|]
        let commonAppSettings = DSCommonAppSettings.Load(Path.Combine(AppContext.BaseDirectory, "CommonAppSettings.json"));
        let loggerDBSettings = commonAppSettings.LoggerDBSettings
        loggerDBSettings.ModelFilePath <- runtimeModel.SourceDsZipPath
        loggerDBSettings.DbWriter <- "PCSIM"

        // 기존 db log 가 삭제되는 것을 방지하기 위해서 test 용으로 따로 database 설정
        loggerDBSettings.ConnectionPath <- Path.Combine(AppContext.BaseDirectory, "TmpLogger.sqlite3")

        let cleanExistingDb = true      //DB TAGKind 코드변경 반영하기 위해 이전 DB 있으면 삭제
        let queryCriteria = new QueryCriteria(commonAppSettings, -1, DateTime.Now.Date.AddDays(-1), Nullable<DateTime>());
        DbWriter.CreateAsync(queryCriteria, systems, cleanExistingDb).Wait()
        let hasChangedVaules = DsSimulator.Do(runtimeModel.Cpu) 

        runtimeModel, loggerDBSettings.ConnectionPath, hasChangedVaules