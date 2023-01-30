namespace T.DriverIO

open NUnit.Framework

open Engine.Parser.FS
open T
open T.CPU
open System
open Engine.Core
open Engine.Common.FS
open Engine.Cpu
open Engine.CodeGenCPU
open System.Linq
open Dsu.PLC.LS

[<AutoOpen>]
module DriverIOLSCommonTest =

    type DriverIOLSCommon() =
        inherit EngineTestBaseClass()
        let t = CpuTestSample()

        let conn = new LsConnection(LsConnectionParameters("192.168.0.100"))

        member x.Connection = conn

        [<Test>]
        member __.``XXXXXXXXX LS Connection test`` () =
            let ls = DriverIOLSCommon()
            let conn = ls.Connection
            conn.PerRequestDelay <- 1000
            if conn.Connect()
            then true
            else false
