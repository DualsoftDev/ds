#if X64
namespace Tx64
#else
namespace Tx86
#endif
open T

open System.IO
open System.Globalization

open NUnit.Framework
open Dual.Common.Core.FS
open Engine.Core
open PLC.CodeGen.LSXGI
open XGCommLib
open System


[<AutoOpen>]
module XgCOMFixtures =

#if X64
    let isX64Defined = true
#else
    let isX64Defined = false
#endif


    //대신 XgCOMBaseClass20 사용"
    [<AbstractClass>]
    type XgCOMBaseClass(?connStr) =
        inherit TestBaseClass("HWPLCLogger")
        do
            if (Environment.Is64BitProcess <> isX64Defined ) then
                failwithlog "Platform and X64 compile flag mismatch"

        //let connStr = connStr |? "192.168.0.101:2004"
        let connStr = connStr |? "192.168.0.100:2004"

        member val CommObject:CommObject = null with get, set
        member val Factory:CommObjectFactory = null with get, set

        [<SetUp>]
        member x.Setup () =
            x.Factory <-
                // DO NOT WORK : let t = Type.GetTypeFromProgID("XGCommLib.CommObjectFactory")
                let t = Type.GetTypeFromCLSID(Guid("338B2AC0-EE93-4C53-8AF9-F079F7075CB4")) // CommObjectFactory
                Activator.CreateInstance(t) :?> CommObjectFactory
            x.CommObject <- x.Factory.GetMLDPCommObject(connStr)
            if 0 = x.CommObject.Connect("") then
                failwithlog "Failed to connect to XG."

            logInfo "Connection established."
            ()

        [<TearDown>]
        member x.TearDown () =
            x.CommObject.Disconnect() |> ignore
            ()



    [<AbstractClass>]
    type XgCOMBaseClass20(?connStr) =
        inherit TestBaseClass("HWPLCLogger")
        do
            if (Environment.Is64BitProcess <> isX64Defined ) then
                failwithlog "Platform and X64 compile flag mismatch"

        let connStr = connStr |? "192.168.0.100:2004"

        member val CommObject:CommObject20 = null with get, set
        member val Factory:CommObjectFactory20 = null with get, set

        [<SetUp>]
        member x.Setup () =
            x.Factory <-
                // DO NOT WORK : let t = Type.GetTypeFromProgID("XGCommLib.CommObjectFactory")
                let t = Type.GetTypeFromCLSID(Guid("7BBF93C0-7C64-4205-A2B0-45D4BD1F51DC")) // CommObjectFactory
                Activator.CreateInstance(t) :?> CommObjectFactory20
            x.CommObject <- x.Factory.GetMLDPCommObject20(connStr)
            if 0 = x.CommObject.Connect("") then
                failwithlog "Failed to connect to XG."

            logInfo "Connection established."
            ()

        [<TearDown>]
        member x.TearDown () =
            x.CommObject.Disconnect() |> ignore
            ()

        member x.CreateDevice(deviceType:char, memType:char, ?size:int, ?offset:int) : DeviceInfo =
            let size = size |? 8
            let offset = offset |? 0
            let di = x.Factory.CreateDevice()
            di.ucDeviceType <- Convert.ToByte(deviceType)
            di.ucDataType <- Convert.ToByte(memType)
            di.lSize <- size
            di.lOffset <- offset

            di