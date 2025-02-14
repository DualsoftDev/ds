namespace T

open System
open NUnit.Framework

open Dual.Common.UnitTest.FS
open Dual.Common.Core.FS


#nowarn "0044"

[<AutoOpen>]
module ResultBuilderTestModule =
    let inline (==) x y = Result.equal x y === true


    //type ResultBuilder() =
    //    member this.Bind (x, f) = Result.bind f x
    //    member this.Return x = Ok x
    //    member this.ReturnFrom (x: Result<_,_>) = x
    //    member this.Zero () = Ok ()
    //    member this.Delay f = f
    //    member this.Run f = f ()
    //    member this.Combine (x: Result<unit, _>, f) = Result.bind f x

    //    member this.TryWith (body, handler) =
    //        try this.ReturnFrom (body ())
    //        with e -> handler e

    //    member this.TryFinally (body, compensation) =
    //        try this.ReturnFrom (body ())
    //        finally compensation ()

    //    member this.Using (disposable: #IDisposable, body) =
    //        let body' () = body disposable
    //        this.TryFinally(body', fun () -> disposable.Dispose())

    //    member this.While (guard, body) =
    //        if guard() then
    //            this.Bind(body(), fun () ->
    //                this.While (guard, body)
    //            )
    //        else
    //            this.Zero()

    //    member this.For (items: seq<_>, body) =
    //        this.Using(items.GetEnumerator(), fun it ->
    //            this.While(it.MoveNext,
    //                this.Delay(fun () -> body it.Current)
    //            )
    //        )



    ///// Result<> type 모나드
    /////
    ///// Combine returns upon encountering an Error value, so an `if` without an else that returns an Error value will return
    ///// that value without executing the following code, while an Ok () will return the result of the following code.
    //let result = ResultBuilder()


    [<TestFixture>]
    type ResultBuilderTest() =
        // 테스트에서 사용할 기본적인 Result 빌더와 동작 테스트
        [<Test>]
        member _.``ResultBuilder basics``() =
            result { return "Good" } == Ok "Good"
            result { return! Ok "Good" } == Ok "Good"
            result { return! Error "Bad" } == Error "Bad"

            // Ok 값의 동작 확인
            result {
                do! Error "Error"
            } == Error "Error"

            result {
                do! Ok ()
            } == Ok ()

            result {
                do! Ok ()
                return Error "Inner error"
            } == Ok (Error "Inner error")

            result {
                do! Ok ()
                return! Error "Propagation error"
            } == Error "Propagation error"

            result {
                do! Error "Outer error"
                return "Anything..."
            } == Error "Outer error"

            result {
                do! Ok ()
                return "Hello"
            } == Ok "Hello"

            // Nullable을 Result로 변환하여 테스트
            result {
                let! x = Nullable<int>() |> Option.ofNullable |> Result.ofOption "Null value"
                return "Hello"
            } == Error "Null value"

            result {
                let! x = Nullable<int>(3) |> Option.ofNullable |> Result.ofOption "Null value"
                return "Hello"
            } == Ok "Hello"

            // obj를 Result로 변환하여 테스트
            result {
                let! x = Option.ofObj(null : obj) |> Result.ofOption "Null object"
                return "Hello"
            } == Error "Null object"

            result {
                let! x = Option.ofObj("goOn" : obj) |> Result.ofOption "Null object"
                return "Hello"
            } == Ok "Hello"

        [<Test>]
        member _.``Return after should not execute``() =
            result {
                return ()
                failwith "This line, after return() should not be evaluated!"
            } == Ok ()

            result {
                let! xxx = Error "Some error"
                failwith "This line, after short-cutted should not be evaluated!"
            } == Error "Some error"


//        [<Test>]
//        member _.``X For loop should execute without error``() =
//            let items = [1; 2; 3]
//            let rUnit =
//                result {
//                    for item in items do
//                        if item = 2 then
//                            forceTrace "Breaking loop"
//                            return ()
//                    failwith "이 문장은 실행 되면 안됩니다."
//                }

//            rUnit == Ok ()


//#if false   // 컴파일 안됨!!
//            let rOk =
//                result {
//                    for item in items do
//                        if item = 2 then
//                            forceTrace "Breaking loop"
//                            return! Ok "Item is 2"
//                    failwith "이 문장은 실행 되면 안됩니다."
//                }

//            rOk == Ok "Item is 2"
//#endif

//            let rError =
//                result {
//                    for item in items do
//                        if item = 2 then
//                            forceTrace "Breaking loop"
//                            return! Error "Item is 2"
//                    failwith "이 문장은 실행 되면 안됩니다."
//                }

//            rError == Error "Item is 2"
        //[<Test>]
        //member _.``X While loop should execute until condition fails``() =
        //    let mutable counter = 0
        //    let result =
        //        result {
        //            while counter < 3 do
        //                counter <- counter + 1
        //                if counter = 2 then
        //                    return! Error "Counter reached 2"
        //            failwith "이 문장은 실행 되면 안됩니다."
        //        }

        //    result == Error "Counter reached 2"

        [<Test>]
        member _.``Using should dispose resource``() =

            let mutable disposed = false
            let createTestDisposable() =
                {
                    new IDisposable with
                        member _.Dispose() =
                            disposed <- true
                }

            let resource = createTestDisposable()
            let res =
                result {
                    use r = resource
                    return "Resource used"
                }

            res == (Ok "Resource used")
            disposed === true


    [<TestFixture>]
    type ResultCastTest() =

        [<Test>]
        member _.``Result.Cast should succeed for valid type conversion``() =
            // int to float casting (Ok case)
            let result: Result<int, string> = Ok 42
            let castResult = result.Cast<float>()
            castResult == Ok 42.0

            // string to obj casting (Ok case)
            let resultStr: Result<string, string> = Ok "Hello"
            let castResultStr = resultStr.Cast<obj>()
            castResultStr == Ok (box "Hello")

        [<Test>]
        member _.``Result.Cast should throw exception for invalid type conversion``() =
            // int to string casting (invalid cast, should throw)
            let result: Result<int, string> = Ok 42
            (fun () -> result.Cast<string>() |> ignore) |> ShouldFail

            // Error case should not cast and return the same error
            let resultError: Result<int, string> = Error "Some error"
            let castResultError = resultError.Cast<float>()
            castResultError == Error "Some error"

        [<Test>]
        member _.``Result.TryCast should succeed for valid type conversion``() =
            // int to float casting (Ok case)
            let result: Result<int, string> = Ok 42
            let tryCastResult = result.TryCast<float>()
            tryCastResult == Ok 42.0

            // string to obj casting (Ok case)
            let resultStr: Result<string, string> = Ok "Hello"
            let tryCastResultStr = resultStr.TryCast<obj>()
            tryCastResultStr == Ok (box "Hello")

        [<Test>]
        member _.``Result.TryCast should return Error for invalid type conversion``() =
            // int to string casting (invalid cast, should return Error)
            let result: Result<int, string> = Ok 42
            let tryCastResult = result.TryCast<string>()
            tryCastResult == Error "Casting failure"

            // Error case should not cast and return the same error
            let resultError: Result<int, string> = Error "Some error"
            let tryCastResultError = resultError.TryCast<float>()
            tryCastResultError == Error "Some error"