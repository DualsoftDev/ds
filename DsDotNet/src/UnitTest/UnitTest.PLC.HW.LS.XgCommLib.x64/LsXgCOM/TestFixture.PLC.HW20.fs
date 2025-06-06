#if X64
namespace Tx64
#else
namespace Tx86
#endif
open T

open System.IO
open System.Globalization

open NUnit.Framework
open Engine.Common.FS
open Engine.Core
open PLC.CodeGen.LSXGI
open XGCommLib
open System


[<AutoOpen>]
module XgCOMFixtures20 =
    type XCommObjectFactory = CommObjectFactory20
    type XCommObject = CommObject20
    let guidComObjectFactory = Guid("7BBF93C0-7C64-4205-A2B0-45D4BD1F51DC")

#if X64
    let isX64Defined = true
#else
    let isX64Defined = false
#endif


    [<AbstractClass>]
    type XgCOMBaseClass20(?connStr) =
        inherit TestBaseClass("HWPLCLogger")
        do
            if (Environment.Is64BitProcess <> isX64Defined ) then
                failwithlog "Platform and X64 compile flag mismatch"

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

