#if X64
namespace Tx64
#else
namespace Tx86
#endif
open T

open NUnit.Framework
open AddressConvert
open Engine.Core.CoreModule
open Engine.Common.FS
open System.Text.RegularExpressions

type XgkAddressTest() =
    inherit TestBaseClass("HWPLCLogger")

    [<Test>]
    member x.``XGK Address parsing test`` () =
        let testDevice (testBitDevice:bool) (typ:DeviceType) =
            //let h = Regex(@"^%([PMLKFTCS])(\d{1,4})([\da-fA-F])$").Match("%P0A")


            let strTyp = typ.ToString()
            let qnas = [
                if typ = DeviceType.D then
                    if testBitDevice then
                        yield $"{strTyp}00000.0", $"%%{strTyp}X00000.0"
                        yield $"{strTyp}00000.1", $"%%{strTyp}X00000.1"
                        yield $"{strTyp}00000.2", $"%%{strTyp}X00000.2"
                        yield $"{strTyp}00001.0", $"%%{strTyp}X00001.0"
                        yield $"{strTyp}00001.1", $"%%{strTyp}X00001.1"
                        yield $"{strTyp}00001.2", $"%%{strTyp}X00001.2"
                        yield $"{strTyp}00011.2", $"%%{strTyp}X00011.2"
                        yield $"{strTyp}01011.2", $"%%{strTyp}X01011.2"
                        yield $"{strTyp}10011.F", $"%%{strTyp}X10011.F"
                
                    yield $"{strTyp}00001" , $"%%{strTyp}W00001"
                    yield $"{strTyp}00002" , $"%%{strTyp}W00002"
                    yield $"{strTyp}00003" , $"%%{strTyp}W00003"
                else if typ = DeviceType.N || typ = DeviceType.L then
                    if testBitDevice then
                        yield $"{strTyp}000000", $"%%{strTyp}X000000"
                        yield $"{strTyp}000001", $"%%{strTyp}X000001"
                        yield $"{strTyp}000002", $"%%{strTyp}X000002"
                        yield $"{strTyp}000010", $"%%{strTyp}X000010"
                        yield $"{strTyp}000011", $"%%{strTyp}X000011"
                        yield $"{strTyp}000012", $"%%{strTyp}X000012"
                        yield $"{strTyp}000112", $"%%{strTyp}X000112"
                        yield $"{strTyp}010112", $"%%{strTyp}X010112"
                        yield $"{strTyp}10011F", $"%%{strTyp}X10011F"
                
                    yield $"{strTyp}00001" , $"%%{strTyp}W00001"
                    yield $"{strTyp}00002" , $"%%{strTyp}W00002"
                    yield $"{strTyp}00003" , $"%%{strTyp}W00003"
                else 
                    if testBitDevice then
                        yield $"{strTyp}00000", $"%%{strTyp}X00000"
                        yield $"{strTyp}00001", $"%%{strTyp}X00001"
                        yield $"{strTyp}00002", $"%%{strTyp}X00002"
                        yield $"{strTyp}00010", $"%%{strTyp}X00010"
                        yield $"{strTyp}00011", $"%%{strTyp}X00011"
                        yield $"{strTyp}00012", $"%%{strTyp}X00012"
                        yield $"{strTyp}00112", $"%%{strTyp}X00112"
                        yield $"{strTyp}10112", $"%%{strTyp}X10112"
                        yield $"{strTyp}1011F", $"%%{strTyp}X1011F"
                
                    yield $"{strTyp}0001" , $"%%{strTyp}W0001"
                    yield $"{strTyp}0002" , $"%%{strTyp}W0002"
                    yield $"{strTyp}0003" , $"%%{strTyp}W0003"
            ]

            for (tag, answer) in qnas do
                let fEnetTag = tryToFEnetTag CpuType.XgbMk tag
                fEnetTag.Value === answer

        let testUDeivce() =
            let qnas = [
                "U0.0", "%UW0"
                "U0.1", "%UW1"
                "U0.31", "%UW31"
                "U1.0", "%UW32"
            ]
            for (tag, answer) in qnas do
                let fEnetTag = tryToFEnetTag CpuType.XgbMk tag
                fEnetTag.Value === answer

            let invalids = [
                //"U0.0.0"            // XG5000 UI 상에서는 지원되지 않고, FEnet 통신으로는 지원됨.
                "U0.32"             // U0.31 에서 끝나고, U1.0 으로 시작해야 함
            ]
            for tag in invalids do
                let fEnetTag = tryToFEnetTag CpuType.XgbMk tag
                fEnetTag.IsNone === true

        let testBitAndWordDevice = testDevice true
        let testWordDevice = testDevice false

        let bitAndWordDeviceTypes = [ P; M; L; K; F; T; C; ]    //S Step제어용 디바이스 수집 불가
        let wordDeviceTypes = [D; R; T; C]                   // U    // '사용설명서_XGK_XGB_명령어집_국문_v2.8.pdf', pp.2-12

        bitAndWordDeviceTypes |> iter testBitAndWordDevice
        wordDeviceTypes |> iter testWordDevice

        testUDeivce()


