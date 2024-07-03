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
    RegistryPPTDS.TimeSimutionMode <-  TimeSimutionMode.TimeX1.ToString();


    [<Fact>]
    let ``MSG_CHECK`` () =
        MSG_CHECK.Do(testPath, false) |> Assert.True
    [<Fact>]
    let ``MSG_EXPORT`` () =
        MSG_DSEXPORT.Do(testPath, false)|> Assert.True
    [<Fact>]
    let ``MSG_GENIOLIST`` () =
        MSG_GENIOLIST.Do(testPath, false)|> Assert.True
    [<Fact>]
    let ``MSG_GENLSPLC XGI`` () =
        RegistryPPTDS.PagePlatformTarget <- PlatformTarget.XGI.ToString();
        MSG_GENLSPLC.Do(testPath, "", false)|> Assert.True    
    [<Fact>]
    let ``MSG_GENLSPLCEMULATION XGI`` () =
        RegistryPPTDS.PagePlatformTarget <- PlatformTarget.XGI.ToString();
        MSG_GENLSPLCEMULATION.Do(testPath, "", false)|> Assert.True  
    [<Fact>]
    let ``MSG_GENLSPLC XGK`` () =
        RegistryPPTDS.PagePlatformTarget <- PlatformTarget.XGK.ToString();
        MSG_GENLSPLC.Do(testPath, "", false)|> Assert.True    
    [<Fact>]
    let ``MSG_GENLSPLCEMULATION XGK`` () =
        RegistryPPTDS.PagePlatformTarget <- PlatformTarget.XGK.ToString();
        MSG_GENLSPLCEMULATION.Do(testPath, "",false)|> Assert.True
    [<Fact>]
    let ``MSG_GENWINPC`` () =
        MSG_GENWINPC.Do(testPath,"",  false)|> Assert.True
    [<Fact>]
    let ``MSG_PLCIOCSV`` () =
        MSG_PLCIOCSV.Do(testPath, "", false)|> Assert.True
    [<Fact>]
    let ``MSG_HWSETTING`` () =
        MSG_HWSETTING.Do("192.168.9.100", false)|> Assert.True
    [<Fact>]
    let ``MSG_SIMULATION`` () =
        MSG_SIMULATION.Do(testPath, SimViewEnum.FromPPTPage,   false)|> Assert.True
    [<Fact>]
    let ``MSG_ANIMATION`` () =
        MSG_ANIMATION.Do(testPath, false)|> Assert.True
    [<Fact>]
    let ``MSG_TIMECHART`` () =
        MSG_TIMECHART.Do(testPath, false)|> Assert.True
    [<Fact>]
    let ``MSG_LAYOUT`` () =
        MSG_LAYOUT.Do(testPath, false)|> Assert.True