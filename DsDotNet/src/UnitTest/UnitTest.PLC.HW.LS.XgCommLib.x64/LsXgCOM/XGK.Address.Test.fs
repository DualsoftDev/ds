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
        let testDevice(typ:DeviceType, testBit:bool) =
            //let h = Regex(@"^%([PMLKFTCS])(\d{1,4})([\da-fA-F])$").Match("%P0A")


            let strTyp = typ.ToString()
            let bits = [
                $"%%{strTyp}00000", LsFEnetTagInfo.Create($"%%{strTyp}00000", typ, DataType.Bit, 0)
                $"%%{strTyp}00001", LsFEnetTagInfo.Create($"%%{strTyp}00001", typ, DataType.Bit, 1)
                $"%%{strTyp}00002", LsFEnetTagInfo.Create($"%%{strTyp}00002", typ, DataType.Bit, 2)
                $"%%{strTyp}00010", LsFEnetTagInfo.Create($"%%{strTyp}00010", typ, DataType.Bit, 16*1 + 0)
                $"%%{strTyp}00011", LsFEnetTagInfo.Create($"%%{strTyp}00011", typ, DataType.Bit, 16*1 + 1)
                $"%%{strTyp}00012", LsFEnetTagInfo.Create($"%%{strTyp}00012", typ, DataType.Bit, 16*1 + 2)
                $"%%{strTyp}00112", LsFEnetTagInfo.Create($"%%{strTyp}00112", typ, DataType.Bit, 16*11 + 2)
                $"%%{strTyp}10112", LsFEnetTagInfo.Create($"%%{strTyp}10112", typ, DataType.Bit, 16*1011 + 2)
                $"%%{strTyp}1011F", LsFEnetTagInfo.Create($"%%{strTyp}1011F", typ, DataType.Bit, 16*1011 + 15)
            ]
            let words = [
                $"%%{strTyp}0000" , LsFEnetTagInfo.Create($"%%{strTyp}0000",  typ, DataType.Word, 0)
                $"%%{strTyp}0001" , LsFEnetTagInfo.Create($"%%{strTyp}0001",  typ, DataType.Word, 16*1)
                $"%%{strTyp}0002" , LsFEnetTagInfo.Create($"%%{strTyp}0002",  typ, DataType.Word, 16*2)
                $"%%{strTyp}0003" , LsFEnetTagInfo.Create($"%%{strTyp}0003",  typ, DataType.Word, 16*3)
            ]

            let testSet = (if testBit then bits else []) @ words

            for (tag, answer) in testSet do
                let info = tryParseTag tag
                if info.IsNone || info.Value <> answer then
                    noop()

                info.Value === answer

        let bitDeviceTypes = [ P; M; L; K; F; T; C; ]       //S Step제어용 디바이스 수집 불가
        let wordDeviceTypes = [D; R; U; T; C]               // '사용설명서_XGK_XGB_명령어집_국문_v2.8.pdf', pp.2-12

        for dt in bitDeviceTypes do
            testDevice(dt, true)
        for dt in wordDeviceTypes do
            testDevice(dt, false)

