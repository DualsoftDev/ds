type Content = ArraySegment<byte> list

type ContentBuilder() =
    member _.Run(c: Content) =
        let crlf = "\r\n"B
        [|for part in List.rev c do
            yield! part.Array[part.Offset..(part.Count+part.Offset-1)]
            yield! crlf |]

    member _.Yield(_) = []

    [<CustomOperation("body")>]
    member _.Body(c: Content, segment: ArraySegment<byte>) =
        segment::c

    [<CustomOperation("body")>]
    member _.Body(c: Content, bytes: byte[]) =
        ArraySegment<byte>(bytes, 0, bytes.Length)::c

    [<CustomOperation("body")>]
    member _.Body(c: Content, bytes: byte[], offset, count) =
        ArraySegment<byte>(bytes, offset, count)::c

    [<CustomOperation("body")>]
    member _.Body(c: Content, content: System.IO.Stream) =
        let mem = new System.IO.MemoryStream()
        content.CopyTo(mem)
        let bytes = mem.ToArray()
        ArraySegment<byte>(bytes, 0, bytes.Length)::c

    [<CustomOperation("body")>]
    member _.Body(c: Content, [<ParamArray>] contents: string[]) =
        List.rev [for c in contents -> let b = Text.Encoding.ASCII.GetBytes c in ArraySegment<_>(b,0,b.Length)] @ c