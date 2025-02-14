open System
let dispose (x:#IDisposable) = if x <> null then x.Dispose()



let xs =
    seq {
        for i in 1..10 do
            printfn $"Generating {i}"
            yield i
    }

let xsc = xs |> Seq.cache

// fsi 에서 xs 를 평가... 몇 회 반복해도 항상 Generating 메시지 출력
// xsc 는 한번만 출력. 이후는 cache 버젼 사용
// xsc 를 IDisposable 형태의 dispose 수행하면 내부 cache 삭제됨.


let xscd = xsc :?> IDisposable

