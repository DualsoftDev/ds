[<AutoOpen>]
module Engine.CodeGenCPU.ConvertSystem

open System.Linq
open Engine.Core
open Engine.CodeGenCPU



type DsSystem with
    
    member s.B1_AllButtons(): CommentedStatement =
        //test ahn
        (s._run.Expr, s._run.Expr) --| (s._run, "B1" )

    member s.B2_AllLamps(): CommentedStatement =
        //test ahn
        (s._run.Expr, s._run.Expr) --| (s._run, "B2" )
