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
module Group1 =
    
    [<TestFixture>]
    type IOMapBasic() as this =
        inherit MapIOTestBaseClass(false)

            
        // Common teardown for each test can be done here.
        [<TearDown>]
        member _.TearDown() = ()
            // Teardown code...
            // Individual tests follow:

        [<Test>]
        member _.ReadEmptyDataReturnsZeros() = 
            this.MAPS
            |> Seq.iter(fun map ->
                let mIO = map.Value
                mIO.ClearMemoryData()
                let readData = mIO.Read(0, 4)
                readData |> should equal [| 0x00uy; 0x00uy; 0x00uy; 0x00uy |]
            )

        [<Test>]
        member _.WriteFullDataConsistency() =
            this.MAPS
            |> Seq.iter(fun map ->
                
                let mIO = map.Value
                let size = mIO.MemorySize
                let fullData = Array.init size (fun _ -> 0xFFuy)
                mIO.Write(fullData, 0)
                let readData = mIO.Read(0, size)
                readData |> should equal fullData
            )

        [<Test>]
        member _.ModifySpecificRange() =
            let initialData = [| 0xAAuy; 0xBBuy |]
            let modifiedData = [| 0xCCuy; 0xDDuy |]
            this.MAPS
            |> Seq.iter(fun map ->
                let mIO = map.Value
                mIO.Write(initialData, 5)
                mIO.Write(modifiedData, 5)
                mIO.Read(5, 2) |> should equal modifiedData
            )

        [<Test>]
        member _.WriteInvalidDataSizeThrowsException() =
            let largeData = Array.init 1024 (fun _ -> 0xEEuy)
            this.MAPS
            |> Seq.iter(fun map ->
                let _, size = map.Key
                let mIO = map.Value
                if size < 1024 then
                    (fun () -> mIO.Write(largeData, 0)) |> shouldFail
            )

        [<Test>]
        member _.MultipleWritesWithSameData() =
            let repeatData = [| 0x11uy; 0x22uy |]
            this.MAPS
            |> Seq.iter(fun map ->
                let mIO = map.Value
                for _ in [1..5] do
                    mIO.Write(repeatData, 3)
                mIO.Read(3, 2) |> should equal repeatData
            )


        [<Test>]
        member _.ModifySpecificBitAndVerify() =
            let testData = [| 0x0Fuy; 0x00uy |]
            this.MAPS
            |> Seq.iter(fun map ->
                let mIO = map.Value
                mIO.Write(testData, 0)
                mIO.WriteBit(true, 0, 7)
                mIO.WriteBit(true, 1, 7)
                let modifiedData = mIO.Read(0, 2)
                modifiedData |> should equal [| 0x8Fuy; 0x80uy |]
                mIO.ClearMemoryData()
            )

        [<Test>]
        member _.PerformanceTestWithLargeDataSet() =
            this.MAPS
            |> Seq.iter(fun map ->
                let mIO = map.Value
                let largeData = Array.init mIO.MemorySize (fun i -> byte(i % 256))
                mIO.Write(largeData, 0)
                let readData = mIO.Read(0, mIO.MemorySize)
                readData |> should equal largeData
            )

        [<Test>]
        member _.CallingMethodsAfterDispose() =
            this.MAPS
            |> Seq.iter(fun map ->
                let mIO = map.Value
                mIO.Dispose()
                (fun () -> mIO.WriteBit(true, 0, 1)) |> shouldFail
                (fun () -> mIO.Read(0, 2)|>ignore) |> shouldFail
            )

        [<Test>]
        member _.VerifyInitialValues() =
            this.MAPS
            |> Seq.iter(fun map ->
                let mIO = map.Value
                let initialData = mIO.Read(0, 2)
                initialData |> should equal [| 0x00uy; 0x00uy |]
            )

        [<Test>]
        member _.VerifyReadBitReturnValue() =
            let testData = [| 0x80uy; 0x01uy |]
            this.MAPS
            |> Seq.iter(fun map ->
                let mIO = map.Value
                mIO.Write(testData, 0)
                mIO.ReadBit(0, 0) |> should equal false
                mIO.ReadBit(0, 7) |> should equal true
                mIO.ReadBit(1, 0) |> should equal true
            )
        
        [<Test>]
        member _.WriteDataSequentially() =
            let data = [| 0x69uy; 0x7Auy |]
            this.MAPS
            |> Seq.iter(fun map ->
                let mIO = map.Value
                for i in [1..4] do
                    mIO.Write(data, i * 2)
                for i in [1..4] do
                    mIO.Read(i * 2, 2) |> should equal data
            )

        [<Test>]
        member _.ReuseMemoryIOObjects() =
            this.MAPS
            |> Seq.iter(fun map ->
                let mIO = map.Value
                mIO.ClearMemoryData()
                let data = [| 0x5Buy; 0x6Cuy |]
                mIO.Write(data, 0)
                mIO.Read(0, 2) |> should equal data
            )

        
        
        [<Test>]
        member _.BoundaryValuesForReadAndWrite() =
            let data = [| 0x10uy; 0x20uy |]
            this.MAPS
            |> Seq.iter(fun map ->
                let _, size = map.Key
                let mIO = map.Value
                mIO.Write(data, size - 2)
                let readData = mIO.Read(size - 2, 2)
                readData |> should equal data
            )

        [<Test>]
        member _.VerifyDataAfterClearMemoryData() =
            let data = Array.init 10 (fun _ -> 0xAAuy)
            this.MAPS
            |> Seq.iter(fun map ->
                let mIO = map.Value
                mIO.Write(data, 0)
                mIO.ClearMemoryData()
                let clearedData = mIO.Read(0, 10)
                clearedData |> should equal [| for _ in [1..10] -> 0x00uy |]
            )

        [<Test>]
        member _.InvalidStartPositionForReadAndWrite() =
            let data = [| 0x01uy; 0x02uy |]
            this.MAPS
            |> Seq.iter(fun map ->
                let _, size = map.Key
                let mIO = map.Value
                (fun () -> mIO.Write(data, -1)) |> shouldFail
                (fun () -> mIO.Read(-1, 2) |>ignore) |> shouldFail
            )

        [<Test>]
        member _.WriteDataBeyondCapacity() =
            let data = [| 0x11uy; 0x22uy; 0x33uy; 0x44uy |]
            this.MAPS
            |> Seq.iter(fun map ->
                let _, size = map.Key
                let mIO = map.Value
                if size > 2 then
                    (fun () -> mIO.Write(data, size - 2)) |> shouldFail
            )

        [<Test>]
        member _.VerifyGapsBetweenWrittenData() =
            let data1 = [| 0x55uy; 0x66uy |]
            let data2 = [| 0x77uy; 0x88uy |]
            this.MAPS
            |> Seq.iter(fun map ->
                let mIO = map.Value
                mIO.Write(data1, 5)
                mIO.Write(data2, 10)
                let gapData = mIO.Read(7, 3)
                gapData |> should equal [| 0x00uy; 0x00uy; 0x00uy |]
            )

        [<Test>]
        member _.MultipleReadsReturnConsistentData() =
            this.MAPS 
            |> Seq.iter(fun map ->
                let name, size = map.Key
                let mIO = map.Value
                let testData = mIO.Read(0, size)
                for _ in [1..20] do
                    mIO.Read(0, size) |> should equal testData
            )

        [<Test>]
        member _.WriteThenReadReturnsWrittenData() = 
            let testData = [| 0x12uy; 0x34uy; 0x56uy; 0x78uy |]
            this.MAPS 
            |> Seq.iter(fun map ->
                let mIO = map.Value
                mIO.Write(testData, 0)
                mIO.Read(0, testData.Length) |> should equal testData
                mIO.ClearMemoryData()
            )

        [<Test>]
        member _.WriteBeyondMemorySizeThrowsException() = 
            let testData = [| 0xFFuy; 0xFFuy |]
            this.MAPS 
            |> Seq.iter(fun map ->
                let name, size = map.Key
                let mIO = map.Value
                (fun () -> mIO.Write(testData, size)|>ignore) |> shouldFail
                mIO.ClearMemoryData()
            )

        [<Test>]
        member _.ReadBeyondMemorySizeThrowsException() = 
            this.MAPS  
            |> Seq.iter(fun map ->
                let name, size = map.Key
                let mIO = map.Value
                (fun () -> mIO.Read(size, 1) |> ignore) |> shouldFail
            )

        
        [<Test>]
        member _.ReadWriteConcurrentlyReturnsCorrectData() = 
            let testData = [| 0x09uy; 0x0Auy; 0x0Buy; 0x0Cuy |]
            let mutable readBuffer: byte array = [||]

            this.MAPS  
            |> Seq.iter(fun map ->
                let mIO = map.Value
                mIO.ClearMemoryData()
                let tasks = 
                    [ 
                        async { mIO.Write(testData, 0) }
                        async { readBuffer <- mIO.Read(0, 4) }
                        async { mIO.Write(testData, 0) }
                        async { readBuffer <- mIO.Read(0, 4) }
                        async { mIO.Write(testData, 0) }
                        async { readBuffer <- mIO.Read(0, 4) }
                    ]
                tasks
                |> Async.Sequential 
                |> Async.Ignore
                |> Async.RunSynchronously

                readBuffer |> should equal testData
            )

        [<Test>]
        member _.WriteBitBeyondByteSizeThrowsException() = 
            this.MAPS  
            |> Seq.iter(fun map ->
                let mIO = map.Value
                (fun () -> mIO.WriteBit(true, 0, 8) |>ignore) |> shouldFail
            )

        [<Test>]
        member _.ReadBitBeyondByteSizeThrowsException() = 
            this.MAPS  
            |> Seq.iter(fun map ->
                let mIO = map.Value
                (fun () -> mIO.ReadBit(0, 8) |> ignore) |> shouldFail
            )

       
        [<Test>]
        member _.MemoryLoadAfterUnloadReturnsCorrectData() =  
            let testData = [| 0xDEuy; 0xADuy |]
            this.MAPS  
            |> Seq.iter(fun map ->
                let mIO = map.Value
                mIO.Write(testData, 0)
                mIO.Write(testData, 2)
            )
            this.CloseMap()
            this.OpenMap()
            this.MAPS 
            |> Seq.iter(fun map ->
                let mIO = map.Value
                mIO.Read(0, 2) |> should equal testData
                mIO.Read(2, 2) |> should equal testData
                mIO.ClearMemoryData()
                mIO.Read(0, 2) |> should equal [| 0x00uy; 0x00uy |]
            )

         

        [<Test>]
        member _.MemoryDisposeReleasesAllResources() = 
            this.CloseMap()
            this.OpenMap()
            
        [<Test>]
        member _.WriteAfterDisposeThrowsException() =  
            let testData = [| 0xBEuy; 0xEFuy |]
            this.MAPS 
            |> Seq.iter(fun map ->
                let mIO = map.Value
                mIO.Dispose()
                (fun () -> mIO.Write(testData, 0)) |> shouldFail
                (fun () -> mIO.Write(testData, 2)) |> shouldFail
            )
            this.CloseMap()
            this.OpenMap()
