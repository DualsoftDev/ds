namespace PLC.Convert.Siemens

open System
open System.Collections.Generic
open PLC.Convert
open PLC.Convert.LSCore.Expression
open ConvertSiemensModule

module ConvertSiemensCoilModule =


    let getCoils (networks: Network array) : seq<Terminal> =
        
        let isCoil = function
            | Coil _ -> true
            | _ -> false

        let isA_Contact = function
            | A_Contact _ -> true
            | _ -> false

        let isB_Contact = function
            | B_Contact _ -> true
            | _ -> false

        let filterNetwork (n: Network) =
            let hasCoil = n.Content |> Seq.exists isCoil
            let hasContact = n.Content |> Seq.exists (fun x -> isA_Contact x || isB_Contact x)
            hasCoil && hasContact

        let dictRung = Dictionary<string, Terminal>()

        networks
        |> Array.filter filterNetwork
        |> Array.iter (fun n ->
            match n.Content |> Seq.tryFind isCoil with
            | Some (Coil coil) when not (dictRung.ContainsKey(coil)) ->
                let coilExpr = Terminal(LSCore.Symbol(coil))
                dictRung.Add(coil, coilExpr)
            | _ -> ()
        )

        let rungs =
            networks
            |> Array.filter filterNetwork
            |> Array.map (fun n ->
                match n.Content |> Seq.tryFind isCoil with
                | Some (Coil coil) ->
                    let contactAs = n.Content |> Seq.choose (function A_Contact x -> Some x | _ -> None)
                    let contactBs = n.Content |> Seq.choose (function B_Contact x -> Some x | _ -> None)
                    Rung(coil, contactAs, contactBs, dictRung)
                | _ -> failwith "Invalid Network Structure"
            )

        rungs
        |> Array.collect (fun rung -> rung.RungExprs |> Seq.cast<Terminal> |> Seq.toArray )
        |> Seq.filter (fun terminal -> terminal.HasInnerExpr)
