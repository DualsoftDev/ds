namespace T

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


[<Collection("SerialXgiGenerationTest")>]
type XgCOMReadTest() =
    inherit XgCOMBaseClass()

    [<Test>]
    member x.``Basic read/write test`` () =
        //x.CommObject.IsConnected === true
        let plcId = x.CommObject.GetPLCID;

        let di = x.Factory.CreateDevice()
        di.ucDeviceType <- Convert.ToByte('M')
        di.ucDataType <- Convert.ToByte('B')

        let wBuf = Array.zeroCreate<byte>(1024)
        let rBuf = Array.zeroCreate<byte>(1024)
        x.CommObject.RemoveAll()
        for i = 0 to 10 do
            di.lSize <- 8
            di.lOffset <- i * 8
//  member Write: nCode: int * bufIn: System.Array * nSndSize: int * nOffset: int -> int
//  member Read:  nCode: int * bufIn: System.Array * nRcvSize: int * nOffset: int -> int
            wBuf[i] <- byte i
            x.CommObject.AddDeviceInfo(di)

        // does *NOT* working
        x.CommObject.Write((int)XgCOMCodes.W_M, wBuf, 1024, 0) |> ignore
        x.CommObject.Read((int)XgCOMCodes.R_M, rBuf, 1024, 0) |> ignore


        // working
        x.CommObject.ReadRandomDevice(rBuf) |> ignore

        noop()




