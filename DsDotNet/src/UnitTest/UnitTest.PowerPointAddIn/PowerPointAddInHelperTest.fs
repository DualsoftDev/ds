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

    let testPath = @$"{__SOURCE_DIRECTORY__}../../../../bin/net7.0-windows/HelloDS.pptx";
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
    let ``MSG_GENIOLIST`` () =
        setXGK()
        MSG_GENIOLIST.Do(testPath, false)|> Assert.True
    [<Fact>]
    let ``MSG_GENLSPLC XGI`` () =
        setXGI()
        MSG_GENLSPLC.Do(testPath, "", false)|> Assert.True
    [<Fact>]
    let ``MSG_GENLSPLCEMULATION XGI`` () =
        setXGI()
        MSG_GENLSPLCEMULATION.Do(testPath, "", false)|> Assert.True
    [<Fact>]
    let ``MSG_GENLSPLC XGK`` () =
        setXGK()
        MSG_GENLSPLC.Do(testPath, "", false)|> Assert.True
    [<Fact>]
    let ``MSG_GENLSPLCEMULATION XGK`` () =
        setXGK()
        MSG_GENLSPLCEMULATION.Do(testPath, "",false)|> Assert.True
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
    let ``MSG_SIMULATION`` () =
        setXGK()
        MSG_SIMULATION.Do(testPath, SimViewEnum.FromPptPage, false, false)|> Assert.True
    [<Fact>]
    let ``MSG_ANIMATION`` () =
        MSG_ANIMATION.Do(testPath, false)|> Assert.True
    [<Fact>]
    let ``MSG_TIMECHART`` () =
        setXGK()
        MSG_TIMECHART.Do(testPath, false)|> Assert.True
    [<Fact>]
    let ``MSG_LAYOUT`` () =
        MSG_LAYOUT.Do(testPath, false)|> Assert.True