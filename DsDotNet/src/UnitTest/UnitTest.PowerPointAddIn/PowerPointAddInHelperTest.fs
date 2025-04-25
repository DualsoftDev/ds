namespace UnitTest.PowerPointAddIn

open System
open Xunit
open PowerPointAddInHelper.MSG
open PowerPointAddInHelper
open System.Reflection
open System.IO
open PowerPointAddInForDualsoft
open Engine.Core
open Newtonsoft.Json
open PowerPointAddInHelper.Helper
open Engine.CodeGenCPU

module MSG_TEST =

    let testPath = @$"{__SOURCE_DIRECTORY__}../../../../bin/net8.0-windows/HelloDS.pptx";


    [<Fact>]
    let ``MSG_GENWINPC`` () =
        MSG_GENWINPC.Do(testPath, false)|> Assert.True
    [<Fact>]
    let ``MSG_ANIMATION`` () =
        MSG_ANIMATION.Do(testPath, false)|> Assert.True
    [<Fact>]
    let ``MSG_LAYOUT`` () =
        MSG_LAYOUT.Do(testPath, false)|> Assert.True
