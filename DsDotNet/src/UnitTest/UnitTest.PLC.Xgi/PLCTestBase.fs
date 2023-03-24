namespace T

open NUnit.Framework
open Dsu.PLC.LS
open AddressConvert



[<AutoOpen>]
module PLCTest =
        

    [<AbstractClass>]
    type PLCTestBase(ip : string) =
        let mutable conn = new LsConnection(LsConnectionParameters(ip))
        
        [<SetUp>]
        member X.Setup() =
            conn.Connect() |> ignore

            
        [<TearDown>]
        member X.TearDown() =
            conn.Disconnect() |> ignore
        

        member X.Conn
            with get() = conn
       



      