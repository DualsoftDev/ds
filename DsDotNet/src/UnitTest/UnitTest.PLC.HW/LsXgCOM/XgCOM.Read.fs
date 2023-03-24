namespace T

open System
open Xunit
open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Engine.Common.FS
open PLC.CodeGen.LSXGI
open PLC.CodeGen.Common.FlatExpressionModule
open AddressConvert

[<Collection("SerialXgiGenerationTest")>]
type XgCOMReadTest() =
    inherit XgCOMBaseClass()

    [<Test>]
    member x.``test`` () =
        let a = (|LsTagPatternXgk|_|) "%M00000"
        let di = x.Factory.CreateDevice()
        di.ucDeviceType <- Convert.ToByte('M')
        di.ucDataType <- Convert.ToByte('B')

        for i = 0 to 10 do
            di.lSize <- 8
            di.lOffset <- i * 8
            x.CommObject.AddDeviceInfo(di)

        let buf = Array.zeroCreate<byte>(1024)
        buf[0] <- byte 0xab;
        x.CommObject.ReadRandomDevice(buf)

        noop()


        1 === 1



