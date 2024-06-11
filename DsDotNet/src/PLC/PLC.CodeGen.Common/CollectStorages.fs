namespace PLC.CodeGen.Common

open Dual.Common.Core.FS
open Engine.Core

[<AutoOpen>]

module CollectStoragesModule =

    type TimerStatement with

        member x.CollectStorages() : IStorage list =
            [ yield x.Timer.TimerStruct
              let conditions = [ x.RungInCondition; x.ResetCondition ] |> List.choose id

              for cond in conditions do
                  yield! cond.CollectStorages() ]

    type CounterStatement with

        member x.CollectStorages() : IStorage list =
            [ yield x.Counter.CounterStruct
              let conditions =
                  [ x.UpCondition; x.DownCondition; x.ResetCondition; x.LoadCondition ]
                  |> List.choose id

              for cond in conditions do
                  yield! cond.CollectStorages() ]

    type ActionStatement with

        member x.CollectStorages() : IStorage list =
            [   match x with
                | DuCopy(cond, src, tgt) ->
                    yield! cond.CollectStorages()
                    yield! src.CollectStorages()
                    yield tgt
                | DuCopyUdt(_, _, cond, _src, _tgt) ->
                    yield! cond.CollectStorages() ]

    type Statement with

        member x.CollectStorages() : IStorage list =
            [
                match x with
                | DuAssign(condition, exp, tgt) ->
                    match condition with
                    | Some condition ->
                        yield! condition.CollectStorages()
                    | None -> ()
                    yield! exp.CollectStorages()
                    yield tgt
                
                /// 변수 선언.  PLC rung 생성시에는 관여되지 않는다.
                | DuVarDecl(exp, var) ->
                    yield! exp.CollectStorages()
                    yield var

                | DuTimer stmt -> yield! stmt.CollectStorages()
                | DuCounter stmt -> yield! stmt.CollectStorages()
                | DuAction stmt -> yield! stmt.CollectStorages()

                | DuPLCFunction _functionParameters -> failwithlog "ERROR"
                | (DuUdtDecl _ | DuUdtDefinitions _) -> failwith "Unsupported"
            ]
    type CommentedStatement with

        member x.CollectStorages() : IStorage list = x.Statement.CollectStorages()
