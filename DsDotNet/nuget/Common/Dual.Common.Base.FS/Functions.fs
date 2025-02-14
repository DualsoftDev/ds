namespace Dual.Common.Base.FS


open System
open System.Runtime.CompilerServices
open System.Diagnostics
open System.Collections.Generic

[<AutoOpen>]
module Functions =
    /// Unit Test 에서 실행 중인지 여부 반환
    let isInUnitTest() =
        AppDomain.CurrentDomain.GetAssemblies()
            |> Seq.map _.FullName
            |> Seq.exists _.StartsWith("Microsoft.VisualStudio.TestPlatform.")
            ;

    let getCallStackFunctionNames() =
        let stackTrace = new System.Diagnostics.StackTrace()
        stackTrace.GetFrames() |> Seq.map (fun f -> f.GetMethod().Name)

    /// get current function name
    // 현재 getFuncName 은 제외해야 하므로 1 번째 함수명 반환
    let getFuncName() = getCallStackFunctionNames() |> Seq.item 1


    /// Returns tuple of function execution result and duration
    let duration f =
        let stopWatch = Stopwatch.StartNew();
        let result = f()
        stopWatch.Stop()
        (result, stopWatch.ElapsedMilliseconds)

    /// 원본의 값을 그대로 반환하되, side effect 수행
    let tee (f:'a -> unit) (x: 'a) =
        f x
        x

    let teeForEach (f:'a -> unit) (xs: 'a seq) =
        xs |> Seq.iter f
        xs



    /// 함수 f 수행하는 새로운 함수 반환 (새로운 함수는 이전 결과값을 caching 해서 speed up)
    let memoize f =
        let cache = Dictionary()
        fun x ->
            match cache.TryGetValue x with
            | true, v -> v
            | _ ->
                let vv = f x
                cache.Add(x, vv)
                vv

    /// 함수 f 를 n 번 반복하는 함수 반환
    let rec ntimes (n:int) f =
        if n = 0 then id
        else
            let g = ntimes (n-1) f
            f >> g

    /// 강제 dispose.  주로 Seq.cache 된 obj 에 사용
    let forceDispose(disposable:obj) =
        match disposable with
        | :? IDisposable as disp -> disp.Dispose()
        | _ -> failwith "ERROR: Not disposable object!"

[<Extension>]
type NullExt =
    [<Extension>] static member NullableDefaultValue(x:'T when 'T: not struct, v) = if obj.ReferenceEquals (x, null) then v else x      // obj.ReferenceEquals (x, null) == isNull x
    [<Extension>] static member NullableDefaultWith(x:'T when 'T: not struct, f) = if obj.ReferenceEquals (x, null) then f() else x


#if false
[<AllowNullLiteral>]
type Person(name:string, age:int) =
    member x.Name = name
    member x.Age = age

type Student(name:string, age:int) =
    inherit Person(name, age)

type Teacher(name:string, age:int) =
    inherit Person(name, age)

let p1:Person = Teacher("teacher", 32)
let p2:Person = Student("student", 32)

let sp1 = Some p1
let sp2 = Some p2

let kwak = Person("kwak", 32)
let getNull<'T when 'T : not struct>():'T = Operators.Unchecked.defaultof<'T>
let n:Person = getNull<Person>()
let who = n.DefaultValue kwak
printfn $"who is {who.Name}"
let kim = Person("kim", 31)
let kwakOrKim = kwak.DefaultValue kim
printfn $"{kwakOrKim.Name} == kwak"
#endif