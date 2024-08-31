namespace T.Expression
open T

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open Dual.Common.UnitTest.FS

[<AutoOpen>]
module UdtTestModule =

    type UdtTest() =
        inherit ExpressionTestBaseClass()

        [<Test>]
        member __.``1 udt declaration test`` () =
            let storages = Storages()
            let udt = """
// [0]
struct Person {
    string name;
    int age;
};
Person hong;            // [1]
Person people[10];      // [2]

$hong.name = "Hong";    // [3]
$hong.age = 20;         // [4]

copyStructIf(true, $hong, $people[0]);      // [5]

"""
            let statements = udt |> parseCodeForWindows storages;
            statements |> List.iteri (fun i st -> tracefn "[%d] %A" i st)

            let udtDecl = {
                        TypeName = "Person";
                        Members = [
                            { Type = typedefof<string>; Name = "name" };
                            { Type = typedefof<int>; Name = "age" }
                        ]}
            statements[0] === DuUdtDecl udtDecl


            statements[1] === DuUdtDef { TypeName = "Person"; VarName="hong";   ArraySize=1 }
            statements[2] === DuUdtDef { TypeName = "Person"; VarName="people"; ArraySize=10 }

            match statements[5] with
            | DuAction ( DuCopyUdt { Storages=xStorages; UdtDecl=xUdtDecl; Condition=xCond; Source=xSource; Target=xTarget } ) ->
                xStorages === storages
                xUdtDecl === udtDecl
                xCond.BoxedEvaluatedValue === true// DuTerminal(DuLiteral({Value=true}))
                xSource === "hong"
                xTarget === "people[0]"
            | _ -> failwith "Invalid statement"

            let instances = "hong"::[ for i in [0..9] -> $"people[{i}]" ]
            for ins in instances do
                storages.ContainsKey $"{ins}.name" === true
                storages.ContainsKey $"{ins}.age" === true
            storages["hong.name"].BoxedValue === "Hong"
            storages["hong.age"].BoxedValue === 20
            storages["people[0].name"].BoxedValue === "Hong"
            storages["people[0].age"].BoxedValue === 20

            for ins in [ for i in [1..9] -> $"people[{i}]" ] do
                storages[$"{ins}.name"].BoxedValue === ""
                storages[$"{ins}.age"].BoxedValue === 0




