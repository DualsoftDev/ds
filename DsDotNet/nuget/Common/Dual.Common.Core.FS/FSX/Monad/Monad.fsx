(*
- task
- seq

- option
- async
- disposable
- result
- async{Seq,Option,}
- chooseSeq
- guard
- state
- reader
- content
- custom logger
- string buidler : https://www.fssnip.net/7WR/title/Computation-expression-over-StringBuilder
 *)



open System

module CustomLogMonad =
    type LoggerBuilder() =
        member _.Bind(value, func) =
            printfn "========= Value: %A" value
            func value

        member _.Return(x) =
            printfn "========= Return: %A" x
            x

    let logger = LoggerBuilder()

    let computation = logger {
        let! x = 10
        let! y = 20
        let! z = 10 * 20
        return x + y
    }

    printfn "Computation result: %d" computation

// State Monad는 상태를 유지하며 계산을 수행하는 데 사용됩니다.
module StateMonad =
    //type State<'s, 'a> = State of ('s -> 'a * 's)

    //type StateBuilder() =
    //    member _.Bind(State stateFunc, f) =
    //        State (fun s ->
    //            let (a, newState) = stateFunc s
    //            let (State newStateFunc) = f a
    //            newStateFunc newState)

    //    member _.Return(x) =
    //        State (fun s -> (x, s))

    //    member _.ReturnFrom(m: State<'s, 'a>) = m

    //let state = StateBuilder()

    let incrementState x =
        State (fun s -> ((), s + x))

    let computation = state {
        do! incrementState 1
        do! incrementState 2
        do! incrementState 3
        return ()
    }

    let (State runState) = computation
    let initialState = 0
    let (_, finalState) = runState initialState

    printfn "Final state: %d" finalState    // Final state: 6


// Reader Monad는 읽기 전용 환경을 전달하며 계산을 수행하는 데 사용됩니다.
module ReaderMonad =
    type Reader<'env, 'a> = Reader of ('env -> 'a)

    type ReaderBuilder() =
        member _.Bind(Reader readerFunc, f) =
            Reader (fun env ->
                let a = readerFunc env
                let (Reader newReaderFunc) = f a
                newReaderFunc env)

        member _.Return(x) =
            Reader (fun _ -> x)

        member _.ReturnFrom(m: Reader<'env, 'a>) = m

    let reader = ReaderBuilder()

    let computation = reader {
        let! x = Reader (fun env -> env * 2)    // 10 * 2 = 20
        let! y = Reader (fun env -> env + 3)    // 10 + 3 = 13
        return x + y
    }

    let (Reader runReader) = computation
    let result = runReader 10
    printfn "Result: %d" result     // Result: 33

module ResultMonad =
    let computation = result {
        let! x = Ok 40
        let! y = Ok 4
        if y = 0 then return! Error "Division by zero"
        return x / y
    }

    match computation with
    | Ok value -> printfn "Success: %d" value       // Success: 10
    | Error msg -> printfn "Error: %s" msg




query {
    for digit in [1..10] do
        where (digit < 5)
        select digit
}

(Some 1) |> bind (fun x -> Some x)
Some 1 >>= fun x -> Some x

(>>=)

//module ValidationResultMonad =
//    open System

//    type ValidationResult<'a> =
//        | Valid of 'a
//        | Invalid of string list

//    type ValidationBuilder() =
//        member _.Bind(m: ValidationResult<'a>, f: 'a -> ValidationResult<'b>) =
//            match m with
//            | Valid x -> f x
//            | Invalid errs -> Invalid errs

//        member _.Return(x: 'a) = Valid x

//        member _.ReturnFrom(m: ValidationResult<'a>) = m

//        member _.Combine(v1: ValidationResult<'a>, v2: ValidationResult<'b>) =
//            match v1, v2 with
//            | Valid _, Valid _ -> v2
//            | Invalid errs1, Invalid errs2 -> Invalid (errs1 @ errs2)
//            | Invalid errs, _ | _, Invalid errs -> Invalid errs

//    let validation = ValidationBuilder()

//    let isNotEmpty (str:string) =
//        if String.IsNullOrWhiteSpace(str) then Invalid ["String is empty"]
//        else Valid str

//    let isLongEnough (str:string) =
//        if str.Length < 5 then Invalid ["String is too short"]
//        else Valid str

//    let computation = validation {
//        do! isNotEmpty "Test"
//        do! isLongEnough "Test"
//        return "Validation passed"
//    }

//    match computation with
//    | Valid msg -> printfn "%s" msg
//    | Invalid errs -> printfn "Errors: %A" errs


(* Custom operator: 아래 예의 "body" 등을 모나드에서 사용가능.  F#6.0 이후 지원 *)
// https://learn.microsoft.com/en-us/dotnet/fsharp/whats-new/fsharp-6
module ContentMonad =
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

    let content = ContentBuilder()


    let testMe() =
        let mem = new System.IO.MemoryStream("Stream"B)
        let ceResult =
            content {
                body "Name"
                body (ArraySegment<_>("Email"B, 0, 5))
                body "Password"B 2 4
                body "BYTES"B
                body mem
                body "Description" "of" "content"
            }
        (*
            > ceResult;;
            val it: byte array =
              [|78uy; 97uy; 109uy; 101uy; 13uy; 10uy; 69uy; 109uy; 97uy; 105uy; 108uy;
                13uy; 10uy; 115uy; 115uy; 119uy; 111uy; 13uy; 10uy; 66uy; 89uy; 84uy; 69uy;
                83uy; 13uy; 10uy; 83uy; 116uy; 114uy; 101uy; 97uy; 109uy; 13uy; 10uy; 68uy;
                101uy; 115uy; 99uy; 114uy; 105uy; 112uy; 116uy; 105uy; 111uy; 110uy; 13uy;
                10uy; 111uy; 102uy; 13uy; 10uy; 99uy; 111uy; 110uy; 116uy; 101uy; 110uy;
                116uy; 13uy; 10uy|]
        *)


// https://github.com/fsprojects/FSharp.Control.AsyncSeq/blob/main/tests/FSharp.Control.AsyncSeq.Tests/AsyncSeqTests.fs
#r "nuget: FSharp.Control.AsyncSeq"
open FSharp.Control
let oneThenTwo =
    asyncSeq {
        yield 1
        do! Async.Sleep 1000 // non-blocking sleep
        yield 2
    }
let xxx = oneThenTwo |> AsyncSeq.toListAsync |> Async.RunSynchronously      // [1; 2]


()


(* IO monad : https://www.youtube.com/watch?v=h00DRlHewrM
https://gist.github.com/mjgpy3/4516d3f14d07be867a2ce0cb03803012
 *)
type Effects = {
    ReadLine: unit -> string
    PrintLine: string -> unit
}

type Io<'a> = Effects -> 'a
let (>>=) (io: Io<'a>) (f: 'a -> Io<'b>) : Io<'b> =
    fun eff -> f (io eff) eff

let readLine () eff = eff.ReadLine ()
let printLine text eff = eff.PrintLine text















// State 타입 정의
type State = { X: int; Y: int; Messages: string list }

// Computation Expression 빌더 정의
type StateBuilder() =
    member _.Bind(m, f) =
        fun s ->
            let (r, s') = m s
            f r s'

    member _.Return(x) = fun s -> (x, s)
    member _.ReturnFrom(m) = m
    member _.Zero() = fun s -> ((), s)

// Computation Expression 빌더 인스턴스
let state = StateBuilder()

// 상태를 업데이트하는 함수들
let updateX dx =
    fun s -> ((), { s with X = s.X + dx })

let updateY dy =
    fun s -> ((), { s with Y = s.Y + dy })

let addMessage msg =
    fun s -> ((), { s with Messages = msg :: s.Messages })

// 재귀적으로 상태를 업데이트하는 함수
let rec updateStateRecursively n =
    state {
        if n > 0 then
            do! updateX 1
            do! updateY 2
            do! addMessage (sprintf "Step %d" n)
            return! updateStateRecursively (n - 1)
        else
            return ()
    }

// 실행 예제
let initialState = { X = 0; Y = 0; Messages = [] }

let finalState = updateStateRecursively 5 initialState |> snd

printfn "Final State: X = %d, Y = %d, Messages = %A" finalState.X finalState.Y finalState.Messages










module mod1 =

    // State 타입 정의
    type State = { X: int; Y: int; W: int; H: int; Messages: string list }

    // Computation Expression 빌더 정의
    type StateBuilder() =
        member _.Bind(m, f) =
            fun s ->
                let (r, s') = m s
                f r s'

        member _.Return(x) = fun s -> (x, s)
        member _.ReturnFrom(m) = m
        member _.Zero() = fun s -> ((), s)

        // 초기 상태로 실행을 시작
        member _.Run(f) = f

    // 상태를 업데이트하는 함수들
    let updateW dw =
        fun s -> ((), { s with W = s.W + dw })

    let updateH dh =
        fun s -> ((), { s with H = s.H + dh })

    let getState =
        fun s -> (s, s)

    // Computation Expression 빌더 인스턴스 생성 함수
    let state = StateBuilder()

    // 재귀적으로 상태를 업데이트하는 함수
    let rec updateStateRecursively n =
        state {
            if n > 0 then
                // W, H 값을 업데이트
                do! updateW 10
                do! updateH 5

                // 현재 상태를 가져와 출력
                let! currentState = getState
                printfn "Updated State: X=%d, Y=%d, W=%d, H=%d" currentState.X currentState.Y currentState.W currentState.H

                // 재귀 호출
                return! updateStateRecursively (n - 1)
            else
                return ()
        }

    // 실행 예제
    let runExample x y n =
        // 초기 상태 설정
        let initialState = { X = x; Y = y; W = 0; H = 0; Messages = [] }

        // 상태 업데이트 실행
        let finalState = updateStateRecursively n initialState |> snd

        // 최종 상태 출력
        printfn "Final State: X = %d, Y = %d, W = %d, H = %d, Messages = %A" finalState.X finalState.Y finalState.W finalState.H finalState.Messages

    // X = 10, Y = 20 초기값 설정 후 실행
    runExample 10 20 5











module mod2 =


    // State 타입 정의
    type State = { X: int; Y: int; W: int; H: int; Messages: string list }

    // Computation Expression 빌더 정의
    type StateBuilder() =
        member _.Bind(m, f) =
            fun s ->
                let (r, s') = m s
                f r s'

        member _.Return(x) = fun s -> (x, s)
        member _.ReturnFrom(m) = m
        member _.Zero() = fun s -> ((), s)

        // 초기 상태로 실행을 시작
        member _.Run(f) = f

    // 상태를 업데이트하는 함수들
    let updateW dw =
        fun s -> ((), { s with W = s.W + dw })

    let updateH dh =
        fun s -> ((), { s with H = s.H + dh })

    let getState =
        fun s -> (s, s)

    // Computation Expression 빌더 인스턴스 생성 함수
    let state = StateBuilder()

    // 새로운 상태를 반영하는 함수
    let updateStateWithNewState newState =
        fun _ -> ((), state)

    // 재귀적으로 상태를 업데이트하는 함수
    let rec updateStateRecursively n =
        state {
            if n > 0 then
                // W, H 값을 업데이트
                do! updateW 10
                do! updateH 5

                // 현재 상태를 가져와 출력
                let! currentState = getState
                printfn "Updated State: X=%d, Y=%d, W=%d, H=%d" currentState.X currentState.Y currentState.W currentState.H

                // 내부에서 새 상태를 계산
                let! newState =
                    state {
                        // 새로운 상태에 대한 로직을 여기서 수행
                        do! updateW 5
                        do! updateH 2
                        return! getState
                    }

                // 새 상태를 현재 상태에 반영
                do! updateStateWithNewState newState

                // 재귀 호출
                return! updateStateRecursively (n - 1)
            else
                return ()
        }

    // 실행 예제
    let runExample x y n =
        // 초기 상태 설정
        let initialState = { X = x; Y = y; W = 0; H = 0; Messages = [] }

        // 상태 업데이트 실행
        let finalState = updateStateRecursively n initialState |> snd

        // 최종 상태 출력
        printfn "Final State: X = %d, Y = %d, W = %d, H = %d, Messages = %A" finalState.X finalState.Y finalState.W finalState.H finalState.Messages

    // X = 10, Y = 20 초기값 설정 후 실행
    runExample 10 20 5





module mod3 =
    // State 타입 정의
    type State = { X: int; Y: int; W: int; H: int; Messages: string list }

    // Computation Expression 빌더 정의
    type StateBuilder() =
        member _.Bind(m, f) =
            fun s ->
                let (r, s') = m s
                f r s'

        member _.Return(x) = fun s -> (x, s)
        member _.ReturnFrom(m) = m
        member _.Zero() = fun s -> ((), s)

        // 초기 상태로 실행을 시작
        member _.Run(f) = f

    let initXy (x, y) =
        fun s -> ((), { s with X = x; Y = y })
    // 상태를 업데이트하는 함수들
    let updateW dw =
        fun s -> ((), { s with W = s.W + dw })

    let updateH dh =
        fun s -> ((), { s with H = s.H + dh })

    let getState =
        fun s -> (s, s)

    // Computation Expression 빌더 인스턴스 생성 함수
    let state = StateBuilder()

    // 새로운 상태를 반영하는 함수
    let updateStateWithNewState state =
        fun old ->
            let newState = { X = old.X + state.X; Y = 0; W = 0; H = 0; Messages = [] }
            ((), state)

    // 재귀적으로 상태를 업데이트하는 함수
    let rec updateStateRecursively n =
        state {
            if n > 0 then
                // W, H 값을 업데이트
                do! updateW 10
                do! updateH 5

                // 현재 상태를 가져와 출력
                let! currentState = getState
                printfn "Updated State: X=%d, Y=%d, W=%d, H=%d" currentState.X currentState.Y currentState.W currentState.H

                // 내부에서 새 상태를 계산
                let! newState =
                    state {
                        do! initXy (1, 1)
                        // 새로운 상태에 대한 로직을 여기서 수행
                        do! updateW 5
                        do! updateH 2
                        return! getState
                    }

                // 새 상태를 현재 상태에 반영
                do! updateStateWithNewState newState

                // 재귀 호출
                return! updateStateRecursively (n - 1)
            else
                return ()
        }

    // 실행 예제
    let runExample x y n =
        // 초기 상태 설정
        let initialState = { X = x; Y = y; W = 0; H = 0; Messages = [] }

        // 상태 업데이트 실행
        let finalState = updateStateRecursively n initialState |> snd

        // 최종 상태 출력
        printfn "Final State: X = %d, Y = %d, W = %d, H = %d, Messages = %A" finalState.X finalState.Y finalState.W finalState.H finalState.Messages

    // X = 10, Y = 20 초기값 설정 후 실행
    runExample 10 20 5





module mod4 =
    // State 타입 정의
    type State = { X: int; Y: int; W: int; H: int; Messages: string list }

    // Computation Expression 빌더 정의
    type StateBuilder(sx, sy) =

        member _.Bind(m, f) =
            fun s ->
                let (r, s') = m s
                f r s'

        member _.Return(x) = fun s -> (x, s)
        member _.ReturnFrom(m) = m
        member _.Zero() = fun s -> ((), { s with X = sx; Y = sy })

        // 초기 상태로 실행을 시작
        member _.Run(f) = f

    let initXy (x, y) =
        fun s -> ((), { s with X = x; Y = y })
    // 상태를 업데이트하는 함수들
    let updateW dw =
        fun s -> ((), { s with W = s.W + dw })

    let updateH dh =
        fun s -> ((), { s with H = s.H + dh })

    let getState =
        fun s -> (s, s)

    // Computation Expression 빌더 인스턴스 생성 함수
    let state (x, y) = StateBuilder(x, y)

    // 새로운 상태를 반영하는 함수
    let updateStateWithNewState state =
        fun old ->
            let newState = { X = old.X + state.X; Y = 0; W = 0; H = 0; Messages = [] }
            ((), state)

    // 재귀적으로 상태를 업데이트하는 함수
    let rec updateStateRecursively n =
        state (100, 100) {
            if n > 0 then
                // W, H 값을 업데이트
                do! updateW 10
                do! updateH 5

                // 현재 상태를 가져와 출력
                let! currentState = getState
                printfn "Updated State: X=%d, Y=%d, W=%d, H=%d" currentState.X currentState.Y currentState.W currentState.H

                // 내부에서 새 상태를 계산
                let! newState =
                    state (200, 200) {
                        //do! initXy (1, 1)
                        // 새로운 상태에 대한 로직을 여기서 수행
                        do! updateW 5
                        do! updateH 2
                        return! getState
                    }

                // 새 상태를 현재 상태에 반영
                do! updateStateWithNewState newState

                // 재귀 호출
                return! updateStateRecursively (n - 1)
            else
                return ()
        }

    // 실행 예제
    let runExample x y n =
        // 초기 상태 설정
        let initialState = { X = x; Y = y; W = 0; H = 0; Messages = [] }

        // 상태 업데이트 실행
        let finalState = updateStateRecursively n initialState |> snd

        // 최종 상태 출력
        printfn "Final State: X = %d, Y = %d, W = %d, H = %d, Messages = %A" finalState.X finalState.Y finalState.W finalState.H finalState.Messages

    // X = 10, Y = 20 초기값 설정 후 실행
    runExample 10 20 5

    state (100, 100) { () }