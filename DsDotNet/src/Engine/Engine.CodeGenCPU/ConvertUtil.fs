namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open System
open Engine.Common.FS

[<AutoOpen>]
module ConvertUtil =

    let getVM(v:Vertex) = v.VertexManager :?> VertexManager
    let rec getCoinTags(v:Vertex, isInTag:bool) : Tag<bool> seq =
            match v with
            | :? Call as c ->
                [ for j in c.CallTarget.JobDefs do
                    let typ = if isInTag then "I" else "O"
                    PlcTag( $"{j.ApiName}_{typ}", "", false) :> Tag<bool>
                ]
            | :? Alias as a ->
                match a.TargetWrapper with
                | DuAliasTargetReal ar    -> getCoinTags( ar, isInTag)
                | DuAliasTargetCall ac    -> getCoinTags( ac, isInTag)
                | DuAliasTargetRealEx ao  -> getCoinTags( ao, isInTag)
            | _ -> failwith "Error"

    let getTxRxTags(v:Vertex, isTx:bool) : Tag<bool> seq =
        match v with
        | :? Call as c ->
            c.CallTarget.JobDefs
                .SelectMany(fun j->
                    if isTx then
                        j.ApiItem.TXs.Select(getVM).Select(fun f->f.ST.Expr)
                    else
                        j.ApiItem.RXs.Select(getVM).Select(fun f->f.ET.Expr)
                )
                .Cast<Tag<bool>>()
        | :? Alias as a ->
            match a.TargetWrapper with
            | DuAliasTargetReal ar    -> getCoinTags(ar, isTx)
            | DuAliasTargetCall ac    -> getCoinTags(ac, isTx)
            | DuAliasTargetRealEx ao  -> getCoinTags(ao, isTx)
        | _ -> failwith "Error"
    


    [<AutoOpen>]
    type SRE = 
    |Start
    |Reset
    |End
    
    [<Flags>]
    [<AutoOpen>]
    type ConvertType = 
    |RealPure            = 0b00000001  
    |RealExPure          = 0b00000010  
    |CallPure            = 0b00000100  
    |AliasPure           = 0b00001000  
    |AliasForCall        = 0b00100000  
    |AliasForReal        = 0b01000000  
    |AliasForRealEx      = 0b10000000  
    |VertexAll           = 0b11111111  
    //RC      //Real, Call as RC
    //RCA     //Real, Call, Alias(Call) as RCA
    //RRA     //Real, RealExF, Alias(Real) as RRA
    //CA      //Call, Alias(Call) as CA 
    //V       //Real, RealExF, Call, Alias as V

    let IsSpec (v:Vertex) (vaild:ConvertType) = 
        let isVaildVertex =
            match v with
            | :? Real   -> vaild.HasFlag(RealPure)
            | :? RealEx -> vaild.HasFlag(RealExPure) 
            | :? Call   -> vaild.HasFlag(CallPure)
            | :? Alias as a  -> 
                match a.TargetWrapper with
                | DuAliasTargetReal ar   -> vaild.HasFlag(AliasForReal)
                | DuAliasTargetCall ac   -> vaild.HasFlag(AliasForCall)
                | DuAliasTargetRealEx ao -> vaild.HasFlag(AliasForRealEx)
            |_ -> failwith "Error"

        isVaildVertex
        //if not <| isVaildVertex 
        //then failwith $"{v.Name} can't applies to [{vaild}] case"

    let private tags2LogicalAndOrExpr (fLogical: IExpression list -> Expression<bool>) (FList(ts:Tag<bool> list)) : Expression<bool> =
        match ts with
        | [] -> failwith "tags2AndExpr: Empty list"
        | t :: [] -> tag2expr t
        | _ -> ts.Select(tag2expr) 
                |> List.ofSeq 
                |> List.cast<IExpression>
                |> fLogical

    
    /// boolean AND operator
    let (<&&>) (left: Expression<bool>) (right: Expression<bool>) = fLogicalAnd [ left; right ]
    /// boolean OR operator
    let (<||>) (left: Expression<bool>) (right: Expression<bool>) = fLogicalOr [ left; right ]
    /// boolean NOT operator
    let (!!)   (exp: Expression<bool>) = fLogicalNot [exp]
    /// Assign statement
    let (<==)  (storage: IStorage) (exp: IExpression) = DuAssign(exp, storage)
    /// Assign Puls statement  : Pulse Coil 타입 필요
    let (<=!)  (storage: IStorage) (exp: IExpression) = DuAssign(exp, storage)


    /// Create None Relay Coil Statement
    let (--|) (sets: Expression<bool>, rsts: Expression<bool>) (coil: TagBase<bool>, comment:string) = 
        coil <== (sets <&&> (!! rsts)) |> withExpressionComment comment
    /// Create Relay Coil Statement                                                      
    let (==|) (sets: Expression<bool>, rsts: Expression<bool>) (coil: TagBase<bool> , comment:string) =
        coil <== (sets <||> tag2expr coil <&&> (!! rsts)) |> withExpressionComment comment
     /// Create None Relay Pulse Coil Statement
    let (--^) (sets: Expression<bool>, rsts: Expression<bool>) (coil: TagBase<bool>, comment:string) = 
        coil <=! (sets <&&> (!! rsts)) |> withExpressionComment comment

    /// Tag<'T> (들)로부터 AND Expression<'T> 생성
    let toAnd = tags2LogicalAndOrExpr fLogicalAnd
    /// Tag<'T> (들)로부터 OR  Expression<'T> 생성
    let toOr  = tags2LogicalAndOrExpr fLogicalOr


    [<Extension>]
    type ConvertUtilExt =
        [<Extension>] static member ToAnd (xs:DsBit seq)        = xs.Cast<Tag<bool>>() |> toAnd
        [<Extension>] static member ToAnd (xs:PlcTag<bool> seq) = xs.Cast<Tag<bool>>() |> toAnd
        [<Extension>] static member ToAnd (xs:Tag<bool> seq)    = xs |> toAnd
        [<Extension>] static member ToOr  (xs:DsBit seq)        = xs.Cast<Tag<bool>>() |> toOr
        [<Extension>] static member ToOr  (xs:PlcTag<bool> seq) = xs.Cast<Tag<bool>>() |> toOr
        [<Extension>] static member ToOr  (xs:Tag<bool> seq)    = xs |> toOr
        [<Extension>]
        static member FindEdgeSources(graph:DsGraph, target:Vertex, edgeType:ModelingEdgeType): Vertex seq =
            let edges = graph.GetIncomingEdges(target)
            let foundEdges =
                match edgeType with
                | StartPush -> edges.OfNotResetEdge().Where(fun e -> e.EdgeType.HasFlag(EdgeType.Strong))
                | StartEdge -> edges.OfNotResetEdge().Where(fun e -> not <| e.EdgeType.HasFlag(EdgeType.Strong))
                | ResetEdge -> edges.OfWeakResetEdge()
                | ResetPush -> edges.OfStrongResetEdge()
                | ( StartReset | InterlockWeak | Interlock )
                    -> failwith $"Do not use {edgeType} Error"

            foundEdges.Select(fun e->e.Source)
    