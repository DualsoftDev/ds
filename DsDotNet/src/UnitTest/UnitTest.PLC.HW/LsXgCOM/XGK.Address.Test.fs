namespace T

open NUnit.Framework
open AddressConvert
open Engine.Core.CoreModule
open Engine.Common.FS
open System.Text.RegularExpressions

type XgkAddressTest() =
    inherit TestBaseClass("HWPLCLogger")

    [<Test>]
    member x.``test`` () =
        let testDevice(typ:DeviceType, testBit:bool) =
            let a = Regex(@"(\d{1,3})(\d)").Match("11")
            let b = Regex(@"(\d{1,3})(\d)").Match("X1111")
            let b2 = Regex(@"^(\d{1,3})(\d)$").Match("X1111")
            let c = Regex(@"(\d{1,3})([\da-fA-F])").Match("XX1111")
            let d = Regex(@"^(\d{1,3})([\da-fA-F])$").Match("111A")
            let e = Regex(@"([PMLKFTCS])(\d{1,3})([\da-fA-F])$").Match("P111A")
            let f = Regex(@"^([PMLKFTCS])(\d{1,3})([\da-fA-F])$").Match("P111A")
            let g = Regex(@"^([PMLKFTCS])(\d{1,3})([\da-fA-F])$").Match("P0A")

            match "P0A" with
                | RegexPattern @"^([PMLKFTCS])(\d)([\da-fA-F])$" [ _; DevicePattern device; Int32Pattern wordOffset; HexPattern bitOffset] ->
                    noop()
                | RegexPattern @"^([PMLKFTCS])(\d{1,4})([\da-fA-F])$" [ _; DevicePattern device; Int32Pattern wordOffset; HexPattern bitOffset] ->
                    noop()
                | _ ->
                    ()




            let strTyp = typ.ToString()
            let bits = [
                //$"%%{strTyp}0",     LsTagAnalysis.Create($"%%{strTyp}0",     typ, DataType.Bit, 0)
                //$"%%{strTyp}1",     LsTagAnalysis.Create($"%%{strTyp}1",     typ, DataType.Bit, 1)
                //$"%%{strTyp}8",     LsTagAnalysis.Create($"%%{strTyp}8",     typ, DataType.Bit, 8)
                //$"%%{strTyp}A",     LsTagAnalysis.Create($"%%{strTyp}A",     typ, DataType.Bit, 10)
                //$"%%{strTyp}F",     LsTagAnalysis.Create($"%%{strTyp}F",     typ, DataType.Bit, 15)
                $"%%{strTyp}1A",    LsTagAnalysis.Create($"%%{strTyp}1A",    typ, DataType.Bit, 16*1 + 10)
                $"%%{strTyp}2A",    LsTagAnalysis.Create($"%%{strTyp}2A",    typ, DataType.Bit, 16*2 + 10)

                $"%%{strTyp}00000", LsTagAnalysis.Create($"%%{strTyp}00000", typ, DataType.Bit, 0)
                $"%%{strTyp}00001", LsTagAnalysis.Create($"%%{strTyp}00001", typ, DataType.Bit, 1)
                $"%%{strTyp}00002", LsTagAnalysis.Create($"%%{strTyp}00002", typ, DataType.Bit, 2)
                $"%%{strTyp}00010", LsTagAnalysis.Create($"%%{strTyp}00010", typ, DataType.Bit, 16*1 + 0)
                $"%%{strTyp}00011", LsTagAnalysis.Create($"%%{strTyp}00011", typ, DataType.Bit, 16*1 + 1)
                $"%%{strTyp}00012", LsTagAnalysis.Create($"%%{strTyp}00012", typ, DataType.Bit, 16*1 + 2)
                $"%%{strTyp}00112", LsTagAnalysis.Create($"%%{strTyp}00112", typ, DataType.Bit, 16*11 + 2)
                $"%%{strTyp}10112", LsTagAnalysis.Create($"%%{strTyp}10112", typ, DataType.Bit, 16*1011 + 2)
                $"%%{strTyp}1011F", LsTagAnalysis.Create($"%%{strTyp}1011F", typ, DataType.Bit, 16*1011 + 15)
            ]
            let words = [
                $"%%{strTyp}0000" , LsTagAnalysis.Create($"%%{strTyp}0000",  typ, DataType.Word, 0)
                $"%%{strTyp}0001" , LsTagAnalysis.Create($"%%{strTyp}0001",  typ, DataType.Word, 16*1)
                $"%%{strTyp}0002" , LsTagAnalysis.Create($"%%{strTyp}0002",  typ, DataType.Word, 16*2)
                $"%%{strTyp}0003" , LsTagAnalysis.Create($"%%{strTyp}0003",  typ, DataType.Word, 16*3)
            ]

            let testSet = (if testBit then bits else []) @ words

            for (tag, answer) in testSet do
                let info = getXgkTagInfo tag
                if info.IsNone || info.Value <> answer then
                    noop()

                info.Value === answer

        let bitDeviceTypes = [ P; M; L; K; F; T; C; ]       //S Step제어용 디바이스 수집 불가
        let wordDeviceTypes = [D; R; U; T; C]               // '사용설명서_XGK_XGB_명령어집_국문_v2.8.pdf', pp.2-12

        for dt in bitDeviceTypes do
            testDevice(dt, true)
        for dt in wordDeviceTypes do
            testDevice(dt, false)

