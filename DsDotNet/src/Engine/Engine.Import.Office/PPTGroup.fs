namespace Engine.Import.Office

open System
open System.Linq
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Drawing
open System.Collections.Generic
open Microsoft.FSharp.Core
open Dual.Common.Core.FS

[<AutoOpen>]
module PPTGroupModule =
    
    type pptRealGroup(iPage: int, nodes: pptNode seq) =
        let mutable parent: pptNode option = None
        let childSet = HashSet<pptNode>()

        let nodeNames (nodes: pptNode seq) =
            nodes.Select(fun s -> s.Name).JoinWith(", ")

        do
            let reals = nodes.Where(fun w -> w.NodeType.IsReal)
            let calls = nodes.Where(fun w -> w.NodeType.IsCall)

            if (reals.Count() > 1) then
                Office.ErrorPPT(
                    Group,
                    ErrID._23,
                    $"Reals:{reals |> nodeNames}",
                    iPage,
                    Office.ShapeID(reals.First().Shape)
                )

            parent <-
                if (reals.Any() |> not) then
                    None
                else
                    Some(reals |> Seq.head)

            let children = nodes |> Seq.filter (fun node -> node.NodeType = REAL |> not)

            children |> Seq.iter (fun child -> childSet.Add(child) |> ignore)

        member x.RealKey = sprintf "%d;%s" iPage (parent.Value.Name)
        member x.PageNum = iPage
        member x.Parent: pptNode option = parent
        member x.Children = childSet
