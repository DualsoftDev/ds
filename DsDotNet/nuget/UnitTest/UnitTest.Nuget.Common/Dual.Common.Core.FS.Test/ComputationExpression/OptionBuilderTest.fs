namespace T

open System
open NUnit.Framework

open Dual.Common.Core
open Dual.Common.Base.CS
open Dual.Common.UnitTest.FS
open Dual.Common.Core.FS

#nowarn "0044"

[<AutoOpen>]
module OptionBuilderTestModule =

    [<AllowNullLiteral>]
    type private RefClassSample() =
        member val Value = 1 with get, set
        member val Name = "" with get, set

    [<TestFixture>]
    type OptionBuilderTest() =
        [<Test>]
        member _.``OptionBuilder basics``() =
            option { do! None } === None
            option { do! Some() } === Some()

            option {
                do! Some()
                return None
            } === Some(None)

            option {
                do! Some()
                return! None
            } === None

            option {
                do! None
                return "Anything..."
            } === None

            option {
                do! Some()
                return "Hello"
            } === Some "Hello"


            option {
                let! x = Nullable<int>()    // short circuiting
                return "Hello"
            } === None

            option {
                let! x = Nullable<int>(3)
                return "Hello"
            } === Some "Hello"


            option {
                let! x = Option.ofObj(null)
                return "Hello"
            } === None

            option {
                let! x = Option.ofObj("goOn")
                return "Hello"
            } === Some "Hello"


            option {
                return ()
                failwith "This line, after return() should not be evaluated!"
            } === Some ()

            option {
                return "Hello"
                failwith "ERROR"
            } === Some "Hello"


            option {
                let! xxx = None
                failwith "This line, after short-cutted should not be evaluated!"
            } === None

        [<Test>]
        member _.``System.Nullable test``() =
            let opt1 = 1 |> Nullable.ofValue
            let nullObj:RefClassSample = null
            //let opt2 = nullObj |> Nullable.ofValue      // compile 오류
            ()

