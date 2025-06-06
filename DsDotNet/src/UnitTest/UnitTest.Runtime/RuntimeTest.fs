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
open T.CPU

module RuntimeTest =


    [<Fact>]
    let ``Runtime Running Test`` () =
        let runtimeModel, pathDB, hasChangedVaules =
            let testPpt =  @$"{__SOURCE_DIRECTORY__}../../../UnitTest/UnitTest.Model/ImportOfficeExample/sampleA/exportDS/testA/testMy/my.pptx"
            RuntimeTestCommon.getRuntimeModelForSim  testPpt

        (*시뮬레이션 구동 테스트*)
        hasChangedVaules |> Assert.True //값변경있으면서 구동하면 true


        (*DB 로깅 구동 테스트*)
        let info =  runtimeModel.System.GetInfo()
        let options = JsonSerializerOptions()
        options.NumberHandling <- JsonNumberHandling.AllowNamedFloatingPointLiterals
        let json = JsonSerializer.Serialize(info, options)
        let data = JsonSerializer.Deserialize(json, options)
        info.Name = runtimeModel.System.Name |> Assert.True

