namespace T

open NUnit.Framework
open FsUnit.Xunit
open DsMxComm
open Dual.PLC.Common.FS

[<AutoOpen>]
module DataTypeTesterModule =

    let (===) x y = y |> should equal x

    type DataTypeTester() =

        // ✅ 1. 일반적인 비트(MxBit) 주소 테스트 
        [<Test>]
        member _.``MxBit Type Addresses Should Parse Correctly`` () =
            [
                "X12",    Some { Device = MxDevice.X;  DataTypeSize = MxBit;  BitOffset = 18 }
                "Y232",   Some { Device = MxDevice.Y;  DataTypeSize = MxBit;  BitOffset = 562 }
                "B4F",    Some { Device = MxDevice.B;  DataTypeSize = MxBit;  BitOffset = 79 }
                "SB12",   Some { Device = MxDevice.SB; DataTypeSize = MxBit;  BitOffset = 18 }
                "DX100",  Some { Device = MxDevice.DX; DataTypeSize = MxBit;  BitOffset = 256 }
                "DY45",   Some { Device = MxDevice.DY; DataTypeSize = MxBit;  BitOffset = 69 }
            ]
            |> List.iter (fun (addr, expected) -> tryParseMxTag addr === expected)

        // ✅ 2. 일반적인 워드(MxWord) 주소 테스트 (W는 Hex 변환 반영)
        [<Test>]
        member _.``MxWord Type Addresses Should Parse Correctly`` () =
            [
                "D122",   Some { Device = MxDevice.D;  DataTypeSize = MxWord; BitOffset = 1952 }
                "W3A",    Some { Device = MxDevice.W;  DataTypeSize = MxWord; BitOffset = 928 }
                "ZR10",   Some { Device = MxDevice.ZR; DataTypeSize = MxWord; BitOffset = 160 }
                "T15",    Some { Device = MxDevice.T;  DataTypeSize = MxWord; BitOffset = 240 }
                "C33",    Some { Device = MxDevice.C;  DataTypeSize = MxWord; BitOffset = 528 }
                "W20",    Some { Device = MxDevice.W;  DataTypeSize = MxWord; BitOffset = 512 }
                "WF",     Some { Device = MxDevice.W;  DataTypeSize = MxWord; BitOffset = 240 }
                "SWF",    Some { Device = MxDevice.SW; DataTypeSize = MxWord;  BitOffset = 240 }
                "SW10",   Some { Device = MxDevice.SW; DataTypeSize = MxWord;  BitOffset = 256 }
                "W3A",    Some { Device = MxDevice.W;  DataTypeSize = MxWord;  BitOffset = 928 }
            ]
            |> List.iter (fun (addr, expected) -> tryParseMxTag addr === expected)

        // ✅ 3. 16진수 기반 비트(MxBit) 주소 테스트 (W는 Hex 변환 반영)
        [<Test>]
        member _.``Hexadecimal MxBit Addresses Should Parse Correctly`` () =
            [
                "B4F",    Some { Device = MxDevice.B;  DataTypeSize = MxBit;  BitOffset = 79 }
                "SB2C",   Some { Device = MxDevice.SB; DataTypeSize = MxBit;  BitOffset = 44 }
                "XFF",    Some { Device = MxDevice.X;  DataTypeSize = MxBit;  BitOffset = 255 }
                "YF",     Some { Device = MxDevice.Y;  DataTypeSize = MxBit;  BitOffset = 15 }
            ]
            |> List.iter (fun (addr, expected) -> tryParseMxTag addr === expected)

        // ✅ 4. 비트 오프셋 포함된 주소 테스트 (W는 Hex 변환 반영)
        [<Test>]
        member _.``MxBit Offset Included Addresses Should Parse Correctly`` () =
            [
                "D100.5", Some { Device = MxDevice.D;  DataTypeSize = MxBit;  BitOffset = 1605 }
                "W20.3",  Some { Device = MxDevice.W;  DataTypeSize = MxBit;  BitOffset = 515 }
                "WA.1",   Some { Device = MxDevice.W;  DataTypeSize = MxBit;  BitOffset = 161 }
                "ZR50.7", Some { Device = MxDevice.ZR; DataTypeSize = MxBit;  BitOffset = 807 }
                "R12.3",  Some { Device = MxDevice.R;  DataTypeSize = MxBit;  BitOffset = 195 }
                "SW100.2",Some { Device = MxDevice.SW; DataTypeSize = MxBit;  BitOffset = 4098 }
            ]
            |> List.iter (fun (addr, expected) -> tryParseMxTag addr === expected)

        // ✅ 5. 잘못된 주소가 None을 반환하는지 테스트 (추가 케이스 포함)
        [<Test>]
        member _.``Invalid Addresses Should Return None`` () =
            [
                "Invalid123"
                "XYZ"
                "XG12"
                "D..10"
                "123"
                "M-1"
                "P4Z"  // 지원하지 않는 장치 타입
                "A123" // 존재하지 않는 장치명
                "D100.." // 잘못된 형식
                "B-12"   // 음수 주소 (잘못된 입력)
                "T100.X" // 잘못된 형식의 비트
                "ZRFF"   // 잘못된 ZR 포맷
                "WXYZ"   // 16진수 범위를 벗어난 값
                "SBG12"  // 잘못된 SB 포맷
                "XZZ"    // 잘못된 16진수 입력
                "RFF"    // 잘못된 R 포맷
            ]
            |> List.iter (fun addr -> tryParseMxTag addr === None)
