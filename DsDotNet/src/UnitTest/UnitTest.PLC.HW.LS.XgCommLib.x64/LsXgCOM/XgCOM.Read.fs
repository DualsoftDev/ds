#if X64
namespace Tx64
#else
namespace Tx86
#endif
open T

open System
open Xunit
open NUnit.Framework

open Engine.Common.FS

[<AutoOpen>]
module XgCommLibSpec =

    type XgCOMCodes =
        | R_F = 0x24a
        | R_U = 0x24e
        | R_I = 0x24f
        | R_Q = 0x250
        | R_M = 0x251
        | R_A = 0x253

        | W_U = 0xc8
        | W_I = 0xc9
        | W_Q = 0xca
        | W_M = 0xcb
        | W_A = 0xcd

    let [<Literal>] MAX_RANDOM_READ_POINTS = 64
    let [<Literal>] MAX_ARRAY_BYTE_SIZE = 512   // 64*8

(*
    XGComLib summary
    ----------------
    모든 version 에서 ReadRandomDevice 는 동작함.
       - 단, 64점 이상인 경우, fail 함
    V20 버젼 사용시, WriteDevice_Bit 동작함.
    32/64 bit 모두 동일 한 듯.
*)
[<Collection("SerialXgiGenerationTest")>]
type XgCOM10ReadTest() =
    inherit XgCOMBaseClass()

    [<Test>]
    member x.``Basic read/write test`` () =
        x.CommObject.IsConnected() === 1
        let plcId = x.CommObject.GetPLCID;

        let di = x.Factory.CreateDevice()
        di.ucDeviceType <- Convert.ToByte('M')
        di.ucDataType <- Convert.ToByte('B')

        let wBuf = Array.zeroCreate<byte>(MAX_ARRAY_BYTE_SIZE)
        let rBuf = Array.zeroCreate<byte>(MAX_ARRAY_BYTE_SIZE)
        x.CommObject.RemoveAll()
        for i = 0 to MAX_RANDOM_READ_POINTS-1 do
            di.lSize <- 8
            di.lOffset <- i * 8
            wBuf[i] <- byte i
            x.CommObject.AddDeviceInfo(di)

        // does *NOT* working
        //x.CommObject.Write((int)XgCOMCodes.W_M, wBuf, 1, 0) =!= 0
        //x.CommObject.Read((int)XgCOMCodes.R_M, rBuf, 1, 0) =!= 0

        //x.CommObject.Command((int)XgCOMCodes.W_M, wBuf, 1024, 0) =!= 0
        //x.CommObject.Command((int)XgCOMCodes.R_M, rBuf, 1024, 0) =!= 0

        //let offset = 16*10
        //let a = x.CommObject.WriteDevice_Bit("M", offset, 1) //=== 1

        //let mutable nRead = 0
        ////x.CommObject.ReadDevice_Block("M", 0, &rBuf[0], 1024, &nRead)
        //let mutable buf:byte = 0uy
        //x.CommObject.ReadDevice_Block("M", 0, &buf, 1, &nRead)
        // does *NOT* working



        //// NOT working
        //x.CommObject.WriteRandomDevice(wBuf) // === 1

        // working : 단 random device 갯수가 64 이하 일 때...
        x.CommObject.ReadRandomDevice(rBuf) === 1

        noop()

    (* Pre20 version 은 WriteDevice_Bit 지원 안함. *)
    //[<Test>]
    //member x.``Write bit test`` () =
    //    for i = 0 to 1023 do
    //        x.CommObject.WriteDevice_Bit("M", i, 0) === 1




[<Collection("SerialXgiGenerationTest")>]
type XgCOM20ReadTest() =
    inherit XgCOMBaseClass20()

    [<Test>]
    member x.``Basic read/write test`` () =
        x.CommObject.IsConnected() === 1
        let plcId = x.CommObject.GetPLCID;

        let di = x.CreateDevice('M', 'B')

        let wBuf = Array.zeroCreate<byte>(MAX_ARRAY_BYTE_SIZE)
        let rBuf = Array.zeroCreate<byte>(MAX_ARRAY_BYTE_SIZE)
        x.CommObject.RemoveAll()
        for i = 0 to MAX_RANDOM_READ_POINTS-1 do
            di.lOffset <- i * 8
            wBuf[i] <- byte i
            x.CommObject.AddDeviceInfo(di)

        // does *NOT* working
        //x.CommObject.Write((int)XgCOMCodes.W_M, wBuf, 1, 0) =!= 0
        //x.CommObject.Read((int)XgCOMCodes.R_M, rBuf, 1, 0) =!= 0

        //x.CommObject.Command((int)XgCOMCodes.W_M, wBuf, 1024, 0) =!= 0
        //x.CommObject.Command((int)XgCOMCodes.R_M, rBuf, 1024, 0) =!= 0

        //let offset = 16*10
        //let a = x.CommObject.WriteDevice_Bit("M", offset, 1) //=== 1

        //let mutable nRead = 0
        ////x.CommObject.ReadDevice_Block("M", 0, &rBuf[0], 1024, &nRead)
        //let mutable buf:byte = 0uy
        //x.CommObject.ReadDevice_Block("M", 0, &buf, 1, &nRead)
        // does *NOT* working

#if USEV20
        // WORKING
        //for i = 0 to 1023 do
        //    x.CommObject20.WriteDevice_Bit("M", i, 0) === 1
        //for i = 0 to 4096*2-1 do
        //    x.CommObject.WriteDevice_Bit("M", i, 1) === 1
        //for i = 0 to 1023 do
        //    x.CommObject.WriteDevice_Bit("M", i, 0) === 1
        // WORKING


        // NOT working
        //for i = 0 to 1023 do
        //    let mutable nRead = 0
        //    x.CommObject.ReadDevice_Bit("M", i, &nRead)
        //    rBuf[i] <- byte nRead
        // NOT working
#endif


        //// NOT working
        //x.CommObject.WriteRandomDevice(wBuf) // === 1

        // working : 단 random device 갯수가 너무 많지 않으면...
        x.CommObject.ReadRandomDevice(rBuf) === 1

        noop()


    [<Test>]
    member x.``Write bit test`` () =
        let start = 16*5
        //for i = start to 1023 do
        //    x.CommObject.WriteDevice_Bit("M", i, 1) === 1
        for i = 128 to 256 do
            x.CommObject.WriteDevice_Bit("M", i, 1) === 1


    [<Test>]
    member x.``Write Q0 bit test`` () =
        x.CommObject.WriteDevice_Bit("Q", 0, 1) === 1
    [<Test>]
    member x.``Write Q0 and read it test`` () =
        x.CommObject.WriteDevice_Bit("Q", 0, 1) === 1

        x.CommObject.RemoveAll()
        let di = x.CreateDevice('Q', 'B', 8, 0)
        x.CommObject.AddDeviceInfo(di)
        let rBuf = Array.zeroCreate<byte>(8)
        x.CommObject.ReadRandomDevice(rBuf) === 1
        (rBuf[0] &&& 1uy) === 1uy

    /// ReadDevice_Bit NOT working
    [<Test>]
    member x.``X Read bit test`` () =
        let start = 16*5
        for i = start to 1023 do
            let nRead = 0
            x.CommObject.ReadDevice_Bit("M", i, ref nRead) === 1
            nRead = 1
            noop()


    [<Test>]
    member x.``Write bit and read random test`` () =
        //let start = 16*5
        let start = 0

        //for i = start to 1023 do
        //    x.CommObject.WriteDevice_Bit("M", i, 1) === 1

        let di = x.CreateDevice('M', 'B')

        let rBuf = Array.zeroCreate<byte>(MAX_ARRAY_BYTE_SIZE)
        x.CommObject.RemoveAll()
        for i = start to start+MAX_RANDOM_READ_POINTS-1 do
            di.lOffset <- i * 8
            x.CommObject.AddDeviceInfo(di)

        x.CommObject.ReadRandomDevice(rBuf) === 1
        noop()


    [<Test>]
    member x.``Write bit and read random with offset test`` () =
        x.CommObject.RemoveAll()
        let start = 1

        //for i = start to 1023 do
        //    x.CommObject.WriteDevice_Bit("M", i, 1) === 1

        let di = x.CreateDevice('M', 'B')

        let rBuf = Array.zeroCreate<byte>(MAX_ARRAY_BYTE_SIZE)
        for i in start*8 .. 8 .. ((start+MAX_RANDOM_READ_POINTS)*8 - 1) do
            //di.lOffset <- i * 8
            di.lOffset <- i
            x.CommObject.AddDeviceInfo(di)


        x.CommObject.ReadRandomDevice(rBuf) === 1
        noop()

    [<Test>]
    member x.``Read random of Q test`` () =
        x.CommObject.RemoveAll()

        let rBuf = Array.zeroCreate<byte>(8)
        let di = x.CreateDevice('Q', 'B')
        x.CommObject.AddDeviceInfo(di)
        x.CommObject.ReadRandomDevice(rBuf) === 1
        noop();


    [<Test>]
    member x.``Read random of I test`` () =
        x.CommObject.RemoveAll()

        let di = x.CreateDevice('I', 'B')

        let rBuf = Array.zeroCreate<byte>(8)
        x.CommObject.AddDeviceInfo(di)

        x.CommObject.ReadRandomDevice(rBuf) === 1
        noop();


    /// Slot change: Does NOT work
    [<Test>]
    member x.``Read random of Q 2nd slot test`` () =
        x.CommObject.RemoveAll()

        let rBuf = Array.zeroCreate<byte>(8)
        let di = x.CreateDevice('Q', 'B')
        x.CommObject.AddDeviceInfo(di)

        (* SetBaseSlot 은 동작하지않고, 위의 di.lOffset 에 8 을 넣었을 때(8byte skip 해서 QX0.1.??) 에는 제대로 읽어 들임.  *)
        x.CommObject.SetChNo(0uy)               // retuns unit ???
        x.CommObject.SetBaseSlot(0uy, 1uy)      // retuns unit ???

        x.CommObject.ReadRandomDevice(rBuf) === 1
        noop();



    [<Test>]
    member x.``Clear bit test`` () =
        for i = 0 to 1023 do
            x.CommObject.WriteDevice_Bit("M", i, 0) === 1

