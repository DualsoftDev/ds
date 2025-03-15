namespace Dual.Common.FS.LSIS.Graph

module MsAgl =
    (*
        MSAGL(Microsoft Automatic Graph Layout) / GLEE interface
        Resources
        - MsAgl.Sample.cs sample
        - http://www.fssnip.net/1d/title/Creating-objects-with-events : F# -> C# event passing 예제
        - YC.QuickGraph project
    *)

    open QuickGraph.Glee
    open QuickGraph
    open System



    let toGleeGraphSample (g:IEdgeListGraph<'v, 'e>) =
        let populatorNodeAdded = new GleeVertexNodeEventHandler<'v> (fun sender args -> ())
        let populatorEdgeAdded = new GleeEdgeEventHandler<'v, 'e> (fun sender args -> ())
        g.ToGleeGraph(populatorNodeAdded, populatorEdgeAdded)


    let toGleeGraph (g:IEdgeListGraph<'v, 'e>) populatorNodeAdded populatorEdgeAdded =
        g.ToGleeGraph(populatorNodeAdded, populatorEdgeAdded)


open MsAgl
open System.Runtime.CompilerServices

[<Extension>] // type GleeExt =
type GleeExt =
    [<Extension>]
    static member GetGleeGraph(g, vertexFormatter, edgeFormatter) =
        toGleeGraph g vertexFormatter edgeFormatter



