namespace Engine.CodeGenCPU

open System.Diagnostics
open System.Linq
open System.Text.RegularExpressions
open Engine.Core

[<AutoOpen>]
module VertexManagerExtension =
    let startTags (xs:VertexManager seq) = xs.Select(fun s->s.StartTag)  
    let resetTags (xs:VertexManager seq) = xs.Select(fun s->s.ResetTag)  
    let endTags   (xs:VertexManager seq) = xs.Select(fun s->s.EndTag)

    type VertexManager with
        ///Real 자신을 공용으로 사용하는 Vertex들  
        member v.GetSharedReal() : VertexManager seq =
                (v.Vertex :?> Real)
                    .GetVertexSharedReal()
                    .Select(fun v-> v.VertexManager)
                    .Cast<VertexManager>()

        ///Call 자신을 공용으로 사용하는 Vertex들  
        member v.GetSharedCall() : VertexManager seq =
                (v.Vertex :?> Call)
                    .GetVertexSharedCall()
                    .Select(fun v-> v.VertexManager)
                    .Cast<VertexManager>()
