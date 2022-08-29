namespace Dual.Core.Types

open Dual.Common
open Dual.Core.Prelude
open Dual.Core.Types
open System.Runtime.CompilerServices

module OnOffM =
    let toExpression x =
        match x with
        | TurnOn(n) -> Terminal(n)
        | TurnOff(f) -> Unary(Neg, (Terminal(f)))
    let getTurnOn x =
        match x with
        | TurnOn tag -> Some tag
        | _ -> None
    let getTurnOff x =
        match x with
        | TurnOff tag -> Some tag
        | _ -> None
    let getTag x =
        match x with
        | TurnOn tag | TurnOff tag -> tag

    //let fromString str =
    //    match str with
    //    | ActivePattern.RegexPattern @"([^+-]*)([+-]?)" [tag; nf] ->
    //        Some (if nf = "+" then TurnOn(tag) else TurnOff(tag))
    //    | _ -> None

// TODO : Gamma
#if false
module PUnitM =
    let getAction x =
        match x with
        | Action(pa) -> Some(pa)
        | _ -> None
    let getReference x =
        match x with
        | Reference(cond) -> Some(cond)
        | _ -> None

    /// PUnit -> PAction option
    let u2a = getAction
#endif

module PActionM =
    /// PAction 이 OnOffAction 인 경우, OnOffAction 을 반환하고, 그렇지 않으면 None 을 반환
    let getOnOffAction x =
        match x with
        | OnOffAction nf -> Some(nf)
        | _ -> None
    /// PAction 이 Parallel Actions 인 경우, ParallelActions 을 반환하고, 그렇지 않으면 None 을 반환
    let getParallelActions x =
        match x with
        | ParallelActions actions -> Some(actions)
        | _ -> None
    let getPLCAction x =
        match x with
        | PLCAction(cmd, param) -> Some(cmd, param)
        | _ -> None

    let onOffPActionToCondition (act:PAction) =
        match act with 
        | OnOffAction(TurnOn(tag))
        | OnOffAction(TurnOff(tag)) -> Some(Terminal(tag))
        | _ -> None

    /// Action 에 포함된 Tag 들을 expression 으로 환산해서 반환
    let PActionToCondition (act:PAction) =
        match act with 
        | OnOffAction(TurnOn(tag))
        | OnOffAction(TurnOff(tag))
            -> Some(Terminal(tag))
        | ParallelActions(actions) ->
            actions
            |> Seq.map onOffPActionToCondition
            |> Seq.choose id
            |> mkBinary And
            |> Some
        | PLCAction(_) | _
            -> None

    let collectTagsFromAction act =
        let rec collectdHelper act = 
            seq {
                match act with
                | OnOffAction(TurnOn(tag)) | OnOffAction(TurnOff(tag)) ->
                    yield tag |> box
                | PLCAction(c, p) -> yield! p.CollectTags() |> Seq.map box
                | ParallelActions(acts) -> yield! acts |> Seq.collect collectdHelper
            } 
        collectdHelper act |> Seq.ofType<PLCTag>
// TODO : Gamma
#if false
    let rec collectOnOffTagsFromAction act =
        seq {
            match act with
            | OnOffAction(TurnOn(tag)) | OnOffAction(TurnOff(tag)) ->
                yield tag.ToText()
            | ParallelActions(acts) -> yield! acts |> Seq.collect collectTagsFromAction
            | PLCAction(c, p) -> ()
        } 

    /// On/Off 에 관련된 모든 신호 수집.  Parallel 구문 내부까지 재귀적으로 수집
    let rec collectOnOffActions action =
        seq {
            match action with
            | OnOffAction(_) -> yield action
            | PLCAction(c, p) -> yield OnOffAction(TurnOn(PseudoTerminal(p)))
            | ParallelActions(acts) -> yield! acts |> Seq.collect collectOnOffActions
        } 
#endif
    /// PAtion에 소속된 Action Seq을 반환
    let rec collectActions action =
        seq {
            match action with
            | OnOffAction(_) | PLCAction(_) ->
                yield action
            | ParallelActions(acts) ->
                yield! acts |> Seq.collect collectActions
        }

    let getActionTags (act:PAction Option) =
        match act with
          | Some(expr) -> collectTagsFromAction expr
          | None -> empty

    /// PAction -> OnOff option
    let a2nf = getOnOffAction
    /// PAction -> PAction list (=parallel actions) option
    let a2pa = getParallelActions
    /// PAction -> PLCAction (= cmd, param) option
    let a2plc = getPLCAction

module AddressM =
    let getDevice x =
        match x with
        | I(_) -> "I"
        | Q(_) -> "O"
        | M(_) -> "M"
    let toString (addrOpt:Address option) =
        addrOpt
        |> Option.map toString
        |> Option.defaultValue null
    let tryParse (addr:string) =
        if addr.isNullOrEmpty()
        then None
        else Some(Address.FromString(addr))

module PLCTagM =
    let distinctByTag (tags:PLCTag seq) =
        tags |> Seq.distinctBy(fun t -> t.Tag)
    ()



[<Extension>] // type OnOffExt =
type OnOffExt =
    [<Extension>] static member ToExpression(onOff) = OnOffM.toExpression onOff
    [<Extension>] static member GetTurnOn(onOff) = OnOffM.getTurnOn onOff
    [<Extension>] static member GetTurnOff(onOff) = OnOffM.getTurnOff onOff
    [<Extension>] static member GetTag(onOff) = OnOffM.getTag onOff
    //[<Extension>] static member FromString(str) = OnOffM.fromString str


[<Extension>] // type PActionExt =
type PActionExt =
    /// PAction 이 OnOffAction 인 경우, OnOffAction 을 반환하고, 그렇지 않으면 None 을 반환
    [<Extension>] static member GetOnOffAction(pAction) = PActionM.getOnOffAction pAction
    /// PAction 이 Parallel Actions 인 경우, ParallelActions 을 반환하고, 그렇지 않으면 None 을 반환
    [<Extension>] static member GetParallelActions(pAction) = PActionM.getParallelActions pAction
    [<Extension>] static member GetPLCAction(pAction) = PActionM.getPLCAction pAction
    [<Extension>] static member OnOffPActionToCondition(pAction) = PActionM.onOffPActionToCondition pAction
    [<Extension>] static member PActionToCondition(pAction) = PActionM.PActionToCondition pAction

    [<Extension>] static member CollectTagsFromAction(pAction) = PActionM.collectTagsFromAction pAction
    //[<Extension>] static member CollectOnOffTagsFromAction(pAction) = PActionM.collectOnOffTagsFromAction pAction
    //[<Extension>] static member CollectOnOffActions(pAction) = PActionM.collectOnOffActions pAction
    //[<Extension>] static member CollectActions(pAction) = PActionM.collectActions pAction
    //[<Extension>] static member GetActionTags(pAction) = PActionM.getActionTags pAction


[<Extension>] // type AddressExt =
type AddressExt =
    [<Extension>] static member GetDevice(address) = AddressM.getDevice address



