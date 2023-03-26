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


(*
    XGComLib summary
    ----------------
    모든 version 에서 ReadRandomDevice 는 동작함.
       - 단, 64점 이상인 경우, fail 함
    V20 버젼 사용시, WriteDevice_Bit 동작함.
    32/64 bit 모두 동일 한 듯.
*)
[<Collection("SerialXgiGenerationTest")>]
type XgCOMReadTest() =
    inherit XgCOMBaseClass()

    [<Test>]
    member x.``Basic read/write test`` () =
        x.CommObject.IsConnected() === 1
        let plcId = x.CommObject.GetPLCID;

        let di = x.Factory.CreateDevice()
        di.ucDeviceType <- Convert.ToByte('M')
        di.ucDataType <- Convert.ToByte('B')

        let wBuf = Array.zeroCreate<byte>(1024)
        let rBuf = Array.zeroCreate<byte>(1024)
        x.CommObject.RemoveAll()
        for i = 0 to 1023 do
            di.lSize <- 8
            di.lOffset <- i * 8
            wBuf[i] <- byte i
            if i < 64 then
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




