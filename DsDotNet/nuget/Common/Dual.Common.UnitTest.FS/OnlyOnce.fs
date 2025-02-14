namespace Dual.Common.UnitTest.FS

open System
open System.Linq
open FsUnit.Xunit
open System.Text.RegularExpressions
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Collections.Generic

[<AutoOpen>]
module Base =
    //let private tracefn fmt = Printf.kprintf System.Diagnostics.Trace.WriteLine fmt

    /// should equal
    let ShouldEqual x y                    = y |> should equal x
    let Eq x y                             = y |> should equal x
    let Neq x y                            = y |> should not' (equal x)
    let ShouldNotEqual x y                 = y |> should not' (equal x)
    let (===) x y                          = y |> should equal x
    let (===&) x y                         = y |> should equal x; y
    let (=!=) x y                          = y |> should not' (equal x)
    let ShouldBeTrue x                     = x |> should be True
    let SbTrue x                           = x |> should be True
    let ShouldBeFalse x                    = x |> should not' (be True)
    let SbFalse x                          = x |> should not' (be True)
    let ShouldBeNullOrEmptyString x        = x |> should be NullOrEmptyString
    let SbNullOrEmptyString x              = x |> should be NullOrEmptyString
    let ShouldBeEmpty x                    = x |> should be Empty
    let SbEmpty x                          = x |> should be Empty
    let ShouldNotBeEmpty x                 = x |> should not' (be Empty)
    let ShouldBeNull x                     = x |> should be Null
    let SbNull x                           = x |> should be Null
    let SbSome (x:'a option)               = x.IsSome |> SbTrue
    let SbNone (x:'a option)               = x.IsNone |> SbTrue
    let ShouldNotBeNull x                  = x |> should not' (be Null)

    let ShouldBeSameAs x                   = should be (sameAs x)
    let SbSameAs x                         = should be (sameAs x)
    let ShouldNotBeSameAs x                = should not' (be sameAs x)
    let SnbSameAs x                        = should not' (be sameAs x)
    let ShouldBeExactType<'t> x            = x |> should be ofExactType<'t>
    let SbExactType<'t> x                  = x |> should be ofExactType<'t>
    let ShouldNotBeExactType<'t> x         = x |> should not' (be ofExactType<'t>)
    let SnbExactType<'t> x                 = x |> should not' (be ofExactType<'t>)

    let ShouldThrowExceptionType<'ex> func = func |> should throw typeof<'ex>
    let ShouldContain value sequ           = sequ |> should contain value
    let ShouldFail func                    = func |> shouldFail
    let ShouldFailWith msg func            = func |> should (throwWithMessage msg) typeof<System.Exception>
    let ShouldFailWithT<'ex> msg func      = func |> should (throwWithMessage msg) typeof<'ex>
    let ShouldFailWithSubstringT<'ex when 'ex :> Exception> (substring:string) (func:unit->unit) =
        try
            func()
            failwithlog "No exception matched!"
        with
        | :? 'ex as excpt when excpt.Message.Contains substring ->
            tracefn $"Got expected exception:\r\n{excpt}"
            ()  // OK
        | _ as err ->
            failwith $"Exception messsage match failed on {err}.  expected = {substring}"
    let SeqEq a b = Enumerable.SequenceEqual(a, b) |> ShouldBeTrue
    let SetEq (xs:'a seq) (ys:'a seq) =
        (xs.Count() = ys.Count() && xs |> Seq.forall(fun x -> ys.Contains(x)) ) |> ShouldBeTrue

    /// line comment 삭제 및 trim 후, 두 문자열이 같은지 비교
    let (=~=) (xs:string) (ys:string) =
        let removeComment input =
            let blockComments = @"/\*(.*?)\*/"
            let lineComments = @"//(.*?)$"
            Regex.Replace(input, $"{blockComments}|{lineComments}", "", RegexOptions.Singleline)

        let toArray (xs:string) =
            xs.SplitByLine()
                .Select(removeComment)
                .Select(fun x -> x.Trim())
                |> Seq.where(fun x -> x.Any() && not <| x.StartsWith("//"))
                |> Array.ofSeq
        let xs = toArray xs
        let ys = toArray ys
        for (x, y) in Seq.zip xs ys do
            if x.Trim() <> y.Trim() then
                failwithf "[%s] <> [%s]" x y
        xs.Length === ys.Length


type UnitTestExtension =
    /// Unit test 용 Seq equal check
    [<Extension>] static member SeqEq<'T>(xs:IEnumerable<'T>, ys:IEnumerable<'T>) = SeqEq xs ys
