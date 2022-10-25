namespace UnitTest.Engine

open System
open System.IO
open log4net
open log4net.Config
open Engine.Core
open Engine.Common.FS
open FsUnit.Xunit
open Engine.Common

// FsUnit/XUnit 사용법:
// https://github.com/fsprojects/FsUnit/tree/master/tests/FsUnit.Xunit.Test
// https://marnee.silvrback.com/fsharp-and-xunit-classfixture
[<AutoOpen>]
module Fixtures =
    let configureLog4Net (loggerName:string) log4netConfigFile =
        XmlConfigurator.Configure(new FileInfo(log4netConfigFile)) |> ignore
        let logger = LogManager.GetLogger(loggerName)
        Engine.Common.Global.Logger <- logger
        Engine.Parser.Global.Logger <- logger
        gLogger <- logger
        logger

    let SetUpTest() = 
            
            let cwd = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\"))
            sprintf "테스트 초기화 수행" |> ignore
            let configFile = $@"{cwd}App.config"
            let logger = configureLog4Net "EngineLogger" configFile

            // 로깅 결과 파일 : UnitTest.Engine/bin/logEngine*.txt
            logInfo "Log4net logging enabled!!!"

            if not (File.Exists configFile) then
                failwith "config 파일 위치를 강제로 수정해 주세요."
            ()

        //interface IDisposable with
        //    member __.Dispose () =
        //        //CLEAN UP TEST DATA OR WHATEVER YOU NEED TO CLEANUP YOUR TESTS
        //        ()

//module DemoTests =
//    type DemoTests() =
//        [<Test>]
//        member __.``Can create a start node`` () =
//            //DO THE TEST STUFF
//            "INPUT" |> should equal "RESULT"
//        interface IClassFixture<Fixtures.DemoFixture>

[<AutoOpen>]
module Base =
    /// should equal
    let ShouldEqual x y                    = y |> should equal x
    let Eq x y                             = y |> should equal x
    let Neq x y                            = y |> should not' (equal x)
    let ShouldNotEqual x y                 = y |> should not' (equal x)
    let (===) x y                          = y |> should equal x
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
            failwith "No exception matched!"
        with
        | :? 'ex as excpt when excpt.Message.Contains substring ->
            tracefn $"Got expected exception:\r\n{excpt}"
            ()  // OK
        | _ as err ->
            failwith $"Exception messsage match failed on {err}.  expected = {substring}"
