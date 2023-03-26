namespace T

open NUnit.Framework
open Dsu.PLC.LS
open FSharpPlus
open AddressConvert
open Engine.Common.FS

[<AutoOpen>]
module XGKTest =
    type XGKTest() =
        inherit PLCTestBase("192.168.0.101")                //xgk or xgbmk
        let conn = base.Conn
        let bitDeviceTypes = [ P; M; L; K; F; T; C; ]       //S Step제어용 디바이스 수집 불가
        let wordDeviceTypes = [D; R; U; T; C]               // '사용설명서_XGK_XGB_명령어집_국문_v2.8.pdf', pp.2-12

        //let testDevice(typ:DeviceType, testBit:bool) =
        //    let strTyp = typ.ToString()
        //    let bits = [
        //        $"%%{strTyp}F",     LsTagAnalysis.Create($"%%{strTyp}F",     typ, DataType.Bit, 15)
        //        $"%%{strTyp}1A",    LsTagAnalysis.Create($"%%{strTyp}1A",    typ, DataType.Bit, 16*1 + 10)
        //        $"%%{strTyp}2A",    LsTagAnalysis.Create($"%%{strTyp}2A",    typ, DataType.Bit, 16*2 + 10)

        //        $"%%{strTyp}00000", LsTagAnalysis.Create($"%%{strTyp}00000", typ, DataType.Bit, 0)
        //        $"%%{strTyp}00001", LsTagAnalysis.Create($"%%{strTyp}00001", typ, DataType.Bit, 1)
        //        $"%%{strTyp}00002", LsTagAnalysis.Create($"%%{strTyp}00002", typ, DataType.Bit, 2)
        //        $"%%{strTyp}00010", LsTagAnalysis.Create($"%%{strTyp}00010", typ, DataType.Bit, 16*1 + 0)
        //        $"%%{strTyp}00011", LsTagAnalysis.Create($"%%{strTyp}00011", typ, DataType.Bit, 16*1 + 1)
        //        $"%%{strTyp}00012", LsTagAnalysis.Create($"%%{strTyp}00012", typ, DataType.Bit, 16*1 + 2)
        //        $"%%{strTyp}00112", LsTagAnalysis.Create($"%%{strTyp}00112", typ, DataType.Bit, 16*11 + 2)
        //        $"%%{strTyp}10112", LsTagAnalysis.Create($"%%{strTyp}10112", typ, DataType.Bit, 16*1011 + 2)
        //        $"%%{strTyp}1011F", LsTagAnalysis.Create($"%%{strTyp}1011F", typ, DataType.Bit, 16*1011 + 15)
        //    ]
        //    let words = [
        //        $"%%{strTyp}0000" , LsTagAnalysis.Create($"%%{strTyp}0000",  typ, DataType.Word, 0)
        //        $"%%{strTyp}0001" , LsTagAnalysis.Create($"%%{strTyp}0001",  typ, DataType.Word, 16*1)
        //        $"%%{strTyp}0002" , LsTagAnalysis.Create($"%%{strTyp}0002",  typ, DataType.Word, 16*2)
        //        $"%%{strTyp}0003" , LsTagAnalysis.Create($"%%{strTyp}0003",  typ, DataType.Word, 16*3)
        //    ]

        //    let testSet = (if testBit then bits else []) @ words

        //    for (tag, answer) in testSet do
        //        let info = getXgkTagInfo tag
        //        if info.IsNone || info.Value <> answer then
        //            noop()

        //        info.Value === answer


        [<Test>]
        member __.``xgk(xgbmk) Connection Test`` () =
            conn.Connect() === true

/////더미 메모리로 값 반환 확인,  메모리별확인, 주소별 확인 (두 type)

//        [<Test>]
//        member __.``xgk LsTagAnalysis Return Test`` () =
//            //let tag = conn.ReadATagUI8("%MW0")
//            //ShouldNotBeNull tag

//            let bitDeviceTypes = [ P; M; L; K; F; T; C; ]       //S Step제어용 디바이스 수집 불가
//            let wordDeviceTypes = [D; R; U; T; C]               // '사용설명서_XGK_XGB_명령어집_국문_v2.8.pdf', pp.2-12

//            for dt in bitDeviceTypes do
//                testDevice(dt, true)
//            for dt in wordDeviceTypes do
//                testDevice(dt, false)

        [<Test>]
        member __.``xgk %PW Read LsTagAnalysis Test`` () =
            let isIEC = true
            let mutable _some : LsTagAnalysis = {Tag = ""; Device = DeviceType.ZR; DataType = DataType.Continuous; BitOffset = 0; IsIEC=isIEC}
            let mutable tag : string = "%MW00"

            tryParseTag CpuType.XgbMk tag |> Option.get |> fun x -> _some <- x
            _some.Tag === "%MW00"
            _some.Device === DeviceType.M
            _some.DataType === DataType.Word
            _some.BitOffset === 0


        //[<Test>]
        //member __.``xgk %PW  Write-Read Test`` () =
        //    let isIEC = true
        //    let mutable _some : LsTagAnalysis = {Tag = ""; Device = DeviceType.ZR; DataType = DataType.Continuous; BitOffset = 0; IsIEC=isIEC}
        //    let mutable tag : string = "%MW00"
        //    conn.WriteRandomTags




