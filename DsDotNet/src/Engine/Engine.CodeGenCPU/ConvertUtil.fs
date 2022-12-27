namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open System

[<AutoOpen>]
module ConvertUtil =
    
    [<Flags>]
    type ConvertType = 
    |CvRealPure            = 0b00000001  
    |CvRealEx              = 0b00000010  
    |CvCall                = 0b00000100  
    |CvAlias               = 0b00001000  
    |CvAliasForCall        = 0b00100000  
    |CvAliasForReal        = 0b01000000  
    |CvAliasForRealEx      = 0b10000000  
    |CvV                   = 0b11111111  

    //RC      //Real, Call as RC
    //RCA     //Real, Call, Alias(Call) as RCA
    //RRA     //Real, RealExF, Alias(Real) as RRA
    //CA      //Call, Alias(Call) as CA 
    //V       //Real, RealExF, Call, Alias as V

    let ConvertTypeCheck (v:Vertex) (vaild:ConvertType) = 
        let isVaildVertex =
            match v with
            | :? Real   -> vaild.HasFlag(ConvertType.CvRealPure)
            | :? RealEx -> vaild.HasFlag(ConvertType.CvRealEx) 
            | :? Call   -> vaild.HasFlag(ConvertType.CvCall)
            | :? Alias as a  -> 
                match a.TargetWrapper with
                | DuAliasTargetReal ar   -> vaild.HasFlag(ConvertType.CvAliasForReal)
                | DuAliasTargetCall ac   -> vaild.HasFlag(ConvertType.CvAliasForCall)
                | DuAliasTargetRealEx ao -> vaild.HasFlag(ConvertType.CvAliasForRealEx)
            |_ -> failwith "Error"

        if not <| isVaildVertex 
        then failwith $"{v.Name} can't applies to [{vaild}] case"
       
    [<Extension>]
    type ConvertUtilExt =

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


    // vertex 를 coin 입장에서 봤을 때의 extension methods
    type Vertex with
        member coin.GetCoinTags(memory:VertexMemoryManager, isInTag:bool) : Tag<bool> seq =
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


        member coin.GetTxRxTags(isTx:bool, memory:VertexMemoryManager) : Tag<bool> seq =
            let getVertexManager(v:Vertex) = v.VertexMemoryManager :?> VertexMemoryManager

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
