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
            let code = """
int8 myInt8 = 0y;
uint8 mySByte = 0uy;
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
            let statements = code |> parseCode;

            let typeMismatches =
                [
                    "int8 myByte = 0s;"
                    "double myDouble = 0;"
                    "int myInt = 0.0;"
                ]
            for m in typeMismatches do
                (fun () -> m |> parseCode |> ignore) |> ShouldFailWithSubstringT "Type mismatch"
            ()

