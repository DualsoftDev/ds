[<AutoOpen>]
module Engine.CodeGenCPU.ConvertFunctions

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type VertexMCall with
    member v.CallFunctionPS() : CommentedStatement =
        let set = v.MM.Expr
        (set, v._off.Expr) --| (v.PSFunc, getFuncName())

    member v.CallFunctionPE() : CommentedStatement =
        let set = v.PSFunc.Expr
        (set, v._off.Expr) --| (v.PEFunc, getFuncName())
