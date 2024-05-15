[<AutoOpen>]
module Engine.CodeGenCPU.ConvertVariableData

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type VariableManager with

    member v.V1_ConstMove(sys:DsSystem) =
         (sys._on.Expr, v.InitValue |> literal2expr) --> (v.VariableTag, getFuncName())

         