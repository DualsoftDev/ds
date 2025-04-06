[<AutoOpen>]
module Engine.CodeGenCPU.ConvertEmulation

open System
open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type TaskDev with

    member d.SensorEmulation(coins:Vertex seq) =
        let call = coins.OfType<Call>().First()
        let rst = call.MutualResetExpr

        [|
            if call.ValueParamIO.In.DataType = DuBOOL then
                let set = coins.GetPureCalls()
                               .Select(fun c-> d.ApiItem.ApiItemEnd).ToOr()

                let positiveBool = call.ValueParamIO.In.ReadSimValue |> Convert.ToBoolean
                yield ((if positiveBool then set else !@set), rst) --| (d.InTag, getFuncName())
            else
                for c in coins.GetPureCalls() do
                    let setData = c.ValueParamIO.In.ReadSimValue |> any2expr
                    let set = d.ApiItem.ApiItemEnd.Expr
                    yield (set, setData) --> (d.InTag, getFuncName())
        |]
