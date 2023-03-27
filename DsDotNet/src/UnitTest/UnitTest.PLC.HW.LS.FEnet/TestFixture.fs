namespace T

open NUnit.Framework
open Dsu.PLC.LS
open FSharpPlus
open AddressConvert
open Engine.Common.FS

[<AutoOpen>]
module FEnetTestModule =
    [<AbstractClass>]
    type FEnetTestBase(?ip, ?port) =
        inherit TestBaseClass("LsHwPLCFEnet")
        let ip = ip |? "192.168.0.101"
        let port = port |? 2004
        let conn = new LsConnection(LsConnectionParameters(ip, uint16 port))

        [<SetUp>]
        member X.Setup() =
            conn.Connect() === true


        [<TearDown>]
        member X.TearDown() =
            conn.Disconnect() === true

        member X.Conn = conn

