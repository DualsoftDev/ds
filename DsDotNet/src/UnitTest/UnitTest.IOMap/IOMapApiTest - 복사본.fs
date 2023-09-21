namespace T.IOMap

open NUnit.Framework
open System.Collections.Generic
open IOMapApi.MemoryIOApi
open FsUnit.Xunit
open T
open IOMapApi.MemoryIOManagerImpl
open Dual.Common.Core.FS
open IOMapApi

[<AutoOpen>]
module IOMapApiTestModule =
    
 
    [<TestFixture>]
    type IOMapApiTest() =
        //inherit ExpressionTestBaseClass()
        let MAPS = Dictionary<string*int, MemoryIO>()
        let files = 
                [
                    "PAIX\NMC2\I", 64
                    "PAIX\NMC2\O", 64     
                    "PAIX\NMF\I", 128
                    "PAIX\NMF\O", 128
                    
                    "UnitTest\B",128
                    "UnitTest\C",256
                    "UnitTest\D",512
                    //"UnitTest\E",int max
                ]

   
        // Common setup for each test can be done here.
        [<OneTimeSetUp>]
        member _.SetUp() = 
        // Setup code...
               //IOMapApiTest 는 관리자 권한 경로거나 windows Service실행이 아니기 때문에 
            MemoryUtilImpl.TestMode <- true
    
            files 
            |> Seq.iter(fun (name, size)->
                    let name = @$"{name}"
                    if MemoryIOManager.Delete(name) then
                        tracefn $"MemoryIOManager Delete {name}"

                    if MemoryIOManager.Create(name, size) then
                        tracefn $"MemoryIOManager Create {name} size({size})"
                
                    MemoryIOManager.ClearData(name)
                    MemoryIOManager.Load(name) |> ignore
                
                    MAPS.Add((name, size), MemoryIO(name)) |>ignore
                )
            
        // Common teardown for each test can be done here.
        [<TearDown>]
        member _.TearDown() = ()
            // Teardown code...
            // Individual tests follow:

        [<Test>]
        member _.DeviceNameIsCorrect() =
            MAPS 
            |> Seq.iter(fun map ->
                let name, size = map.Key 
                name |> should equal map.Value.Device
                )

        [<Test>]
        member _.MemorySizeIsCorrect() =
            MAPS 
            |> Seq.iter(fun map ->
                let name, size = map.Key 
                size
                |> should equal map.Value.MemorySize
                )

        [<Test>]
        member _.ReadReturnsCorrectData() = 
            MAPS 
            |> Seq.iter(fun map ->
                let name, size = map.Key
                for i in [0..size-1] do 
                map.Value.Read(i, 1)[0] |> should equal  0uy
                )

        [<Test>]
        member _.ReadBitIsCorrect() = 
            MAPS 
            |> Seq.iter(fun map ->
                let name, size = map.Key
                let mIO = map.Value
                for i in [0..size-1] do 
                    for j in [0..7] do
                        mIO.ReadBit(i, j) |> should equal false
                )

        [<Test>]
        member _.WriteCorrectlyUpdatesMemory() = 
            let testData1 = [| 0xFFuy |]
            let testData2 = [| 0x00uy |]
            MAPS 
            |> Seq.iter(fun map ->
                let name, size = map.Key
                let mIO = map.Value
                mIO.Write(testData1, 0)
                mIO.Read(0, 1)[0] |> should equal testData1[0]
                mIO.Write(testData2, 0)
                mIO.Read(0, 1)[0] |> should equal testData2[0]
                )

        [<Test>]
        member _.WriteBitCorrectlyUpdatesMemory() = 
            MAPS 
            |> Seq.iter(fun map ->
                let name, size = map.Key
                let mIO = map.Value
                mIO.WriteBit(true, 0, 7) // Write the highest bit
                mIO.Read(0, 1)[0] |> should equal 0x80uy
                mIO.WriteBit(false, 0, 7) // Write the highest bit
                mIO.Read(0, 1)[0] |> should equal 0x00uy
                )

        [<Test>]
        member _.GetMemoryDataReturnsExpectedData() = 
            let testData1 = [| 0xFFuy; 0x00uy; 0xAAuy; 0x55uy |]
            let testData2 = [| 0x00uy; 0x00uy; 0x00uy; 0x00uy |]
            MAPS 
            |> Seq.iter(fun map ->
                let name, size = map.Key
                let mIO = map.Value
                mIO.Write(testData1, 0)
                let returnedData = mIO.GetMemoryData()
                returnedData.[0..3] |> should equal testData1
                mIO.Write(testData2, 0)
                )

        [<Test>]
        member _.GetMemoryAsDataTableHasCorrectData() = 
            let testData = [| 0xFFuy; 0x00uy; 0xAAuy; 0x55uy |]
            MAPS 
            |> Seq.iter(fun map ->
                let name, size = map.Key
                let mIO = map.Value
                mIO.Write(testData, 0)
                let dt = mIO.GetMemoryAsDataTable()
                for i in 0..3 do
                    (dt.Rows.[0].[i] :?> byte) |> should equal testData.[i]
                )

        [<Test>]
        member _.OpenMMFThrowsExpectedException() = 
            // Assuming your MemoryIO constructor or methods throw when MMF doesn't exist
            let test = fun () -> MemoryIO("Nonexistent") |> ignore
            test |> should throw typeof<System.IO.FileNotFoundException>