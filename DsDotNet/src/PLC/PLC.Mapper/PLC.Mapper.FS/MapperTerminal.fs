namespace PLC.Mapper.FS

open System

[<AutoOpen>]
module MapperTerminalModule =

            
    type Terminal =
        | Coil of PlcTerminal
        | ContactNega of PlcTerminal
        | ContactPosi of PlcTerminal
        | Other of PlcTerminal
        with override x.ToString() =
                match x with
                | Coil tag | ContactNega tag | ContactPosi tag | Other tag -> tag.Variable

    type Network = {
        Title: string
        Content: Terminal array
    }

    let getCoils (networks: Network array) : seq<Terminal> =
    
        let isCoil = function Coil _ -> true | _ -> false
        let isContact = function ContactPosi _ | ContactNega _ -> true | _ -> false

        let filterValidNetwork net =
            net.Content |> Array.exists isCoil &&
            net.Content |> Array.exists isContact

        let validNetworks = networks |> Array.filter filterValidNetwork

        validNetworks |> Seq.choose (fun net -> net.Content |> Array.tryFind isCoil)
