namespace Engine.Cpu

open Engine.Core
open System.Runtime.CompilerServices

[<AutoOpen>]
module CoreExtensionsModule =
    type Statement with
        member x.GetTargetStorages() =
            match x with
            | DuAssign (expr_, target) -> [ target ]
            | DuVarDecl (expr_, var) -> [ var ]
            | DuTimer timerStatement ->
                [ for s in timerStatement.Timer.InputEvaluateStatements do
                    yield! s.GetTargetStorages() ]
            | DuCounter counterStatement ->
                [ for s in counterStatement.Counter.InputEvaluateStatements do
                    yield! s.GetTargetStorages() ]
            | DuAction (DuCopy (condition_, source_, target)) -> [ target ]
            | DuAugmentedPLCFunction _ -> []

        member x.GetSourceStorages() =
            match x with
            | DuAssign (expr, target_) -> expr.CollectStorages()
            | DuVarDecl (expr, var_) -> expr.CollectStorages()
            | DuTimer timerStatement ->
                [ for s in timerStatement.Timer.InputEvaluateStatements do
                    yield! s.GetSourceStorages() ]
            | DuCounter counterStatement ->
                [ for s in counterStatement.Counter.InputEvaluateStatements do
                    yield! s.GetSourceStorages() ]
            | DuAction (DuCopy (condition, source_, target_)) -> condition.CollectStorages()
            | DuAugmentedPLCFunction _ -> []



    let getTargetStorages (x:Statement) = x.GetTargetStorages()
    let getSourceStorages (x:Statement) = x.GetSourceStorages()
    let getAutoButtons (x:DsSystem) = x.AutoButtons


//[<Extension>]
//type StatementExt =
//    [<Extension>] static member GetTargetStorages (x:Statement) = x.GetTargetStorages()
//    [<Extension>] static member GetSourceStorages (x:Statement) = x.GetSourceStorages()


    //[<Extension>]
    //type ExpressionExt =
        //[<Extension>]
        //static member NotifyStatus (x:PlanTag<bool>) = () //test ahn
        //    //if x.Value then
        //    //    match x.TagFlag with
        //    //    | R -> ChangeStatusEvent (x.Vertex, Ready)
        //    //    | G -> ChangeStatusEvent (x.Vertex, Going)
        //    //    | F -> ChangeStatusEvent (x.Vertex, Finish)
        //    //    | H -> ChangeStatusEvent (x.Vertex, Homing)
        //    //    | _->()