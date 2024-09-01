namespace T.IOHub

open Dual.Common.UnitTest.FS
open NUnit.Framework
open System
open System.IO
open System.Diagnostics
open IO.Core
open Xunit
open Dual.Common.Core.FS

[<AutoOpen>]
module TestLockModule =
    
    let lockedExe (locker:obj) f =
        if isNull(locker) then
            Debug.WriteLine("Locked")
            f()
        else
            Debug.WriteLine("Unocked")
            lock locker f

    let testSizeof<'T> () = 
        sizeof<'T>
        

    let readTBytes<'T> (tOffset:int): byte[] =
        let size = sizeof<'T>
        let stream:FileStream = null
        let byteOffset = tOffset * size
        let buffer = Array.zeroCreate<byte> (size)
        stream.Seek(int64 byteOffset, SeekOrigin.Begin) |> ignore
        stream.Read(buffer, 0, size) |> ignore
        buffer


    [<Collection("ZmqTesting")>]
    [<TestFixture>]
    type TestLock() =
        inherit TestClassWithLogger(Path.Combine($"{__SOURCE_DIRECTORY__}/App.config"), "IOHubLogger")


        [<Test>]
        member x.LockedFunctionTest() =
            let a = lockedExe x.Locker (fun () -> Debug.WriteLine("A"); 1)
            let b = lockedExe null (fun () -> Debug.WriteLine("B"); true)
            noop()
        [<Test>]
        member x.LockedActionTest() =
            let a1 = lockedExe x.Locker (fun () -> Debug.WriteLine("A"))
            Debug.WriteLine($"Unit Result={a1}")

        [<Test>]
        member x.SizeOf() =
            testSizeof<byte>() === 1
            testSizeof<uint16>() === 2
