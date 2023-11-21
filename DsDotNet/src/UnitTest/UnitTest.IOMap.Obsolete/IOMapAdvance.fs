namespace T.IOMap

open NUnit.Framework
open System.Collections.Generic
open IOMapApi.MemoryIOApi
open FsUnit.Xunit
open T
open IOMapApi.MemoryIOManagerImpl
open Dual.Common.Core.FS
open IOMapApi
open System.IO.MemoryMappedFiles
open System

[<AutoOpen>]
module Group2 =
 
    [<TestFixture>]
    type IOMapAdvance() as this =
        inherit MapIOTestBaseClass(true)

            
        // Common teardown for each test can be done here.
        [<TearDown>]
        member _.TearDown() = ()
            // Teardown code...
            // Individual tests follow:

        [<Test>]
        member _.ConcurrentWritesAtMultiplePositions() =
            let data1 = [| 0x13uy; 0x24uy |]
            let data2 = [| 0x35uy; 0x46uy |]
            this.MAPS
            |> Seq.iter(fun map ->
                let mIO = map.Value
                let tasks = [
                    async { mIO.Write(data1, 7) }
                    async { mIO.Write(data2, 10) }
                ]
                tasks |> Async.Parallel|> Async.Ignore |> Async.RunSynchronously
                mIO.Read(7, 2) |> should equal data1
                mIO.Read(10, 2) |> should equal data2
            )

        [<Test>]
        member _.ConcurrentReadsAtMultiplePositions() =
            let data = [| 0x47uy; 0x58uy |]
            this.MAPS
            |> Seq.iter(fun map ->
                let mIO = map.Value
                mIO.Write(data, 12)
                let tasks = [
                    async { return mIO.Read(12, 2) }
                    async { return mIO.Read(12, 2) }
                ]
                let results = tasks |> Async.Parallel |> Async.RunSynchronously
                results |> Array.exists (fun res -> res <> data) |> should equal false
            )

        [<Test>]
        member _.ConsistencyCheckAfterPartialModification() =
            let modifiedData = [| 0x88uy; 0x99uy |]
            this.MAPS
            |> Seq.iter(fun map ->
                let mIO = map.Value
                let size = mIO.MemorySize
                let initialData = Array.init size (fun _ -> 0x77uy)
                mIO.Write(initialData, 0)
                mIO.Write(modifiedData, 50)
                let modifiedSection = mIO.Read(50, 2)
                let otherSection = mIO.Read(0, 50)
                modifiedSection |> should equal modifiedData
                otherSection |> should not' (equal modifiedData) 
            )


        [<Test>]
        member _.ConcurrentReadsReturnCorrectData() = 
            this.MAPS  
            |> Seq.iter(fun map ->
                let mIO = map.Value
                let tasks = 
                    [ for _ in [1..20] -> async { return mIO.Read(0, 10) } ]
                let results = Async.Parallel tasks |> Async.RunSynchronously
                for res in results do
                    res |> should equal results.[0]
            )


        [<Test>]
        member _.ConcurrentWritesAreConsistent() = 
            let testData1 = [| 0x01uy; 0x02uy; 0x03uy; 0x04uy |]
            let testData2 = [| 0x05uy; 0x06uy; 0x07uy; 0x08uy |]
            this.MAPS  
            |> Seq.iter(fun map ->
                let mIO = map.Value
                let tasks = 
                    [ 
                        async { mIO.Write(testData1, 0) }
                        async { mIO.Write(testData2, 0) }
                        async { mIO.Write(testData1, 0) }
                        async { mIO.Write(testData2, 0) }
                    ]
                tasks
                |> Async.Parallel 
                |> Async.Ignore
                |> Async.RunSynchronously
                let res = mIO.Read(0, 4)
                (res = testData1 || res = testData2) |> should equal true
            )
