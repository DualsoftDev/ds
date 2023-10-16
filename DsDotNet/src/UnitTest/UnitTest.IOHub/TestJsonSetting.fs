namespace T.IOHub

open T
open NUnit.Framework
open System
open IO.Core

[<AutoOpen>]
module JSONSettingTestModule =
    
    [<TestFixture>]
    type JSONSettingTest() =
        inherit TestBaseClass("IOHubLogger")


        [<Test>]
        member _.ParseJSON() =
            let zmqInfo = Zmq.Initialize (__SOURCE_DIRECTORY__ + @"/../../IOHub/IO.Core/zmqsettings.json")
            let server, client, cts = zmqInfo.Server, zmqInfo.Client, zmqInfo.CancellationTokenSource
            let venders = zmqInfo.IOSpec.Vendors

            // Paix(=>"p") 의 o 파일
            let p = venders |> Array.find (fun (v:VendorSpec) -> v.Location = "p")
            let po = p.Files |> Array.find (fun (v:IOFileSpec) -> v.Name = "o")
            let indices = [|0..po.Length-1|]
            let values = indices |> Array.map (fun i -> i |> uint8 |> byte)


            client.WriteBytes("p/o", indices, values)
            let obs:byte[] = client.ReadBytes("p/o", indices)
            SeqEq obs values
            ()
