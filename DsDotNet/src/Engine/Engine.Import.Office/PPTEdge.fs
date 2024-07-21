namespace Engine.Import.Office

open DocumentFormat.OpenXml.Packaging
open System
open System.Linq
open DocumentFormat.OpenXml
open Engine.Core
open System.Collections.Generic
open Microsoft.FSharp.Core
open Dual.Common.Core.FS
open System.Text.RegularExpressions

[<AutoOpen>]
module PPTEdgeModule =

    type pptEdge(conn: Presentation.ConnectionShape, iEdge: UInt32Value, iPage: int, sNode: pptNode, eNode: pptNode) =

        let (causal:ModelingEdgeType), (reverse:bool) = GetCausal(conn, iPage, sNode.Name, eNode.Name)

        member x.PageNum = iPage
        member x.ConnectionShape = conn
        member x.Id = iEdge
        member x.IsInterfaceEdge: bool = x.StartNode.NodeType.IsIF || x.EndNode.NodeType.IsIF
        member x.StartNode: pptNode = if (reverse) then eNode else sNode
        member x.EndNode: pptNode = if (reverse) then sNode else eNode
        member x.ParentId = 0 //reserve

        member val Name = conn.EdgeName()
        member val Key = Objkey(iPage, iEdge)

        member x.Text =
            let sName =
                match sNode.Alias with
                | Some a -> a.Name
                | None -> sNode.Name

            let eName =
                match eNode.Alias with
                | Some a -> a.Name
                | None -> eNode.Name

            if (reverse) then
                $"{iPage};{eName}{causal.ToText()}{sName}"
            else
                $"{iPage};{sName}{causal.ToText()}{eName}"

        member val Causal: ModelingEdgeType = causal

