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
    [<AbstractClass>]
    type XgCOMBaseClass(?connStr) =
        inherit TestBaseClass("HWPLCLogger")
        do
            if (Environment.Is64BitProcess) then
                failwithlog "Only support 32-bit."
        //let connStr = connStr |? "192.168.0.101:2004"
        let connStr = connStr |? "192.168.0.111:2004"

        member val CommObject:CommObject = null with get, set
        member val Factory:CommObjectFactory = null with get, set
        [<SetUp>]
        member x.Setup () =
            //x.Factory <-
            //    //let t = Type.GetTypeFromProgID("XGCommLib.CommObjectFactory")
            //    let t = Type.GetTypeFromCLSID(Guid("338B2AC0-EE93-4C53-8AF9-F079F7075CB4")) // CommObjectFactory
            //    Activator.CreateInstance(t) :?> CommObjectFactory

            //x.CommObject <- x.Factory.GetMLDPCommObject(connStr)
            //if 0 = x.CommObject.Connect("") then
            //    failwithlog "Failed to connect to XG."
            //logInfo "Connection established."
            ()

        [<TearDown>]
        member x.TearDown () =
            x.CommObject.Disconnect() |> ignore
            ()

