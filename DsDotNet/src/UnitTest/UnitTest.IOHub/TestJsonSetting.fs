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

                    client.ClearAll("p/o")
                    client.WriteBytes("p/o", indices, values)
                    let obs:byte[] = client.ReadBytes("p/o", indices)
                    SeqEq obs values

                let check_po_words() =
                    let indices = [|0..(po.Length / 2)-1|]
                    let values = indices |> Array.map (fun i -> uint16 i)

                    client.ClearAll("p/o")
                    client.WriteUInt16s("p/o", indices, values)
                    let ows:uint16[] = client.ReadUInt16s("p/o", indices)
                    SeqEq ows values

                let check_po_dwords() =
                    let indices = [|0..(po.Length / 4)-1|]
                    let values = indices |> Array.map (fun i -> uint32 i)

                    client.ClearAll("p/o")
                    client.WriteUInt32s("p/o", indices, values)
                    let ows:uint32[] = client.ReadUInt32s("p/o", indices)
                    SeqEq ows values

                let check_po_lwords() =
                    let indices = [|0..(po.Length / 8)-1|]
                    let values = indices |> Array.map (fun i -> uint64 i)

                    client.ClearAll("p/o")
                    client.WriteUInt64s("p/o", indices, values)
                    let ows:uint64[] = client.ReadUInt64s("p/o", indices)
                    SeqEq ows values

                check_po_bytes()
                check_po_words()
                check_po_dwords()
                check_po_lwords()

            let checkTopLevel() =
                // LsXgi(=>"") 의 q 파일
                let ls = venders |> Array.find (fun (v:VendorSpec) -> v.Location = "")
                let lsq = ls.Files |> Array.find (fun (v:IOFileSpec) -> v.Name = "q")
                let checkBytes() =
                    let indices = [|0..lsq.Length-1|]
                    let values = indices |> Array.map (fun i -> i |> uint8 |> byte)

                    client.ClearAll("q")
                    client.WriteBytes("q", indices, values)
                    let obs:byte[] = client.ReadBytes("q", indices)
                    SeqEq obs values

                let checkWords() =
                    let indices = [|0..(lsq.Length / 2)-1|]
                    let values = indices |> Array.map (fun i -> uint16 i)

                    client.ClearAll("q")
                    client.WriteUInt16s("q", indices, values)
                    let ows:uint16[] = client.ReadUInt16s("q", indices)
                    SeqEq ows values

                let checkDwords() =
                    let indices = [|0..(lsq.Length / 4)-1|]
                    let values = indices |> Array.map (fun i -> uint32 i)

                    client.ClearAll("q")
                    client.WriteUInt32s("q", indices, values)
                    let ows:uint32[] = client.ReadUInt32s("q", indices)
                    SeqEq ows values

                let checkLwords() =
                    let indices = [|0..(lsq.Length / 8)-1|]
                    let values = indices |> Array.map (fun i -> uint64 i)

                    client.ClearAll("q")
                    client.WriteUInt64s("q", indices, values)
                    let ows:uint64[] = client.ReadUInt64s("q", indices)
                    SeqEq ows values


                checkBytes()
                checkWords()
                checkDwords()
                checkLwords()


            checkTopLevel()
            checkSubFile()