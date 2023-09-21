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
        member _.MultipleReadsReturnConsistentData() =
            MAPS 
            |> Seq.iter(fun map ->
                let name, size = map.Key
                let mIO = map.Value
                let testData = mIO.Read(0, size)
                for _ in [1..10] do
                    mIO.Read(0, size) |> should equal testData
            )

        [<Test>]
        member _.WriteThenReadReturnsWrittenData() = 
            let testData = [| 0x12uy; 0x34uy; 0x56uy; 0x78uy |]
            MAPS 
            |> Seq.iter(fun map ->
                let mIO = map.Value
                mIO.Write(testData, 0)
                mIO.Read(0, testData.Length) |> should equal testData
            )

        [<Test>]
        member _.WriteBeyondMemorySizeThrowsException() = 
            let testData = [| 0xFFuy; 0xFFuy |]
            MAPS 
            |> Seq.iter(fun map ->
                let name, size = map.Key
                let mIO = map.Value
                (fun () -> mIO.Write(testData, size)) |> should throw typeof<System.ArgumentOutOfRangeException>
            )

        [<Test>]
        member _.ReadBeyondMemorySizeThrowsException() = 
            MAPS 
            |> Seq.iter(fun map ->
                let name, size = map.Key
                let mIO = map.Value
                (fun () -> mIO.Read(size, 1) |> ignore) |> should throw typeof<System.ArgumentOutOfRangeException>
            )

        [<Test>]
        member _.ConcurrentReadsReturnCorrectData() = 
            MAPS 
            |> Seq.iter(fun map ->
                let mIO = map.Value
                let tasks = 
                    [ for _ in [1..10] -> async { return mIO.Read(0, 10) } ]
                let results = Async.Parallel tasks |> Async.RunSynchronously
                for res in results do
                    res |> should equal results.[0]
            )

        [<Test>]
        member _.ConcurrentWritesAreConsistent() = ()
            //let testData1 = [| 0x01uy; 0x02uy; 0x03uy; 0x04uy |]
            //let testData2 = [| 0x05uy; 0x06uy; 0x07uy; 0x08uy |]
            //MAPS 
            //|> Seq.iter(fun map ->
            //    let mIO = map.Value
            //    let tasks = 
            //        [ 
            //            async { mIO.Write(testData1, 0) }
            //            async { mIO.Write(testData2, 0) }
            //        ]
            //    Async.Parallel tasks |> Async.RunSynchronously
            //    let res = mIO.Read(0, 4)
            //    // This will check that the result is either testData1 or testData2
            //    res |> should (equal testData1 or equal testData2)
            //)

        [<Test>]
        member _.ReadWriteConcurrentlyReturnsCorrectData() =  ()
            //let testData = [| 0x09uy; 0x0Auy; 0x0Buy; 0x0Cuy |]
            //MAPS 
            //|> Seq.iter(fun map ->
            //    let mIO = map.Value
            //    let tasks = 
            //        [ 
            //            async { mIO.Write(testData, 0) }
            //            async { return mIO.Read(0, 4) }
            //        ]
            //    let results = Async.Parallel tasks |> Async.RunSynchronously
            //    results.[1] |> should equal testData
            //)

        [<Test>]
        member _.WriteBitBeyondByteSizeThrowsException() = 
            MAPS 
            |> Seq.iter(fun map ->
                let mIO = map.Value
                (fun () -> mIO.WriteBit(true, 0, 8)) |> should throw typeof<System.ArgumentOutOfRangeException>
            )

        [<Test>]
        member _.ReadBitBeyondByteSizeThrowsException() = 
            MAPS 
            |> Seq.iter(fun map ->
                let mIO = map.Value
                (fun () -> mIO.ReadBit(0, 8) |> ignore) |> should throw typeof<System.ArgumentOutOfRangeException>
            )

        [<Test>]
        member _.MemoryClearResetsAllData() =
            let testData = [| 0xFFuy; 0xFFuy |]
            MAPS 
            |> Seq.iter(fun map ->
                let mIO = map.Value
                mIO.Write(testData, 0)
                mIO.ClearMemoryData()
                mIO.Read(0, 2) |> should equal [| 0x00uy; 0x00uy |]
            )

        [<Test>]
        member _.MemoryUnloadRelinquishesResources() = ()
            // Depending on your implementation, you might check whether resources 
            // like file handles, memory mappings, etc. have been correctly released.
            // This might involve OS-specific checks or other tools.

        [<Test>]
        member _.MemoryLoadAfterUnloadReturnsCorrectData() =  ()
            //let testData = [| 0xDEuy; 0xADuy |]
            //MAPS 
            //|> Seq.iter(fun map ->
            //    let mIO = map.Value
            //    mIO.Write(testData, 0)
            //    mIO.Unload()
            //    mIO.Load()
            //    mIO.Read(0, 2) |> should equal testData
            //)

        [<Test>]
        member _.MemoryDisposeReleasesAllResources() = ()
            // Similar to the Unload test, this would involve checking that all resources 
            // have been correctly disposed of.

        [<Test>]
        member _.WriteAfterDisposeThrowsException() =  ()
            //let testData = [| 0xBEuy; 0xEFuy |]
            //MAPS 
            //|> Seq.iter(fun map ->
            //    let mIO = map.Value
            //    mIO.Dispose()
            //    (fun () -> mIO.Write(testData, 0)) |> should throw typeof<ObjectDisposedException>
            //)
