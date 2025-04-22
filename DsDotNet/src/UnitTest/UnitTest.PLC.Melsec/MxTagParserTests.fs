namespace T

open NUnit.Framework
open FsUnit.Xunit
open Dual.PLC.Common.FS
open MelsecProtocol

[<AutoOpen>]
module DataTypeTesterModule =

    let (===) x y = y |> should equal x

    type DataTypeTester() =

        // ✅ 1. 일반적인 비트(MxBit) 주소 테스트 
        [<Test>]
        member _.``MxBit Type Addresses Should Parse Correctly`` () =
            [
                "X12",     Some { Device = MxDevice.X;  DataTypeSize = MxBit;  BitOffset = 18 }
                "Y232",    Some { Device = MxDevice.Y;  DataTypeSize = MxBit;  BitOffset = 562 }
                "B4F",     Some { Device = MxDevice.B;  DataTypeSize = MxBit;  BitOffset = 79 }
                "SB12",    Some { Device = MxDevice.SB; DataTypeSize = MxBit;  BitOffset = 18 }
                "DX100",   Some { Device = MxDevice.DX; DataTypeSize = MxBit;  BitOffset = 256 }
                "DY45",    Some { Device = MxDevice.DY; DataTypeSize = MxBit;  BitOffset = 69 }
                "M0",      Some { Device = MxDevice.M;  DataTypeSize = MxBit;  BitOffset = 0 }
                "SM5",     Some { Device = MxDevice.SM; DataTypeSize = MxBit;  BitOffset = 5 }
            ]
            |> List.iter (fun (addr, expected) -> tryParseMxTag addr === expected)

        // ✅ 2. 일반적인 워드(MxWord) 주소 테스트
        [<Test>]
        member _.``MxWord Type Addresses Should Parse Correctly`` () =
            [
                "D122",    Some { Device = MxDevice.D;  DataTypeSize = MxWord; BitOffset = 1952 }
                "W3A",     Some { Device = MxDevice.W;  DataTypeSize = MxWord; BitOffset = 928 }
                "ZR10",    Some { Device = MxDevice.ZR; DataTypeSize = MxWord; BitOffset = 160 }
                "T15",     Some { Device = MxDevice.T;  DataTypeSize = MxWord; BitOffset = 240 }
                "C33",     Some { Device = MxDevice.C;  DataTypeSize = MxWord; BitOffset = 528 }
                "WF",      Some { Device = MxDevice.W;  DataTypeSize = MxWord; BitOffset = 240 }
                "SWF",     Some { Device = MxDevice.SW; DataTypeSize = MxWord; BitOffset = 240 }
                "SD123",   Some { Device = MxDevice.SD; DataTypeSize = MxWord; BitOffset = 1968 }
                "R99",     Some { Device = MxDevice.R;  DataTypeSize = MxWord; BitOffset = 1584 }
                "SW10",    Some { Device = MxDevice.SW; DataTypeSize = MxWord; BitOffset = 256 }
            ]
            |> List.iter (fun (addr, expected) -> tryParseMxTag addr === expected)

        // ✅ 3. 16진수 기반 비트 주소 테스트
        [<Test>]
        member _.``Hexadecimal MxBit Addresses Should Parse Correctly`` () =
            [
                "B4F",     Some { Device = MxDevice.B;  DataTypeSize = MxBit;  BitOffset = 79 }
                "SB2C",    Some { Device = MxDevice.SB; DataTypeSize = MxBit;  BitOffset = 44 }
                "XFF",     Some { Device = MxDevice.X;  DataTypeSize = MxBit;  BitOffset = 255 }
                "YF",      Some { Device = MxDevice.Y;  DataTypeSize = MxBit;  BitOffset = 15 }
                "W10",     Some { Device = MxDevice.W;  DataTypeSize = MxWord; BitOffset = 256 }
                "SW1F",    Some { Device = MxDevice.SW; DataTypeSize = MxWord; BitOffset = 496 }
            ]
            |> List.iter (fun (addr, expected) -> tryParseMxTag addr === expected)

        // ✅ 4. 비트 오프셋 포함된 주소 테스트
        [<Test>]
        member _.``MxBit Offset Included Addresses Should Parse Correctly`` () =
            [
                "D100.5",  Some { Device = MxDevice.D;  DataTypeSize = MxBit;  BitOffset = 1605 }
                "W20.3",   Some { Device = MxDevice.W;  DataTypeSize = MxBit;  BitOffset = 515 }
                "WA.1",    Some { Device = MxDevice.W;  DataTypeSize = MxBit;  BitOffset = 161 }
                "ZR50.7",  Some { Device = MxDevice.ZR; DataTypeSize = MxBit;  BitOffset = 807 }
                "R12.3",   Some { Device = MxDevice.R;  DataTypeSize = MxBit;  BitOffset = 195 }
                "SW100.2", Some { Device = MxDevice.SW; DataTypeSize = MxBit;  BitOffset = 4098 }
            ]
            |> List.iter (fun (addr, expected) -> tryParseMxTag addr === expected)

        // ✅ 5. K 형식 주소 (워드 단위 그룹)
        [<Test>]
        member _.``K-Format Addresses Should Parse Correctly`` () =
            [
                "K4M9",    Some { Device = MxDevice.M;  DataTypeSize = MxWord; BitOffset = 36 }  // 9 * 4
                "K2Y200",  Some { Device = MxDevice.Y;  DataTypeSize = MxWord; BitOffset = 1024 } // 200 * 2 (hex)
                "K8B0",    Some { Device = MxDevice.D;  DataTypeSize = MxWord; BitOffset = 24 }   // 3 * 8
                "K2X10",   Some { Device = MxDevice.X;  DataTypeSize = MxWord; BitOffset = 32 }   // 0x10 * 2 = 16*2
            ]
            |> List.iter (fun (addr, expected) -> tryParseMxTag addr === expected)

        // ✅ 6. 잘못된 주소 테스트
        [<Test>]
        member _.``Invalid Addresses Should Return None`` () =
            [
                "Invalid123"
                "XYZ"
                "XG12"
                "D..10"
                "123"
                "M-1"
                "P4Z"
                "A123"
                "D100.."
                "B-12"
                "T100.X"
                "WXYZ"
                "SBG12"
                "XZZ"
                "ZR.F"
                "K5Z10"
                "K0D5"
                "K3M-2"
                "K4A100"
                "K9M0"
                "MA"
            ]
            |> List.iter (fun addr -> tryParseMxTag addr === None)
