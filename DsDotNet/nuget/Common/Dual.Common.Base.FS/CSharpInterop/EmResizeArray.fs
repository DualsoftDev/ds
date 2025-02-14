namespace Dual.Common.Base.FS

open System.Runtime.CompilerServices

type EmResizeArray =
    [<Extension>]
    static member Resize<'T when 'T : (new: unit -> 'T)> (xs: ResizeArray<'T>, size: int) =
        let currentLength = xs.Count
        if currentLength < size then
            // n보다 작으면 기본 생성자로 채워서 늘림
            for _ in currentLength .. size - 1 do
                xs.Add(new 'T())
        elif currentLength > size then
            // n보다 크면 뒤에서부터 요소 제거
            for _ in 1 .. currentLength - size do
                xs.RemoveAt(xs.Count - 1)


