namespace T

open NUnit.Framework
open Dual.UnitTest.Common.FS
open Dual.Common.Core.FS.PreludeAdhocPolymorphism
open Dual.Common.Core.FS.ActivePattern

module Test =

    [<SetUp>]
    let Setup () =
        ()

    [<Test>]
    let TestFalse () =
        1 === 2
    [<Test>]
    let TestAdhocMap () =
        let incr = (+) 1
        let tryIncr = Option.map incr
        Some 1 |> tryIncr === Some 2
        None |> tryIncr === None
        Some 1 |> map incr === Some 2

        match "123.4.5.7" with
        | IpPattern ip -> ()
        | _ -> failwith "ERROR"
