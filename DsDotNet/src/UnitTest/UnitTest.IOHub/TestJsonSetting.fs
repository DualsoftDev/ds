namespace T.IOHub

open T
open NUnit.Framework
open Dual.Common.Core.FS
open System
open IO.Core
open System.Threading
open IO.Core.ZmqServerModule
open IO.Core.ZmqClientModule

[<AutoOpen>]
module JSONSettingTestModule =
    
    [<TestFixture>]
    type JSONSettingTest() =
        inherit TestBaseClass("IOHubLogger")


        [<Test>]
        member _.ParseJSON() =
            let zmqInfo = Zmq.Initialize (__SOURCE_DIRECTORY__ + @"/../../IOHub/IO.Core/zmqsettings.json")
            ()
