namespace T.Expression
open T

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS

[<AutoOpen>]
module UdtTestModule =

    type UdtTest() =
        inherit ExpressionTestBaseClass()

        [<Test>]
        member __.``1 udt declaration test`` () =
            let storages = Storages()
            let udt = """
struct Person {
    string name;
    int age;
};
//Person hong;
//Person people[10];

"""
            let statements = udt |> parseCodeForWindows storages;
            tracefn $"{statements[0]}"
            ()




