namespace Engine.Cpu

open Engine.Core
open Dual.Common.Core.FS;
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
open System
open System.Collections.Generic
open System.Reactive.Subjects



[<AutoOpen>]
module DsProcessEvent =

    type ProParam = |PRO of Time:DateTime * pro:int

    let mutable CurrProcess:int = 0
    let ProcessSubject = new Subject<ProParam>()

    let DoWork  (pro:int) =
        CurrProcess <- pro
        ProcessSubject.OnNext(ProParam.PRO (DateTime.Now, pro))



[<AutoOpen>]
module CoreExtensionsModule =
    type Statement with
     
        member x.GetTargetStorages() =
            match x with
            | DuAssign (_expr, (:? RisingCoil as rc))  -> [ rc.Storage]
            | DuAssign (_expr, (:? FallingCoil as fc)) -> [ fc.Storage]
            | DuAssign (_expr, target) -> [ target ]
            | DuVarDecl (_expr, var) -> [ var ]
            | DuTimer timerStatement -> [timerStatement.Timer.DN ]
            | DuCounter counterStatement -> [counterStatement.Counter.DN ]
            | DuAction (DuCopy (_condition, _source, target)) -> [ target ]
            | DuAugmentedPLCFunction _ -> []

        member x.GetSourceStorages() =
            match x with
            | DuAssign (expr, _target) -> expr.CollectStorages()
            | DuVarDecl (expr, _var) -> expr.CollectStorages()
            | DuTimer timerStatement -> 
                [ for s in timerStatement.Timer.InputEvaluateStatements do
                    yield! s.GetSourceStorages() ]
            | DuCounter counterStatement ->
                [ for s in counterStatement.Counter.InputEvaluateStatements do
                    yield! s.GetSourceStorages() ]
            | DuAction (DuCopy (condition, _source, _target)) -> condition.CollectStorages()
            | DuAugmentedPLCFunction _ -> []

      

    let getTargetStorages (x:Statement) = x.GetTargetStorages() |> List.toSeq
    let getSourceStorages (x:Statement) = x.GetSourceStorages() |> List.toSeq
    let getAutoButtons (x:DsSystem) = x.AutoButtons

    [<Extension>]
    type ExpressionExt =
        [<Extension>] static member ChangedTags (xs:IStorage seq) = 
                        xs |> Seq.where(fun w -> w.TagChanged)
                           |> Seq.toArray   //list 아니면 TagChanged 정보 없는 초기화 이후 정보 가져오더라도 항목 유지

        [<Extension>] static member ChangedTagsClear (xs:IStorage seq, systems:DsSystem seq) = 
                        xs |> Seq.where(fun w ->  systems.any(fun s-> s:>ISystem = w.DsSystem))//자신 시스템에서만 TagChanged  <- false 가능
                           |> Seq.iter(fun w -> w.TagChanged <- false)

        [<Extension>] static member ExecutableStatements (xs:IStorage seq, mRung:IDictionary<IStorage, Statement seq>) = 
                        xs |> Seq.collect(fun stg -> mRung[stg]) 

      
                        

        [<Extension>]
        static member IsEndThread (x:IStorage) =
            match x.GetApiTagKind() with  //외부 시스템 관련 신호
            | Some _ -> true
            | _ ->
                match x.GetVertexTagKind() with
                //EndPortTag  일 경우 새로운 thread 생성
                | Some VertexTag.endPort -> true
                | Some VertexTag.relayCall -> true  /// relayCall 인과 H/S 필요??
                | _ -> false
                
        [<Extension>]
        static member IsStartThread (x:IStorage) =
            match x.GetApiTagKind() with  //외부 시스템 관련 신호
            | Some _ -> true
            | _ ->
                if Convert.ToBoolean(x.BoxedValue) //ON 신호만 StartThread 생성
                then
                    match x.GetVertexTagKind() with
                    | Some VertexTag.startPort -> true
                    | Some VertexTag.going -> false
                    | _ -> false
                else           
                    false
