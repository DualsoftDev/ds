namespace T.IOHub

open Dual.UnitTest.Common.FS
open NUnit.Framework
open System
open IO.Core

[<AutoOpen>]
module JSONSettingTestModule =
    
    [<TestFixture>]
    type JSONSettingTest() =
        inherit TestBaseClass("IOHubLogger")

        [<Test>]
        member _.Endian() =
            let n = 1234567890UL
            let bytes = BitConverter.GetBytes(n)
            let n2 = BitConverter.ToUInt64(bytes, 0)
            n === n2

        [<Test>]
        member _.ReadWriteWholeFiles() =
            let zmqInfo = Zmq.Initialize (__SOURCE_DIRECTORY__ + @"/../../IOHub/IO.Core/zmqsettings.json")
            let server, client, cts = zmqInfo.Server, zmqInfo.Client, zmqInfo.CancellationTokenSource
            let venders = zmqInfo.IOSpec.Vendors

            let checkSubFile() =
                // Paix(=>"p") 의 o 파일
                let p = venders |> Array.find (fun (v:VendorSpec) -> v.Location = "p")
                let po = p.Files |> Array.find (fun (v:IOFileSpec) -> v.Name = "o")
                let check_po_bytes() =
                    let indices = [|0..po.Length-1|]
                    let values = indices |> Array.map (fun i -> i |> uint8 |> byte)

                    client.ClearAll("p/o") === null
                    client.WriteBytes("p/o", indices, values) === null
                    let (obs:byte[], err_) = client.ReadBytes("p/o", indices)
                    SeqEq obs values

                let check_po_words() =
                    let indices = [|0..(po.Length / 2)-1|]
                    let values = indices |> Array.map (fun i -> uint16 i)

                    client.ClearAll("p/o") === null
                    client.WriteUInt16s("p/o", indices, values) === null
                    let (ows:uint16[], err_) = client.ReadUInt16s("p/o", indices)
                    SeqEq ows values

                let check_po_dwords() =
                    let indices = [|0..(po.Length / 4)-1|]
                    let values = indices |> Array.map (fun i -> uint32 i)

                    client.ClearAll("p/o") === null
                    client.WriteUInt32s("p/o", indices, values) === null
                    let (ows:uint32[], err_) = client.ReadUInt32s("p/o", indices)
                    SeqEq ows values

                let check_po_lwords() =
                    let indices = [|0..(po.Length / 8)-1|]
                    let values = indices |> Array.map (fun i -> uint64 i)

                    client.ClearAll("p/o") === null
                    client.WriteUInt64s("p/o", indices, values) === null
                    let (ows:uint64[], err_) = client.ReadUInt64s("p/o", indices)
                    SeqEq ows values

                check_po_words()
                check_po_dwords()
                check_po_lwords()
                check_po_bytes()

            let checkTopLevel() =
                // LsXgi(=>"") 의 q 파일
                let ls = venders |> Array.find (fun (v:VendorSpec) -> v.Location = "")
                let lsq = ls.Files |> Array.find (fun (v:IOFileSpec) -> v.Name = "q")
                let checkBytes() =
                    let indices = [|0..lsq.Length-1|]
                    let values = indices |> Array.map (fun i -> i |> uint8 |> byte)

                    client.ClearAll("q") === null
                    client.WriteBytes("q", indices, values) === null
                    let (obs:byte[], err_) = client.ReadBytes("q", indices)
                    SeqEq obs values

                let checkWords() =
                    let indices = [|0..(lsq.Length / 2)-1|]
                    let values = indices |> Array.map (fun i -> uint16 i)

                    client.ClearAll("q") === null
                    client.WriteUInt16s("q", indices, values) === null
                    let (ows:uint16[], err_) = client.ReadUInt16s("q", indices)
                    SeqEq ows values

                let checkDwords() =
                    let indices = [|0..(lsq.Length / 4)-1|]
                    let values = indices |> Array.map (fun i -> uint32 i)

                    client.ClearAll("q") === null
                    client.WriteUInt32s("q", indices, values) === null
                    let (ows:uint32[], err_) = client.ReadUInt32s("q", indices)
                    SeqEq ows values

                let checkLwords() =
                    let indices = [|0..(lsq.Length / 8)-1|]
                    let values = indices |> Array.map (fun i -> uint64 i)

                    client.ClearAll("q") === null
                    client.WriteUInt64s("q", indices, values) === null
                    let (ows:uint64[], err_) = client.ReadUInt64s("q", indices)
                    SeqEq ows values


                checkBytes()
                checkWords()
                checkDwords()
                checkLwords()


            checkTopLevel()
            checkSubFile()

        [<Test>]
        member _.ReadWritePoints() =
            let zmqInfo = Zmq.Initialize (__SOURCE_DIRECTORY__ + @"/../../IOHub/IO.Core/zmqsettings.json")
            let server, client, cts = zmqInfo.Server, zmqInfo.Client, zmqInfo.CancellationTokenSource
            let venders = zmqInfo.IOSpec.Vendors
            let p = venders |> Array.find (fun (v:VendorSpec) -> v.Location = "p")
            let po = p.Files |> Array.find (fun (v:IOFileSpec) -> v.Name = "o")
            po.GetPath() === "p/o"
            let l = venders |> Array.find (fun (v:VendorSpec) -> v.Location = "")
            let lq = l.Files |> Array.find (fun (v:IOFileSpec) -> v.Name = "q")
            lq.GetPath() === "q"


            let length = po.Length
            let v, err = client.Read("p/%ob255")
            ()


        [<Test>]
        member _.TestLimits() =
            let zmqInfo = Zmq.Initialize (__SOURCE_DIRECTORY__ + @"/../../IOHub/IO.Core/zmqsettings.json")
            let server, client, cts = zmqInfo.Server, zmqInfo.Client, zmqInfo.CancellationTokenSource
            let venders = zmqInfo.IOSpec.Vendors

            let p = venders |> Array.find (fun (v:VendorSpec) -> v.Location = "p")
            let po = p.Files |> Array.find (fun (v:IOFileSpec) -> v.Name = "o")
            let length = po.Length

            // 일단, LWord 단위로 파일 layout 가정
            length % 8 === 0

            client.WriteBytes("p/o", [|length - 1|], [|255uy|]) === null
            let b0, err = client.ReadBytes("p/o", [|length - 1|])
            SeqEq b0 [|255uy|]

            let writeErrorneous = client.WriteBytes("p/o", [|length|], [|255uy|])
            writeErrorneous =!= null
            writeErrorneous.Contains("Invalid offset") === true

            client.WriteUInt16s("p/o", [|length/2 - 1|], [|0xFFFFus|]) === null
            let ws, err = client.ReadUInt16s("p/o", [|length/2 - 1|])
            SeqEq ws [|0xFFFFus|]

            client.WriteUInt32s("p/o", [|length/4 - 1|], [|0xFFFF_FFFFu|]) === null
            let dws, err = client.ReadUInt32s("p/o", [|length/4 - 1|])
            SeqEq dws [|0xFFFFFFFFu|]

            client.WriteUInt64s("p/o", [|length/8 - 1|], [|0xFFFF_FFFF_FFFF_FFFFUL|]) === null
            let lws, err = client.ReadUInt64s("p/o", [|length/8 - 1|])
            SeqEq lws [|0xFFFF_FFFF_FFFF_FFFFUL|]


