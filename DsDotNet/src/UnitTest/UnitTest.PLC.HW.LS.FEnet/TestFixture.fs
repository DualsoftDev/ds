namespace T

open NUnit.Framework
open Dsu.PLC.LS
open FSharpPlus
open AddressConvert
open Dual.Common.Core.FS

[<AutoOpen>]
module FEnetTestModule =
    [<AbstractClass>]
    type FEnetTestBase(?ip, ?port) =
        inherit TestBaseClass("LsHwPLCFEnet")
        let ip = ip |? "192.168.0.101"
        let port = port |? 2004
        let conn = new LsConnection(LsConnectionParameters(ip, uint16 port))

        abstract member CreateLsTag: string -> bool -> LsTag

        member x.WriteTagValue(tag, value:obj, convertFEnet) =
            let lsTag = x.CreateLsTag tag convertFEnet
            lsTag.Value <- value
            conn.WriteATag(lsTag) |> ignore
        member x.Write(lsTag:LsTag, value) =
            lsTag.Value <- value
            conn.WriteATag(lsTag) |> ignore
        member x.Write(tag:string, value) = x.WriteTagValue(tag, value, true)
        member x.WriteFEnet(tag, value) = x.WriteTagValue(tag, value, false)
        member _.Read(tag:string) = conn.ReadATag(tag)
        member _.ReadFEnet(tag:string) = conn.ReadATagFEnet(tag)

        [<SetUp>]
        member _.Setup() =
            conn.Connect() === true
            conn.PrintStatus()



        [<TearDown>]
        member _.TearDown() =
            conn.Disconnect() === true

        member _.Conn = conn

