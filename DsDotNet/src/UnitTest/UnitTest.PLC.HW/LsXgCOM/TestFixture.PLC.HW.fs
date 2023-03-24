namespace T

open System.IO
open System.Globalization

open NUnit.Framework
open Engine.Common.FS
open Engine.Core
open PLC.CodeGen.LSXGI
open XGCommLib
open System


[<AutoOpen>]
module XgCOMFixtures =
#if USEV20
    type XCommObjectFactory = CommObjectFactory20
    type XCommObject = CommObject20
    let guidComObjectFactory = Guid("7BBF93C0-7C64-4205-A2B0-45D4BD1F51DC")
#else
    type XCommObjectFactory = CommObjectFactory
    type XCommObject = CommObject
    let guidComObjectFactory = Guid("338B2AC0-EE93-4C53-8AF9-F079F7075CB4")
#endif


    [<AbstractClass>]
    type XgCOMBaseClass(?connStr) =
        inherit TestBaseClass("HWPLCLogger")
#if !X64
        do
            if (Environment.Is64BitProcess) then
                failwithlog "Only support 32-bit."
#endif

        //let connStr = connStr |? "192.168.0.101:2004"
        let connStr = connStr |? "192.168.0.111:2004"

        member val CommObject:XCommObject = null with get, set
        member val Factory:XCommObjectFactory = null with get, set

        [<SetUp>]
        member x.Setup () =
            x.Factory <-
                // DO NOT WORK : let t = Type.GetTypeFromProgID("XGCommLib.CommObjectFactory")
                let t = Type.GetTypeFromCLSID(guidComObjectFactory) // CommObjectFactory
                Activator.CreateInstance(t) :?> XCommObjectFactory
#if USEV20
            x.CommObject <- x.Factory.GetMLDPCommObject20(connStr)
#else
            x.CommObject <- x.Factory.GetMLDPCommObject(connStr)
#endif
            if 0 = x.CommObject.Connect("") then
                failwithlog "Failed to connect to XG."

            logInfo "Connection established."
            ()

        [<TearDown>]
        member x.TearDown () =
            x.CommObject.Disconnect() |> ignore
            ()

