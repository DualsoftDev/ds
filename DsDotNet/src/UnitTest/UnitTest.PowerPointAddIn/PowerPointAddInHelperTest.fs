namespace UnitTest.PowerPointAddIn

open System
open Xunit
open PowerPointAddInHelper.MSG
open PowerPointAddInHelper
open PowerPointAddInShared
open System.Reflection
open System.IO
open PowerPointAddInForDualsoft
open Engine.Core

module MSG_TEST =

    let testPath = @$"{__SOURCE_DIRECTORY__}../../../../bin/net8.0-windows/HelloDS.pptx";
    RegistryPptDS.TimeSimutionMode <-  TimeSimutionModeExtensions.toString(TimeSimutionMode.TimeX1)
    let setXGK() = 
        RegistryPptDS.PagePlatformTarget <- PlatformTarget.XGK.ToString();
        RegistryPptDS.HwDriver <- HwDriveTarget.LS_XGK_IO.ToString();
    let setXGI() = 
        RegistryPptDS.PagePlatformTarget <- PlatformTarget.XGI.ToString();
        RegistryPptDS.HwDriver <- HwDriveTarget.LS_XGI_IO.ToString();

    [<Fact>]
    let ``MSG_CHECK`` () =
        MSG_CHECK.Do(testPath, false) |> Assert.True
    [<Fact>]
    let ``MSG_EXPORT`` () =
        MSG_DSEXPORT.Do(testPath, false)|> Assert.True
    [<Fact>]
    let ``MSG_GENWINPC`` () =
        MSG_GENWINPC.Do(testPath, false)|> Assert.True
    [<Fact>]
    let ``MSG_PLCIOCSV`` () =
        MSG_PLCIOCSV.Do(testPath, "", false)|> Assert.True
    [<Fact>]
    let ``MSG_HWSETTING`` () =
        MSG_HWSETTING.Do(false)|> Assert.True
    [<Fact>]
    let ``MSG_ANIMATION`` () =
        MSG_ANIMATION.Do(testPath, false)|> Assert.True
    [<Fact>]
    let ``MSG_LAYOUT`` () =
        MSG_LAYOUT.Do(testPath, false)|> Assert.True

    [<Fact>]
    let ``MSG_XGT`` () =
        let testXGT() =
            MSG_GENLSPLCEMULATION.Do(testPath, "", false)|> Assert.True
            MSG_GENIOLIST.Do(testPath, false)|> Assert.True
            MSG_GENLSPLC.Do(testPath, "", false)|> Assert.True
            MSG_GENLSPLCEMULATION.Do(testPath, "",false)|> Assert.True
            MSG_TIMECHART.Do(testPath, false)|> Assert.True

            //test ahn simulation 로딩 테스트 다른방식 사용필요
            //MSG_SIMULATION.Do(testPath, SimViewEnum.FromPptPage, false, false)|> Assert.True
        
        setXGK()  //XGK 기준으로 테스트
        testXGT()
        setXGI()  //XGI 기준으로 테스트
        testXGT()
      