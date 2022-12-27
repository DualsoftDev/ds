[<AutoOpen>]
module Engine.CodeGenCPU.ConvertExtenstion

open System.Linq
open Engine.Core
open Engine.CodeGenCPU



type DsSystem with
    //머리가슴배
    member s.E1_SplitReal(): CommentedStatement =
        //test ahn
        (s._run.Expr, s._run.Expr) --| (s._run, "E1" )
