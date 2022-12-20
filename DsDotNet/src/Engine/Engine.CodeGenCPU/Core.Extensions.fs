namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices

open Engine.Core
open Engine.Common.FS

[<AutoOpen>]
module CoreExtensionsForCodeGenCPU =
    let private tagsToArguments (xs:TagBase<'T> seq) : Expression<'T> list =
        xs.Select(tag2expr) |> List.ofSeq

    let private tags2LogicalAndOrExpr (fLogical: IExpression list -> Expression<bool>) (FList(ts:TagBase<bool> list)) : Expression<bool> =
        match ts with
        | [] -> failwith "tags2AndExpr: Empty list"
        | t :: [] -> tag2expr t
        | _ -> ts |> tagsToArguments |> List.cast<IExpression> |> fLogical

    /// Tag<'T> (들)로부터 AND Expression<'T> 생성
    let tags2AndExpr = tags2LogicalAndOrExpr fLogicalAnd
    /// Tag<'T> (들)로부터 AND Expression<'T> 생성
    let tags2OrExpr = tags2LogicalAndOrExpr fLogicalOr

    /// boolean AND operator
    let (<&&>) (left: Expression<bool>) (right: Expression<bool>) = fLogicalAnd [ left; right ]
    /// boolean OR operator
    let (<||>) (left: Expression<bool>) (right: Expression<bool>) = fLogicalOr [ left; right ]
    /// boolean NOT operator
    let (!!)   (exp: Expression<bool>) = fLogicalNot [exp]
    /// Assign statement
    let (<==)  (storage: IStorage) (exp: IExpression) = DuAssign(exp, storage)

    [<Extension>]
    type FuncExt =
        [<Extension>] static member ToTags (xs:#TagBase<'T> seq)  = xs.Cast<TagBase<_>>()
        [<Extension>] static member ToExpr (x:TagBase<bool>)      = tag2expr x
        [<Extension>] static member GetAnd (xs:TagBase<bool> seq) = tags2AndExpr xs
        [<Extension>] static member GetOr  (xs:TagBase<bool> seq) = tags2OrExpr xs

    [<RequireQualifiedAccess>]
    module FuncExt =
        let GetRelayRung(set:Expression<bool>, rst:Expression<bool>, relay:TagBase<bool>): Statement =
            relay <== ((set <||> relay.ToExpr()) <&&> (rst))

        //[sets and]--|----- ! [rsts or] ----- (relay)
        //|relay|-----|
        let GetRelayExpr(sets:TagBase<bool> seq, rsts:TagBase<bool> seq, relay:TagBase<bool>): Expression<bool> =
            (sets.GetAnd() <||> relay.ToExpr()) <&&> (!! rsts.GetOr())

        //[sets and]--|----- ! [rsts or] ----- (coil)
        let GetNoRelayExpr(sets:TagBase<bool> seq, rsts:TagBase<bool> seq): Expression<bool> =
            sets.GetAnd() <&&> (!! rsts.GetOr())

        //[sets and]--|-----  [rsts and] ----- (relay)
        //|relay|-----|
        let GetRelayExprReverseReset(sets:TagBase<bool> seq, reversedResets:TagBase<bool> seq, relay:TagBase<bool>): Expression<bool> =
            (sets.GetAnd() <||> relay.ToExpr()) <&&> (reversedResets.GetOr())


