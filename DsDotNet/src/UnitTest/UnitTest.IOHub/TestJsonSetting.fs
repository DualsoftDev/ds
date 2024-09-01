namespace T.IOHub

open System
open System.IO
open Dual.Common.UnitTest.FS
open NUnit.Framework
open IO.Core
open Xunit
open Dual.Common.Core.FS.Prelude
open Dual.Common.Core.FS

[<AutoOpen>]
module JSONSettingTestModule =
    
    [<Collection("ZmqTesting")>]
    [<TestFixture>]
    type JSONSettingTest() =
        inherit TestClassWithLogger(Path.Combine($"{__SOURCE_DIRECTORY__}/App.config"), "IOHubLogger")

        let checkOk x =
            match x with
            | Ok _ -> ()
            | _ -> failwithlog "ERROR"

        [<Test>]
        member _.Endian() =
            BitConverter.IsLittleEndian === true    // for intel architecture

            let n8 = 0x07_06_05_04_03_02_01_00UL
            let bytes = BitConverter.GetBytes(n8)
            n8 === BitConverter.ToUInt64(bytes, 0)
            bytes[0] === 0x00uy
            bytes[1] === 0x01uy
            bytes[2] === 0x02uy
            bytes[3] === 0x03uy
            bytes[4] === 0x04uy
            bytes[5] === 0x05uy
            bytes[6] === 0x06uy
            bytes[7] === 0x07uy

            noop()

        [<Test>]
        member x.ReadWriteWholeFiles() =
            lock x.Locker (fun () ->
                let zmqInfo = Zmq.Initialize (__SOURCE_DIRECTORY__ + @"/../../IOHub/IO.Core/zmqsettings.json")
                let server, client, cts = zmqInfo.Server, zmqInfo.Client, zmqInfo.CancellationTokenSource
                let venders = zmqInfo.IOSpec.Vendors

                let checkSubFile() =
                    // Paix(=>"p") 의 o 파일
                    let p = venders |> Array.find (fun (v:VendorSpec) -> v.Location = "p")
                    let po = p.Files |> Array.find (fun (v:IOFileSpec) -> v.Name = "o")

                    let check_po_bits() =
                        ()
                        let indices = [|0.. po.Length|]
                        let values = indices |> map (fun i -> i % 2 = 0)

                        client.ClearAll("p/o") |> checkOk
                        client.WriteBits("p/o", indices, values) |> checkOk
                        match client.ReadBits("p/o", indices) with
                        | Ok bits ->
                            for (i, b) in bits |> Array.indexed do
                                (i % 2 = 0) === b
                        | _ ->
                            failwithlog "ERROR"

                    let check_po_bytes() =
                        let indices = [|0..po.Length-1|]
                        let values = indices |> map (uint8 >> byte)

                        client.ClearAll("p/o") |> checkOk
                        client.WriteBytes("p/o", indices, values) |> checkOk
                        match client.ReadBytes("p/o", indices) with
                        | Ok obs -> SeqEq obs values
                        | _ -> failwithlog "ERROR"

                    let check_po_words() =
                        let indices = [|0..(po.Length / 2)-1|]
                        let values = indices |> map uint16

                        client.ClearAll("p/o") |> checkOk
                        client.WriteUInt16s("p/o", indices, values) |> checkOk
                        match client.ReadUInt16s("p/o", indices) with
                        | Ok ows -> SeqEq ows values
                        | _ -> failwithlog "ERROR"

                    let check_po_dwords() =
                        let indices = [|0..(po.Length / 4)-1|]
                        let values = indices |> map uint32

                        client.ClearAll("p/o") |> checkOk
                        client.WriteUInt32s("p/o", indices, values) |> checkOk
                        match client.ReadUInt32s("p/o", indices) with
                        | Ok ows -> SeqEq ows values
                        | _ -> failwithlog "ERROR"

                    let check_po_lwords() =
                        let indices = [|0..(po.Length / 8)-1|]
                        let values = indices |> map uint64

                        client.ClearAll("p/o") |> checkOk
                        client.WriteUInt64s("p/o", indices, values) |> checkOk
                        match client.ReadUInt64s("p/o", indices) with
                        | Ok ows -> SeqEq ows values
                        | _ -> failwithlog "ERROR"

                    check_po_bits()
                    check_po_words()
                    check_po_dwords()
                    check_po_lwords()
                    check_po_bytes()

                let checkTopLevel() =
                    // LsXgi(=>"") 의 q 파일
                    let ls = venders |> Array.find (fun (v:VendorSpec) -> v.Location = "xgi")
                    let lsq = ls.Files |> Array.find (fun (v:IOFileSpec) -> v.Name = "q")
                    let checkBytes() =
                        let indices = [|0..lsq.Length-1|]
                        let values = indices |> map (uint8 >> byte)

                        client.ClearAll("xgi/q") |> checkOk
                        client.WriteBytes("xgi/q", indices, values) |> checkOk
                        match client.ReadBytes("xgi/q", indices) with
                        | Ok obs -> SeqEq obs values
                        | _ -> failwithlog "ERROR"

                    let checkWords() =
                        let indices = [|0..(lsq.Length / 2)-1|]
                        let values = indices |> map uint16

                        client.ClearAll("xgi/q") |> checkOk
                        client.WriteUInt16s("xgi/q", indices, values) |> checkOk
                        match client.ReadUInt16s("xgi/q", indices) with
                        | Ok ows -> SeqEq ows values
                        | _ -> failwithlog "ERROR"

                    let checkDwords() =
                        let indices = [|0..(lsq.Length / 4)-1|]
                        let values = indices |> map uint32

                        client.ClearAll("xgi/q") |> checkOk
                        client.WriteUInt32s("xgi/q", indices, values) |> checkOk
                        match client.ReadUInt32s("xgi/q", indices) with
                        | Ok ows -> SeqEq ows values
                        | _ -> failwithlog "ERROR"

                    let checkLwords() =
                        let indices = [|0..(lsq.Length / 8)-1|]
                        let values = indices |> map uint64

                        client.ClearAll("xgi/q") |> checkOk
                        client.WriteUInt64s("xgi/q", indices, values) |> checkOk
                        match client.ReadUInt64s("xgi/q", indices) with
                        | Ok ows -> SeqEq ows values
                        | _ -> failwithlog "ERROR"



                    let checkEndian() =
                        let n8 = 0x07_06_05_04_03_02_01_00UL
                        let bytes = BitConverter.GetBytes(n8)
                        client.WriteUInt64s("xgi/q", [|0|], [|n8|]) |> checkOk
                        match client.ReadBytes("xgi/q", [|0..7|]) with
                        | Ok bytes ->
                            bytes[0] === 0x00uy
                            bytes[1] === 0x01uy
                            bytes[2] === 0x02uy
                            bytes[3] === 0x03uy
                            bytes[4] === 0x04uy
                            bytes[5] === 0x05uy
                            bytes[6] === 0x06uy
                            bytes[7] === 0x07uy
                        | _ -> failwithlog "ERROR"

                        client.ClearAll("xgi/q") |> checkOk
                        client.WriteBytes("xgi/q", [|0..7|], bytes) |> checkOk
                        match client.ReadUInt64s("xgi/q", [|0|]) with
                        | Ok lws -> lws.[0] === n8
                        | _ -> failwithlog "ERROR"



                    checkBytes()
                    checkWords()
                    checkDwords()
                    checkLwords()

                    checkEndian()


                checkTopLevel()
                checkSubFile()

                zmqInfo.CancellationTokenSource.Cancel()
            )


        [<Test>]
        member x.ReadWritePoints() =
            lock x.Locker (fun () ->
                let zmqInfo = Zmq.Initialize (__SOURCE_DIRECTORY__ + @"/../../IOHub/IO.Core/zmqsettings.json")
                let server, client, cts = zmqInfo.Server, zmqInfo.Client, zmqInfo.CancellationTokenSource
                let venders = zmqInfo.IOSpec.Vendors
                let p = venders |> Array.find (fun (v:VendorSpec) -> v.Location = "p")
                let po = p.Files |> Array.find (fun (v:IOFileSpec) -> v.Name = "o")
                po.GetPath() === "p/o"
                let ls = venders |> Array.find (fun (v:VendorSpec) -> v.Location = "xgi")
                let lq = ls.Files |> Array.find (fun (v:IOFileSpec) -> v.Name = "q")
                lq.GetPath() === "xgi/q"

                let length = po.Length
                match client.ReadBytes("p/o", [|255|]) with
                | _ -> ()

                zmqInfo.CancellationTokenSource.Cancel()
                dispose client
                dispose server
                System.Threading.Thread.Sleep(500)

                ()
            )


        [<Test>]
        member x.TestLimits() =
            lock x.Locker (fun () ->
                let zmqInfo = Zmq.Initialize (__SOURCE_DIRECTORY__ + @"/../../IOHub/IO.Core/zmqsettings.json")
                let server, client, cts = zmqInfo.Server, zmqInfo.Client, zmqInfo.CancellationTokenSource
                let venders = zmqInfo.IOSpec.Vendors

                let p = venders |> Array.find (fun (v:VendorSpec) -> v.Location = "p")
                let po = p.Files |> Array.find (fun (v:IOFileSpec) -> v.Name = "o")
                let length = po.Length

                // 일단, LWord 단위로 파일 layout 가정
                length % 8 === 0

                match client.WriteBytes("p/o", [|length - 1|], [|255uy|]) with
                | Ok _ ->
                    match client.ReadBytes("p/o", [|length - 1|]) with
                    | Ok bs -> SeqEq bs [|255uy|]
                    | _ -> failwithlog "ERROR"
                | Error _ ->
                    failwithlog "ERROR"

                let writeErrorneous = client.WriteBytes("p/o", [|length|], [|255uy|])
                match writeErrorneous with
                | Error err -> err.Contains("Invalid offset") === true
                | Ok _ -> failwithlog "Should have been failed."
            

                match client.WriteUInt16s("p/o", [|length/2 - 1|], [|0xFFFFus|]) with
                | Ok _ ->
                    match client.ReadUInt16s("p/o", [|length/2 - 1|]) with
                    | Ok ws -> SeqEq ws [|0xFFFFus|]
                    | _ -> failwithlog "ERROR"
                | Error _ ->
                    failwithlog "ERROR"



                match client.WriteUInt32s("p/o", [|length/4 - 1|], [|0xFFFF_FFFFu|]) with
                | Ok _ ->
                    match client.ReadUInt32s("p/o", [|length/4 - 1|]) with
                    | Ok dws -> SeqEq dws [|0xFFFFFFFFu|]
                    | _ -> failwithlog "ERROR"
                | Error _ ->
                    failwithlog "ERROR"


                match client.WriteUInt64s("p/o", [|length/8 - 1|], [|0xFFFF_FFFF_FFFF_FFFFUL|]) with
                | Ok _ ->
                    match client.ReadUInt64s("p/o", [|length/8 - 1|]) with
                    | Ok lws -> SeqEq lws [|0xFFFF_FFFF_FFFF_FFFFUL|]
                    | _ -> failwithlog "ERROR"
                | Error _ ->
                    failwithlog "ERROR"

                zmqInfo.CancellationTokenSource.Cancel()
            )

