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
            [ match x with
              | DuCopy(cond, src, tgt) ->
                  yield! cond.CollectStorages()
                  yield! src.CollectStorages()
                  yield tgt ]

    type Statement with

        member x.CollectStorages() : IStorage list =
            [ match x with
              | DuAssign(exp, tgt) ->
                  yield! exp.CollectStorages()

                  match tgt with
                  | :? RisingCoil as rc -> yield rc.Storage
                  | :? FallingCoil as fc -> yield fc.Storage
                  | _ -> yield tgt
              /// 변수 선언.  PLC rung 생성시에는 관여되지 않는다.
              | DuVarDecl(exp, var) ->
                  yield! exp.CollectStorages()
                  yield var

              | DuTimer stmt -> yield! stmt.CollectStorages()
              | DuCounter stmt -> yield! stmt.CollectStorages()
              | DuAction stmt -> yield! stmt.CollectStorages()

              | DuAugmentedPLCFunction _functionParameters -> failwithlog "ERROR" ]

    type CommentedStatement with

        member x.CollectStorages() : IStorage list = x.Statement.CollectStorages()
