namespace T.IOHub

open T
open NUnit.Framework
open System.Collections.Generic
open FsUnit.Xunit
open Dual.Common.Core.FS
open System
open System.IO
open IO.Core
open Newtonsoft.Json

[<AutoOpen>]
module JSONSettingTestModule =
    
    [<TestFixture>]
    type JSONSettingTest() =
        inherit TestBaseClass("IOHubLogger")


        [<Test>]
        member _.ParseJSON() =
            let ioSpec:IOSpec =
                __SOURCE_DIRECTORY__ + @"/../../IOHub/IO.Core/zmqsettings.json"
                |> File.ReadAllText
                |> JsonConvert.DeserializeObject<IOSpec>
            ioSpec.Regulate()

            ()
