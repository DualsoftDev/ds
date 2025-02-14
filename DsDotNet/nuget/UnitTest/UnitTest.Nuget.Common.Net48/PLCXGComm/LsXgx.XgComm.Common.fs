namespace T


open Dual.Common.Core
open DsXgComm
open DsXgComm.Connect
open Dual.Common.Core.FS
open Dual.Common.Base.CS
open NUnit.Framework

[<AutoOpen>]
module LsXgxXgCommTestModule =

       // localhsot 에서 XG5000 시뮬레이터 구동 중일 때에만 true
    let isXgCommAvailable =
        let mutable available = false
        try
            let conn = new DsXgConnection("127.0.0.1")
            if conn.Connect(Some 1) then
                conn.Disconnect() |> ignore
                available <- true
        with ex ->
            forceTrace "Warning: XG COMM module not available.  Skip test"
        available

    let waitReadWriteTags(tags:XGTTag seq) =
        while tags.NonNullAny(fun t->t.GetWriteValue().IsSome) do ()
        while tags.NonNullAny(fun t->t.Value = null) do ()

    let startScan(tags:string seq) =
        let scan = XGTScan("127.0.0.1")
        scan.Scan(tags)

