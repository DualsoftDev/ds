namespace UnitTest.Engine.Expression

open NUnit.Framework

open Engine.Parser.FS
open UnitTest.Engine
open System
open Engine.Core

[<AutoOpen>]
module CodeTestModule =

    type CodeTest() =
        do Fixtures.SetUpTest()

        [<Test>]
        member __.``1 var declaration test`` () =
            let storages = Storages()
            let code = """
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
            let statements = code |> parseCode storages;
            let numAddedVariables = code.Split([|'\r'; '\n'|], StringSplitOptions.RemoveEmptyEntries).Length
            storages.Count === numAddedVariables

            for KeyValue(k, v) in storages do
                ExpressionPrologSubModule.toDouble v.Value === 0.0

            (fun () -> "int8 myInt8 = 0y;" |> parseCode storages |> ignore) |> ShouldFailWithSubstringT "Duplicated"
            storages.Count === numAddedVariables

            let typeMismatches =
                [
                    "int8 myByte = 0s;"
                    "double myDouble = 0;"
                    "int myInt = 0.0;"
                ]
            for m in typeMismatches do
                (fun () -> m |> parseCode storages |> ignore) |> ShouldFailWithSubstringT "Type mismatch"
            ()

        [<Test>]
        member __.``1 var initialization test`` () =
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
            let statements = integers |> parseCode storages;
            let numAddedVariables = integers.Split([|'\r'; '\n'|], StringSplitOptions.RemoveEmptyEntries).Length
            storages.Count === numAddedVariables

            for KeyValue(k, v) in storages do
                ExpressionPrologSubModule.toDouble v.Value === 32.0


        [<Test>]
        member __.``2 var initialization test`` () =
            let storages = Storages()
            let floats = """
float32 myFloat32 = 3.14f;
single mySingle = 3.14f;

float64 myFloat64 = 3.14;
double myDouble = 3.14;
"""
            let statements = floats |> parseCode storages;
            let numAddedVariables = floats.Split([|'\r'; '\n'|], StringSplitOptions.RemoveEmptyEntries).Length
            storages.Count === numAddedVariables

            for KeyValue(k, v) in storages do
                Math.Abs(ExpressionPrologSubModule.toDouble v.Value - 3.14) <= 0.0001 |> ShouldBeTrue


        [<Test>]
        member __.``3 var initialization test`` () =
            let storages = Storages()
            let floats = """
float32 myFloat32 = 3.14f + 3.14f;
single mySingle = 3.14f + 3.14f;

float64 myFloat64 = 3.14 + 3.14;
double myDouble = 3.14 + 3.14;
"""
            let statements = floats |> parseCode storages;
            let numAddedVariables = floats.Split([|'\r'; '\n'|], StringSplitOptions.RemoveEmptyEntries).Length
            storages.Count === numAddedVariables

            for KeyValue(k, v) in storages do
                Math.Abs(ExpressionPrologSubModule.toDouble v.Value - 3.14 * 2.0) <= 0.0001 |> ShouldBeTrue

