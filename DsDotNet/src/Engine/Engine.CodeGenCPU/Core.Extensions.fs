namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open System

[<AutoOpen>]
module ConvertCoreExt =
    
    // vertex 를 coin 입장에서 봤을 때의 extension methods
    type Vertex with
        member coin.GetCoinTags(memory:VertexManager, isInTag:bool) : Tag<bool> seq =
            match coin with
            | :? Call as c ->
                [ for j in c.CallTarget.JobDefs do
                    let typ = if isInTag then "I" else "O"
                    PlcTag( $"{j.ApiName}_{typ}", "", false) :> Tag<bool>
                ]
            | :? Alias as a ->
                match a.TargetWrapper with
                | DuAliasTargetReal ar    -> ar.GetCoinTags(memory, isInTag)
                | DuAliasTargetCall ac    -> ac.GetCoinTags(memory, isInTag)
                | DuAliasTargetRealEx ao  -> ao.GetCoinTags(memory, isInTag)
            | _ -> failwith "Error"
        member coin.GetTxRxTags(isTx:bool, memory:VertexManager) : Tag<bool> seq =
            let getVertexManager(v:Vertex) = v.VertexManager :?> VertexManager

            match coin with
            | :? Call as c ->
                c.CallTarget.JobDefs
                    .SelectMany(fun j->
                        if isTx then
                            j.ApiItem.TXs.Select(fun s -> (getVertexManager s).StartTag)
                        else
                            j.ApiItem.RXs.Select(fun s -> (getVertexManager s).EndTag)
                    )
                    .Cast<Tag<bool>>()
            | :? Alias as a ->
                match a.TargetWrapper with
                | DuAliasTargetReal ar    -> ar.GetCoinTags(memory, isTx)
                | DuAliasTargetCall ac    -> ac.GetCoinTags(memory, isTx)
                | DuAliasTargetRealEx ao  -> ao.GetCoinTags(memory, isTx)
            | _ -> failwith "Error"
    
    
    type DsTag<'T when 'T:equality> with
        member t.ToExpr = tag2expr t
    type PlcTag<'T when 'T:equality> with
        member t.ToExpr = tag2expr t
    //DsBit는 VertexManager expr Member 사용

    type DsSystem with
        member s._on     = DsTag<bool>("_on", false)
        member s._off    = DsTag<bool>("_off", false)
        member s._auto   = DsTag<bool>("_auto", false)
        member s._manual = DsTag<bool>("_manual", false)
        member s._emg    = DsTag<bool>("_emg", false)
        member s._run    = DsTag<bool>("_run", false)
        member s._stop   = DsTag<bool>("_stop", false)
        member s._clear  = DsTag<bool>("_clear", false)
        member s.dryrun  = DsTag<bool>("dryrun", false)
        member s._yy     = DsTag<int> ("_yy", 0)
        member s._mm     = DsTag<int> ("_mm", 0)
        member s._dd     = DsTag<int> ("_dd", 0)
        member s._h      = DsTag<int> ("_h", 0)
        member s._m      = DsTag<int> ("_m", 0)
        member s._s      = DsTag<int> ("_s", 0)
        member s._ms     = DsTag<int> ("_ms", 0)

    type Flow with
        member f.GetCoinTags() : Tag<bool> seq = []
         