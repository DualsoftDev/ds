[<AutoOpen>]
module Engine.CodeGenCPU.ConvertOperationMode

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type Flow with

    member f.O1_IdleOperationMode() =
        let set = !!f.aop.Expr <&&> !!f.mop.Expr
        let rst = f._off.Expr

        (set, rst) --| (f.iop, getFuncName())

    member f.O2_AutoOperationMode() =
        let set = f.AutoExpr 
        let rst = !!f.r_st.Expr
        
        (set, rst) --| (f.aop, getFuncName())

    member f.O3_ManualOperationMode () =
        let set = f.ManuExpr
        let rst = !!f.r_st.Expr 
        
        (set, rst) --| (f.mop, getFuncName())
