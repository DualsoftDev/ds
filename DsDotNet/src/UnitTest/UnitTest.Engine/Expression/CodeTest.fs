namespace UnitTest.Engine.Expression

open NUnit.Framework

open Engine.Parser.FS
open UnitTest.Engine

[<AutoOpen>]
module CodeTestModule =

    type CodeTest() =
        do Fixtures.SetUpTest()

        [<Test>]
        member __.``1 var declaration test`` () =
//            let code = """
//int8 myByte = 0y;
//int16 myShort = 0uy;
//int32 myInt = 0;
//int64 myLong = 0L;
//float32 myFloat = 0.0f;
//float32 myFloat = 0.0f;
//"""
//            let statements = code |> parseCode;

            let typeMismatches =
                [
                    "int8 myByte = 0s;"
                    "double myDouble = 0;"
                    "int myInt = 0.0;"
                ]
            for m in typeMismatches do
                (fun () -> m |> parseCode |> ignore) |> ShouldFailWithSubstringT "Type mismatch"
            ()

