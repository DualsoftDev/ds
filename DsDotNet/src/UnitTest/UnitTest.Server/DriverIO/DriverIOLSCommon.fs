namespace T.DriverIO

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Dual.Common.Core.FS
open Engine.Cpu
open Engine.CodeGenCPU
open System.Linq

[<AutoOpen>]
module DriverIOLSCommonTest =

    type DriverIOLSCommon() =
        inherit EngineTestBaseClass()

        //let conn = new LsConnection(LsConnectionParameters("192.168.0.101"))

        //member x.Connection = conn

        //[<Test>]
        //member __.``XXXX LS Connection test`` () =
        //    let ls = DriverIOLSCommon()
        //    let conn = ls.Connection
        //    conn.PerRequestDelay <- 1000
        //    conn.Connect() === true
