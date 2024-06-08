namespace T.Expression
open T

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open Dual.UnitTest.Common.FS

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
Person hong;
Person people[10];

"""
            let statements = udt |> parseCodeForWindows storages;
            statements |> List.iteri (fun i st -> tracefn "[%d] %A" i st)

            statements[0] === DuUdtDecl ("Person", [
                                            { Type = "string"; Name = "name" };
                                            { Type = "int"; Name = "age" }])
            statements[1] === DuUdtInstances ("Person", "hong", 1)
            statements[2] === DuUdtInstances ("Person", "people", 10)




