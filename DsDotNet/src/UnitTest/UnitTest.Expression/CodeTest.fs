namespace T.Expression
open Dual.UnitTest.Common.FS

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Dual.Common.Core.FS

[<AutoOpen>]
module CodeTestModule =

    type CodeTest() =
        inherit ExpressionTestBaseClass()

        [<Test>]
        member __.``1 var declaration test`` () =
            use _ = setRuntimeTarget WINDOWS
            let storages = Storages()
            let numericDeclarations = """
int8 myInt8 = 0y;
uint8 myUInt8 = 0uy;
byte myByte = 0uy;
sbyte mySByte = 0y;

int16 myInt16 = 0s;
uint16 myUInt16 = 0us;
short myShort = 0s;
ushort myUShort = 0us;

int32 myInt32 = 0;
uint32 myUInt32 = 0u;
int myInt = 0;
uint myUInt = 0u;

int64 myInt64 = 0L;
uint64 myUInt64 = 0UL;
long myLong = 0L;
ulong myULong = 0UL;


float32 myFloat32 = 0.0f;
single mySingle = 0.0f;
float64 myFloat64 = 0.0;
double myDouble = 0.0;
"""
            let statements = numericDeclarations |> parseCodeForWindows storages;
            let numAddedVariables = numericDeclarations.Split([|'\r'; '\n'|], StringSplitOptions.RemoveEmptyEntries).Length
            storages.Count === numAddedVariables

            for KeyValue(k, v) in storages do
                toFloat64 v.BoxedValue === 0.0

            (fun () -> "int8 myInt8 = 0y;" |> parseCodeForWindows storages |> ignore) |> ShouldFailWithSubstringT "Duplicated"
            storages.Count === numAddedVariables

            let fails = [
                "Duplicated"   , "int8 myByte = 0s;"
                "Duplicated"   , "double myDouble = 0;"
                "Duplicated"   , "int myInt = 0.0;"

                "Type mismatch", "int8 myByte2 = 0s;"
                "Type mismatch", "double myDouble2 = 0;"
                "Type mismatch", "int myInt2 = 0.0;"
            ]
            for (expectedFailMessage, failText) in fails do
                (fun () -> failText |> tryParseStatement4UnitTest WINDOWS storages |> ignore) |> ShouldFailWithSubstringT expectedFailMessage

            ()

        [<Test>]
        member __.``1 var initialization test`` () =
            use _ = setRuntimeTarget WINDOWS
            let storages = Storages()
            let integers = """
int8 myInt8 = 32y;
uint8 myUInt8 = 32uy;

int16 myInt16 = 32s;
uint16 myUInt16 = 32us;

int32 myInt32 = 32;
uint32 myUInt32 = 32u;

int64 myInt64 = 32L;
uint64 myUInt64 = 32UL;

"""
            let statements = integers |> parseCodeForWindows storages;
            let numAddedVariables = integers.Split([|'\r'; '\n'|], StringSplitOptions.RemoveEmptyEntries).Length
            storages.Count === numAddedVariables

            for KeyValue(k, v) in storages do
                toFloat64 v.BoxedValue === 32.0


        [<Test>]
        member __.``2 var initialization test`` () =
            use _ = setRuntimeTarget WINDOWS
            let storages = Storages()
            let floats = """
float32 myFloat32 = 3.14f;
single mySingle = 3.14f;

float64 myFloat64 = 3.14;
double myDouble = 3.14;
"""
            let statements = floats |> parseCodeForWindows storages;
            let numAddedVariables = floats.Split([|'\r'; '\n'|], StringSplitOptions.RemoveEmptyEntries).Length
            storages.Count === numAddedVariables

            for KeyValue(k, v) in storages do
                Math.Abs(toFloat64 v.BoxedValue - 3.14) <= 0.0001 |> ShouldBeTrue


        [<Test>]
        member __.``3 var initialization test`` () =
            use _ = setRuntimeTarget WINDOWS
            let storages = Storages()
            let floats = """
float32 myFloat32 = 3.14f + 3.14f;
single mySingle = 3.14f + 3.14f;

float64 myFloat64 = 3.14 + 3.14;
double myDouble = 3.14 + 3.14;
"""
            let statements = floats |> parseCodeForWindows storages;
            let numAddedVariables = floats.Split([|'\r'; '\n'|], StringSplitOptions.RemoveEmptyEntries).Length
            storages.Count === numAddedVariables

            for KeyValue(k, v) in storages do
                Math.Abs(toFloat64 v.BoxedValue - 3.14 * 2.0) <= 0.0001 |> ShouldBeTrue



        [<Test>]
        member __.``3 string/char initialization test`` () =
            use _ = setRuntimeTarget WINDOWS
            let storages = Storages()
            let declarations = [
                $"string myString = {dq}Hello, world{dq};"
                "char myChar = 'x';"
            ]

            let statements =
                [
                    for s in declarations do
                        let statement = tryParseStatement4UnitTest WINDOWS storages s  |> Option.get
                        statement.ToText() === s
                        yield statement
                ]

            match statements[0] with
            | DuVarDecl (expr, target) -> target.BoxedValue === "Hello, world"
            | _ -> failwithlog "ERROR"

            match statements[1] with
            | DuVarDecl (expr, target) -> target.BoxedValue === 'x'
            | _ -> failwithlog "ERROR"

            storages.Count === declarations.Length


        [<Test>]
        member __.``4 coode block test`` () =
            use _ = setRuntimeTarget WINDOWS
            let systemRepo = ShareableSystemRepository()
            let parseText text =
                ModelParser.ParseFromString(text, ParserOptions.Create4Simulation(systemRepo, ".", "ActiveCpuName", None, DuNone))

            let storages = Storages()
            let ds = """
[sys] MySystem = {
    [jobs] = {}
    [variables] = {}
    [commands] = {
        test = #{
            float32 myFloat32 = 3.14f + 3.14f;
            single mySingle = 3.14f + 3.14f;
        }
    }
}
"""
            let system = parseText ds
            system.Functions.Count === 1
            system.Functions[0].Statements[0].ToText() ==="float32 myFloat32 = 3.140000105f + 3.140000105f;"
            system.Functions[0].Statements[1].ToText() ==="float32 mySingle = 3.140000105f + 3.140000105f;"

            let text = system.ToDsText(true, false)
            ds =~= text
            ()

