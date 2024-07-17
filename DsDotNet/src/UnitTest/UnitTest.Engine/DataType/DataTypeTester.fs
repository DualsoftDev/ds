namespace T

open Dual.UnitTest.Common.FS
open Engine.Core
open NUnit.Framework
open System.Linq
open Engine.Parser.FS

[<AutoOpen>]
module DataTypeTesterModule =

    type DataTypeTester() =

        [<Test>]
        member _.``Test Group 1`` () =
            getTextValueNType "\"Hello\"" === (Some ("Hello", DuSTRING))
            getTextValueNType "'a'" === (Some ("a", DuCHAR))
            getTextValueNType "3.14f" === (Some ("3.14", DuFLOAT32))
            getTextValueNType "3.14" === (Some ("3.14", DuFLOAT64))
            getTextValueNType "255uy" === (Some ("255", DuUINT8))

        [<Test>]
        member _.``Test Group 2`` () =
            getTextValueNType "65535us" === (Some ("65535", DuUINT16))
            getTextValueNType "18446744073709551615UL" === (Some ("18446744073709551615", DuUINT64))
            getTextValueNType "true" === (Some ("true", DuBOOL))
            getTextValueNType "false" === (Some ("false", DuBOOL))
            getTextValueNType "9223372036854775807L" === (Some ("9223372036854775807", DuINT64))

        [<Test>]
        member _.``Test Group 3`` () =
            getTextValueNType "4294967295u" === (Some ("4294967295", DuUINT32))
            getTextValueNType "127y" === (Some ("127", DuINT8))
            getTextValueNType "32767s" === (Some ("32767", DuINT16))
            getTextValueNType "2147483647" === (Some ("2147483647", DuINT32))
            getTextValueNType "2147483647l" === (Some ("2147483647", DuINT32))

        [<Test>]
        member _.``Test Group 4`` () =
            getTextValueNType "\"\"" === (Some ("", DuSTRING))
            getTextValueNType "' '" === (Some (" ", DuCHAR))
            getTextValueNType "123f" === (Some ("123", DuFLOAT32))
            getTextValueNType "123." === (Some ("123.", DuFLOAT64))
            getTextValueNType "0uy" === (Some ("0", DuUINT8))

        [<Test>]
        member _.``Test Group 5`` () =
            getTextValueNType "0us" === (Some ("0", DuUINT16))
            getTextValueNType "0UL" === (Some ("0", DuUINT64))
            getTextValueNType "True" === (Some ("True", DuBOOL))
            getTextValueNType "False" === (Some ("False", DuBOOL))
            getTextValueNType "0L" === (Some ("0", DuINT64))

        [<Test>]
        member _.``Test Group 6`` () =
            getTextValueNType "0u" === (Some ("0", DuUINT32))
            getTextValueNType "0y" === (Some ("0", DuINT8))
            getTextValueNType "0s" === (Some ("0", DuINT16))
            getTextValueNType "0" === (Some ("0", DuINT32))
            getTextValueNType "Hello" === None

        [<Test>]
        member _.``Test Group 7`` () =
            getTextValueNType "'ab'" === None
            getTextValueNType "3.14a" === None
            getTextValueNType "3,14" === None
            getTextValueNType "256uy" === None
            getTextValueNType "65536us" === None

        [<Test>]
        member _.``Test Group 8`` () =
            getTextValueNType "18446744073709551616UL" === None
            getTextValueNType "9223372036854775808L" === None
            getTextValueNType "4294967296u" === None
            getTextValueNType "256y" === None
            getTextValueNType "32768s" === None

        [<Test>]
        member _.``Test Group 9`` () =
            getTextValueNType "2147483648" === None
            getTextValueNType "\"\"" === (Some ("", DuSTRING))
            getTextValueNType "\"Test\"" === (Some ("Test", DuSTRING))
            getTextValueNType "'X'" === (Some ("X", DuCHAR))
            getTextValueNType "0.0f" === (Some ("0.0", DuFLOAT32))

        [<Test>]
        member _.``Test Group 10`` () =
            getTextValueNType "0.0" === (Some ("0.0", DuFLOAT64))
            getTextValueNType "1uy" === (Some ("1", DuUINT8))
            getTextValueNType "1us" === (Some ("1", DuUINT16))
            getTextValueNType "1UL" === (Some ("1", DuUINT64))
            getTextValueNType "1u" === (Some ("1", DuUINT32))
            getTextValueNType "1L" === (Some ("1", DuINT64))
            getTextValueNType "32767s" === (Some ("32767", DuINT16))
            getTextValueNType "65535us" === (Some ("65535", DuUINT16))
