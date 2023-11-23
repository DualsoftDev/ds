namespace UnitTest.PowerPointAddIn

open System
open Xunit
open PowerPointAddInHelper.MSG
open PowerPointAddInHelper

module MSG_TEST = 
    let testPath = @$"{__SOURCE_DIRECTORY__}/sample/SamplePowerPointAddIn.pptx"
    [<Fact>]
    let ``MSG_ANIMATION`` () =
        MSG_ANIMATION.Do(testPath,"")
    [<Fact>]
    let ``MSG_CHECK`` () =
        MSG_CHECK.Do(testPath, false)
    [<Fact>]
    let ``MSG_EXPORT`` () =
        MSG_EXPORT.Do(testPath, false)
    [<Fact>]
    let ``MSG_GENIOLIST`` () =
        MSG_GENIOLIST.Do(testPath, false)
    [<Fact>]
    let ``MSG_GENLSPLC`` () =
        MSG_GENLSPLC.Do(testPath)
    [<Fact>]
    let ``MSG_GENWINPC`` () =
        MSG_GENWINPC.Do(testPath, false)
    [<Fact>]
    let ``MSG_IOCSV`` () =
        MSG_IOCSV.Do(testPath)
    [<Fact>]
    let ``MSG_SIMULATION`` () =
        MSG_SIMULATION.Do(testPath, "", false)
        
