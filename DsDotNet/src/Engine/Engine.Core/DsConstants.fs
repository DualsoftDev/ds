// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Core

open System
open Dual.Common.Core.FS
open System.Runtime.CompilerServices

[<AutoOpen>]
module DsConstants =


    /// 사용자 모델링한 edge type (단 reverse 는 반대로 뒤집어서)
    type ModelingEdgeType =
        | StartEdge          (*  ">"    *)
        | ResetEdge          (*  "|>"   *)
        | StartReset         (*  "=>"   *)
        | SelfReset          (*  "=|>"  *)
        | Interlock          (*  "<|>"  *)
        | RevStartEdge       (*  "<"    *)
        | RevResetEdge       (*  "<|"   *)
        | RevStartReset      (*  "<="   *)
        | RevSelfReset       (*  "<|="  *)

    /// Runtime Edge Types
    [<Flags>]
    type EdgeType =
        | None                       = 0b00000000    // Invalid state
        | Start                      = 0b00000001    // Start, Weak
        | Reset                      = 0b00000010    // else start
        | Strong                     = 0b00000100    // else weak
        | AugmentedTransitiveClosure = 0b00001000    // 강한 상호 reset 관계 확장 edge

    type internal MET = ModelingEdgeType
    type internal RET = EdgeType

    /// 뒤집힌 edge 판정.  뒤집혀 있으면 source target 을 반대로 하고 edge 를 다시 뒤집을 것.
    let isReversedEdge(modelingEdgeType:ModelingEdgeType) =
        match modelingEdgeType with
        | RevStartEdge
        | RevResetEdge
        | RevStartReset
        | RevSelfReset
            -> true
        | _ -> false

    let private modelingEdgeTypeAndStrings =
        [
            StartEdge,          TextStartEdge          // ">"
            ResetEdge,          TextResetEdge          // "|>"
            StartReset,         TextStartReset         // "=>"
            SelfReset,          TextSelfReset          // "=|>"
            Interlock,          TextInterlock          // "<|>"
            RevStartEdge,       TextStartEdgeRev       // "<"
            RevResetEdge,       TextResetEdgeRev       // "<|"
            RevStartReset,      TextStartResetRev      // "<="
            RevSelfReset,       TextSelfResetRev       // "<|="
        ]

    let toTextModelEdge(edgeType:ModelingEdgeType) =
        modelingEdgeTypeAndStrings |> filter( fst >> (=) edgeType) |> Seq.head |> snd

    let toModelEdge(edgeText:string) =
        let result =
            modelingEdgeTypeAndStrings
            |> filter( snd >> (=) edgeText)
            |> Seq.tryExactlyOne
            |> Option.map fst
        match result with
        | Some modelingEdge -> modelingEdge
        | _ -> failwithf $"'{edgeText}' is not modelEdgeType"

    type ModelingEdgeInfo<'v>(sources:'v seq, edgeSymbol:string, targets:'v seq) =
        new(source, edgeSymbol, target) = ModelingEdgeInfo([source], edgeSymbol, [target])
        member val Sources = sources.ToFSharpList()
        member val Targets = targets.ToFSharpList()
        member val EdgeSymbol = edgeSymbol
        member x.IsReversedEdge = isReversedEdge (toModelEdge edgeSymbol)
        member x.EdgeType = toModelEdge edgeSymbol

    /// source 와 target 을 edge operator 에 따라서 확장 생성
    let expandModelingEdge (modelingEdgeInfo:ModelingEdgeInfo<'v>) : ('v * EdgeType * 'v) list =
        let mi = modelingEdgeInfo
        let ss, edgeSymbol, ts = mi.Sources, mi.EdgeSymbol, mi.Targets
        [
            for s in ss do
            for t in ts do
                yield!
                    match edgeSymbol with
                    | (* ">"    *) TextStartEdge     -> [(s, RET.Start, t)]
                    | (* "|>"   *) TextResetEdge     -> [(s, RET.Reset, t)]

                    | (* "=>"   *) TextStartReset    -> [(s, RET.Start, t); (t, RET.Reset, s)]
                    | (* "=|>"  *) TextSelfReset     -> [(s, RET.Start, t); (s, RET.Reset, t); (t, RET.Reset, s)]


                    | (* "<|>"  *) TextInterlock     -> [(s, RET.Reset, t); (t, RET.Reset, s)]


                    | (* "<"    *) TextStartEdgeRev  -> [(t, RET.Start, s)]
                    | (* "<|"   *) TextResetEdgeRev  -> [(t, RET.Reset, s)]
                    | (* "<="   *) TextStartResetRev -> [(t, RET.Start, s); (s, RET.Reset, t); ]
                    | (* "<|="   *)TextSelfResetRev  -> [(t, RET.Start, s); (t, RET.Reset, s);  (s, RET.Reset, t); ]

                    | _
                        -> failwithf "Unknown causal edge type: %s" edgeSymbol
        ]

[<Extension>]
type ModelingEdgeExt =
    [<Extension>]
    static member ToText(edgeType:EdgeType) =
        let t = edgeType
        let isStrong = t.HasFlag(RET.Strong)
        if t.HasFlag(RET.Reset) then
            if isStrong then "||>" else "|>"
        else
            if isStrong then ">>" else ">"

    [<Extension>]
    static member ToText(edgeType:ModelingEdgeType) = toTextModelEdge edgeType

    [<Extension>]
    static member ToModelEdge(edgeText:string) = toModelEdge edgeText

    [<Extension>]
    static member IsReversedEdge(edgeText:string) = isReversedEdge (toModelEdge edgeText)
