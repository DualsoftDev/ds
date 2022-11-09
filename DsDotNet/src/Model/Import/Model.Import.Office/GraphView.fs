// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System
open Microsoft.Office.Interop.Excel
open System.Drawing
open System.Reflection
open Engine.Common.FS
open Engine.Core
open System.Collections.Generic
open System.Runtime.CompilerServices

[<AutoOpen>]
module rec ViewModule = 

   
    type ViewNode(name:string, coreVertex:Vertex option, btnType:BtnType option) = 
        let btnType:BtnType option = btnType
        let coreVertex:Vertex option = None

        new () = ViewNode("",  None, None)
        new (name) = ViewNode(name,  None, None)
        new (coreVertex:Vertex) = ViewNode(coreVertex.Name, Some(coreVertex),  None)
        new (name, btnType:BtnType) = ViewNode(name, None, Some(btnType))

        member val Edges = HashSet<ModelingEdgeInfo<ViewNode>>()
        member val Singles = HashSet<ViewNode>()
        member val NodeType = NodeType.MY
        member val Flow:Flow option = None with get, set
        member val Page = 0 with get, set

        member x.CoreVertex =  coreVertex
        member x.BtnType =  btnType
        member x.IsChildExist =  x.Edges.Count>0 || x.Singles.Count>0
        member x.UIKey =  $"{name};{x.GetHashCode()}"


[<Extension>]
type ViewModuleExt = 
    [<Extension>] static member CreateDummy(dummy:pptDummy) =  ViewNode()
                    
    
