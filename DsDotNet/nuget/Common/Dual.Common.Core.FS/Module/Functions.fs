namespace Dual.Common.Core.FS


open System.IO
open System.Diagnostics
open System.Collections.Generic
open System

[<AutoOpen>]
module Functions =
    /// logical not.  '!' 는 dereference 연산자로 사용되기 때문에 '!!' 을 사용
    ///
    /// - operator 재정의를 통해 pipe 없이 다음처럼 가능: not <| boolFunction(true) === !! boolFunction(true)
    ///
    /// - 불가능 : not boolFunction(true)  ==> "not <| boolFunction true" or not (boolFunction true)
    ///
    /// - 불가능 : !! boolFunction true    ==> "!! (boolFunction true)" or !! (boolFunction(true))
    let (!!) = not

    /// 삼항 연산자.  true ?= (1, 0) === if true then 1 else 0 === 1
    ///
    /// - C# 의 ?: 연산자와 역할을 하나, short cut 기능이 없으므로 주의 할 것.  tuple 두개 모두 평가된 후에 (?=) 함수로 전달됨.  (단순 값 위주로 사용 권장)
    ///
    ///   * e.g
    ///
    ///     let fail():bool = failwith "ERROR
    ///
    ///     true ?= (true, fail()) ==> fail() 도 실행되어 crash 함.
    ///
    ///     if true then true else fail() === 1     // OK
    let (?=) condition (trueResult, falseResult) =
        if condition then trueResult else falseResult

    /// condition 만족할 때만 action 수행
    ///
    /// e.g true ==> printfn "Good"
    let (==>) condition action = if condition then action() else ()


    /// id keyword 를 다른 용도로 사용하는 환경에서 id 함수를 구분하기 위함
    let idf = id

    /// Returns tuple of function execution result and duration
    let duration f =
        let stopWatch = Stopwatch.StartNew();
        let result = f()
        stopWatch.Stop()
        (result, stopWatch.ElapsedMilliseconds)

    /// Executes f twice
    let twice f = f >> f

    /// Executes f three times
    let thrice f = f >> f >> f

    /// Function composition
    let compose f g = f >> g

    /// Function(s) composition : http://www.fssnip.net/S/title/Composing-a-list-of-functions
    let composeFunctions fs = Seq.reduce (>>) fs
    // e.g composeFunctions [(*) 2; (+) 7; (*) 3; (+) 3] 3

    /// 함수 f 를 n 번 반복하는 함수 반환
    let inline ntimes (n:int) f = Dual.Common.Base.FS.Functions.ntimes n f


    /// Returns function which get successor
    let successor = fun x -> x + 1

    /// Returns function which get predecessor
    let predecessor = fun x -> x - 1

    /// Returns a function that returns the given constant value. `(fun _ -> a)`
    ///
    /// 주의 사항 : a expression 이 항상 evaluation 됨!.  evaluation 하지 않으려면 konst 쓰지 말고, fun () -> a 의 원 형태를 사용
    let inline konst a = (fun _ -> a)
    /// Returns a function that returns the given constant value. `(fun _ _ -> a)`
    let inline konst2 a = (fun _ _ -> a)

    /// 동일 value 값을 무한 반복하는 seq 반환
    let repeat value = Seq.initInfinite (konst value)

    /// 동일 f 값을 무한 반복하는 seq 반환
    let repeatWith f = Seq.initInfinite (fun _ -> f())

    //// Append x at the end of xs list: [1..3] +++ 4 = [1..4]
    //let (+++) xs x = xs @ [x]

    /// Y-combinator, or Sage bird
    let rec Y f x = f (Y f) x


    // https://stackoverflow.com/questions/42800373/f-pipe-forward-first-argument
    /// argument 의 순서를 바꾸어서 f 호출
    (*
        [1; 2; 3] |> (flip List.append) [4; 5; 6]   ==> [1; 2; 3; 4; 5; 6]
        [1; 2; 3] |> List.append [4; 5; 6]   ==> [4; 5; 6; 1; 2; 3]
        let swapf f = fun x y -> f y x
        let modulo2 = (flipf (%)) 2
        let isEven = modulo2 >> ((=) 0)
        let isOdd = modulo2 >> ((<>) 0)
    *)
    let flipf f x y = f y x

    let falsify _ = false
    let truthyfy _ = true


    /// 원본의 값을 그대로 반환하되, side effect 수행
    let inline tee (f:'a -> unit) (x: 'a) = Dual.Common.Base.FS.Functions.tee f x

    /// seq 원본의 값을 그대로 반환하되, seq 내부의 요소들에 대해서 side effect 수행
    let inline teeForEach (f:'a -> unit) (xs: 'a seq) = Dual.Common.Base.FS.Functions.teeForEach f xs

    /// 함수 f 수행하는 새로운 함수 반환 (새로운 함수는 이전 결과값을 caching 해서 speed up)
    let inline memoize f = Dual.Common.Base.FS.Functions.memoize f


    // http://stackoverflow.com/questions/18928268/f-numeric-type-casting
    /// General type casting
    /// e.g
    ///     convert<int> (box 1.1) -- converts object(->float) to int
    ///     convert<int> 1.23 -- converts float to int
    ///     convert<int> "123" -- converts string to int
    ///     convert<int> "1.23" -- crash!!!
    ///     convert<float> "1.23" |> cast<int> -- converts string -> float -> int
    let convert<'a> input = System.Convert.ChangeType(input, typeof<'a>) :?> 'a

    // http://stackoverflow.com/questions/18928268/f-numeric-type-casting
    /// type casting
    /// e.g
    ///     tryConvert<int> (box 1.1) -- Some(1)
    ///     tryConvert<int> 1.23 -- Some(1)
    ///     tryConvert<int> "123" -- Some(123)
    ///     tryConvert<int> "1.23" -- None
    let tryConvert<'a> input =
        try Some(convert<'a> input)
        with _ -> None

    /// Calls f and returns the result in Ok if it did not throw; otherwise catches the exception and returns it in an Error.
    let inline tryResult f =
        try Ok (f ())
        with e -> Error e

    /// type 검사
    let isType<'a> x = x :> obj :? 'a

    /// 강제 형변환 : 변환 실패시 exception 발생
    let forceCast<'a> x = x :> obj :?> 'a

    /// 두 object 를 최상위 type 인 obj 로 casting 한 후, 동일한지 비교
    let eq a b = (a :> obj) = (b :> obj)



    let inClosedRange (value:'a) ((min:'a), (max:'a)) =
        min <= value && value <= max

    //let min a b = if a < b then a else b
    //let max a b = if a > b then a else b



    let clip n s e = min e (max n s)

    /// f 로 주어진 lambda 함수를 실행하고 그 결과가 answer 값과 같지 않거나 예외가 발생하면 errmsg 로그를 출력하고 fail.
    let execFunc<'T when 'T: equality> (f:unit->'T) (answer:'T) (errmsg:string) =
        try
            let code = f()
            if code <> answer then
                failwithlogf "%s: Incorrect return value: (%A != %A)" errmsg answer code
                None
            else
                Some code
        with exn ->
            failwithlogf "Exception: %s:\r\n%O" errmsg exn
            None

    /// f 로 주어진 lambda 함수를 실행하고 예외가 발생하면 errmsg 로그를 출력하고 fail.
    let execAction(f:unit->unit) (errmsg:string) =
        try
            f()
        with exn ->
            failwithlogf "Exception while %s:\r\n%O" errmsg exn




    /// 수식 계산 : http://www.fssnip.net/1D/title/Parsing-string-expressions-the-lazy-way
    /// DataTable 에 Compute 기능이 있다!!!
    let evaluateExpression =
        let dt = new System.Data.DataTable()
        fun expr -> System.Convert.ToDouble(dt.Compute(expr, ""))

    // evaluateExpression "(1+5)*7/((3+(2-1))/(7-3))";;
    // val it : float = 42.0



    /// 실행파일이 존재하는 경로명 반환
    let getBinDir() =
        let entry = System.Reflection.Assembly.GetEntryAssembly()
        Path.GetDirectoryName(entry.Location)

    /// 실행파일이 존재하는 경로에서 주어진 파일의 full path 반환
    let getFullFilePathOnBin file = Path.Combine(getBinDir(), file)

    /// n-th(0 base) 로 predicate 을 충족하는지 검사
    let nthFinder predicate nth =
        let mutable count = -1
        fun x ->
            if predicate x then
                count <- count + 1
                count = nth
            else
                false

    /// index 와 element 로 predicate 수행.  매 수행마다 index 증가
    let indexedChooser chooser =
        let mutable index = -1
        fun x ->
            index <- index + 1
            chooser index x

    let verifyValue value =
        if not value then
            failwithlog "Failed to verify"
    let verifyWith f = f() |> verifyValue

    /// Type name 을 반환
    let typeName x = x.GetType().Name

    ///get_current_function_name
    let getFuncName() =
        let stackTrace = new System.Diagnostics.StackTrace()
        let stackFrame = stackTrace.GetFrame(1)
        let methodBase = stackFrame.GetMethod()
        methodBase.Name


    module private TestMe =
        let test() =
            // [1..10] 중에서 두번째로 나오는 3의 배수만 filter
            let nine =
                [1..10]
                |> List.filter (nthFinder (fun n -> n % 3 = 0) 2)       // ===> [9]
            assert(nine = [9])

            let evens =
                [1..100]
                |> List.skipWhile ((nthFinder (fun n -> n % 3 = 0) 2) >> not)       // ===> [.. 9] 까지 skip
                |> List.takeWhile ((nthFinder (fun n -> n % 3 = 0) 2) >> not)       // ===> [9..] 에서 3번째로 나오는 3의 배수 이전까지 수집
            assert(evens = [9..14])


            // [1..100] 에서 (20..50) 범위내의 3 의 배수
            // val it : int list = [24; 27; 30; 33; 36; 39; 42; 45; 48]
            let triples =
                [1..100]
                |> List.map (indexedChooser (fun n x ->
                    if 20 < n && n < 50 && x % 3 = 0 then Some x else None))
                |> List.choose id
            assert(triples = [24..3..48])

        let b = [1..10] |> List.map (float >> ((/) 2.0))        // ==> 2 / [1..10] ==> [2.0; 1.0; 0.6666666667; 0.5; 0.4; 0.3333333333; 0.2857142857; 0.25; 0.2222222222; 0.2]
        let c = [1..10] |> List.map (float >> (flipf (/) 2.0))  // ==> [1..10] / 2 ==> [0.5; 1.0; 1.5; 2.0; 2.5; 3.0; 3.5; 4.0; 4.5; 5.0]

