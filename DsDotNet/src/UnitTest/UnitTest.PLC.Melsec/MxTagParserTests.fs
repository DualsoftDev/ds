namespace T

open NUnit.Framework
open FsUnit.Xunit
open Dual.PLC.Common.FS
open MelsecProtocol

[<AutoOpen>]
module DataTypeTesterModule =

    let (===) (x: MxDeviceInfo option) (y: MxDeviceInfo option) =
        match x, y with
        | Some a, Some b ->
            a.Device |> should equal b.Device
            a.BitOffset |> should equal b.BitOffset
            a.DataTypeSize |> should equal b.DataTypeSize
            a.NibbleK |> should equal b.NibbleK
            MxDeviceInfo.Create(a.Address) |> should equal (Some a)
        | None, None -> ()
        | _ -> failwithf "Expected %A but got %A" y x


    type DataTypeTester() =

        [<Test>]
        member _.``Parse Valid MxBit Addresses`` () =
            [
                "X12", MxDevice.X, 0x12
                "Y232", MxDevice.Y, 0x232
                "B4F", MxDevice.B, 0x4F
                "SB12", MxDevice.SB, 0x12
                "DX100", MxDevice.DX, 0x100
                "DY45", MxDevice.DY, 0x45
                "M0", MxDevice.M, 0
                "SM5", MxDevice.SM, 5
            ]
            |> List.iter (fun (addr, device, bitOffset) ->
                Some {
                    Device = device
                    BitOffset = bitOffset
                    DataTypeSize = MxDeviceType.MxBit
                    NibbleK = 0
                } === MxDeviceInfo.Create(addr))

        [<Test>]
        member _.``Parse Valid MxWord Addresses`` () =
            [
                "D122", MxDevice.D, 122
                "W3A", MxDevice.W, 0x3A
                "ZR10", MxDevice.ZR, 10
                "WF", MxDevice.W, 0xF
                "SWF", MxDevice.SW, 0xF
                "SD123", MxDevice.SD, 123
                "R99", MxDevice.R, 99
                "SW10", MxDevice.SW, 0x10
            ]
            |> List.iter (fun (addr, device, wordOffset) ->
                Some {
                    Device = device
                    BitOffset = wordOffset * 16
                    DataTypeSize = MxDeviceType.MxWord
                    NibbleK = 0
                } === MxDeviceInfo.Create(addr))

        [<Test>]
        member _.``Parse Valid MxDotBit Addresses`` () =
            [
                "D100.5", MxDevice.D, 100 * 16 + 5
                "W20.3", MxDevice.W, 0x20 * 16 + 3
                "WA.1", MxDevice.W, 0xA * 16 + 1
                "ZR50.7", MxDevice.ZR, 50 * 16 + 7
                "R12.3", MxDevice.R, 12 * 16 + 3
                "SW100.2", MxDevice.SW, 0x100 * 16 + 2
            ]
            |> List.iter (fun (addr, device, bitOffset) ->
                Some {
                    Device = device
                    BitOffset = bitOffset
                    DataTypeSize = MxDeviceType.MxDotBit
                    NibbleK = 0
                } === MxDeviceInfo.Create(addr))

        [<Test>]
        member _.``Parse Valid K Format Addresses`` () =
            [
                "K4M9", MxDevice.M, 9, 4
                "K2Y200", MxDevice.Y, 0x200, 2
                "K8B0", MxDevice.B, 0, 8
                "K2X10", MxDevice.X, 0x10, 2
            ]
            |> List.iter (fun (addr, device, bitOffset, nibbleK) ->
                Some {
                    Device = device
                    BitOffset = bitOffset
                    DataTypeSize = MxDeviceType.MxBit
                    NibbleK = nibbleK
                } === MxDeviceInfo.Create(addr))

        [<Test>]
        member _.``Invalid Mx Addresses Return None`` () =
            [
                // ❌ 존재하지 않는 장치 또는 문법 오류
                "Invalid123"; "XYZ"; "XG12"; "D..10"; "123"; "M-1"; "P4Z"; "A123"
                "D100.."; "B-12"; "T100.X"; "WXYZ"; "SBG12"; "XZZ"; "ZR.F"
                // ❌ K포맷 비정상: 비정상 접두어 또는 범위 초과
                "K5Z10"; "K0D5"; "K3M-2"; "K4A100"; "K9M0"
                // ❌ 존재하지 않는 조합
                "K2TS2"; "K2CS3"; "K2SD100"
            ]
            |> List.iter (fun addr -> None === MxDeviceInfo.Create(addr))
