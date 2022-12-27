namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices

open Engine.Core
open Engine.Common.FS

[<AutoOpen>]
module CoreExtensionsForCodeGenCPU =
    //let private tagsToArguments (xs:TagBase<'T> seq) : Expression<'T> list =
    //    xs.Select(tag2expr) |> List.ofSeq
    let private tags2LogicalAndOrExpr (fLogical: IExpression list -> Expression<bool>) (FList(ts:Tag<bool> list)) : Expression<bool> =
        match ts with
        | [] -> failwith "tags2AndExpr: Empty list"
        | t :: [] -> tag2expr t
        | _ -> ts.Select(tag2expr) 
                |> List.ofSeq 
                |> List.cast<IExpression>
                |> fLogical

    /// Tag<'T> (들)로부터 AND Expression<'T> 생성
    let tags2AndExpr = tags2LogicalAndOrExpr fLogicalAnd
    /// Tag<'T> (들)로부터 OR  Expression<'T> 생성
    let tags2OrExpr  = tags2LogicalAndOrExpr fLogicalOr

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
        [<Extension>] static member ToAnd (xs:DsBit seq)        = xs.Cast<Tag<bool>>() |> tags2AndExpr
        [<Extension>] static member ToAnd (xs:PlcTag<bool> seq) = xs.Cast<Tag<bool>>() |> tags2AndExpr
        [<Extension>] static member ToAnd (xs:Tag<bool> seq)    = xs |> tags2AndExpr
        [<Extension>] static member ToOr  (xs:DsBit seq)        = xs.Cast<Tag<bool>>() |> tags2OrExpr
        [<Extension>] static member ToOr  (xs:PlcTag<bool> seq) = xs.Cast<Tag<bool>>() |> tags2OrExpr
        [<Extension>] static member ToOr  (xs:Tag<bool> seq)    = xs |> tags2OrExpr
        
        ///Create None Relay Coil Statement
        [<Extension>]
        static member GetRung (coil:TagBase<bool>, sets:Expression<bool> option, rsts:Expression<bool> option) = 
            match sets, rsts with
            | Some(s), Some(r) -> coil <== (s <&&> (!! r))
            | Some(s), None    -> coil <== s
            | None   , Some(r) -> coil <== !! r
            | None   , None    -> failwith "Rung: Empty expresstion"
                     
        ///Create Relay Coil Statement
        [<Extension>]
        static member GetRelay (coil:TagBase<bool>, sets:Expression<bool>, rsts:Expression<bool>) = 
                      coil <== (sets <||> tag2expr coil <&&> (!! rsts))
        