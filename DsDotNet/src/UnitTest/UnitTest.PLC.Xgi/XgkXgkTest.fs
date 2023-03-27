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
        let wordDeviceTypes = [D; R; U; T; C; Z]               // '사용설명서_XGK_XGB_명령어집_국문_v2.8.pdf', pp.2-12
        let wordDeviceTypesforWrite = [D; U; T; C; Z]


        let targetAddresses = ["00000"; "00001"; "00002"; "00010"; "00011"; "00011"; "00012"; "10112"; "1011F"; "0000"; "0003"; "0010"; "0100"; ]

        let safeLWordAddresses = [|0..255|]

        let safeDWordAddresses = [| for lw in safeLWordAddresses do yield! [2*lw; 2*lw+1] |]
        let safeWordAddresses =  [| for dw in safeDWordAddresses do yield! [2*dw; 2*dw+1] |]
        //let safeByteAddresses =  [| for w in safeWordAddresses do yield! [w; w+1UL] |]   // [| 0..1023 |]
        

        let testReadDevice(typ:DeviceType) =
            let mutable pass: bool = true
            let testadd = [|0;1;11;35|]
            let strTyp = typ.ToString()
            let safeWordTags = testadd |> Array.map (fun addr -> sprintf "%%%sW%d" strTyp addr)

            try
                for tag in safeWordTags do
                    conn.ReadATag(tag) |> ignore
                    pass <- true //R 읽기도 안됨
            with
                | ex ->
                    ignore ex // 예외 처리 코드
                    pass <- false

            pass === true

        let testReadWriteTargetDevice(typ:DeviceType, testBit:bool) =
            let strTyp = typ.ToString()
            let bits = [
                //$"%%{strTyp}F",     LsTagAnalysis.Create($"%%{strTyp}F",     typ, DataType.Bit, 15)       //정규식 필요
                //$"%%{strTyp}1A",    LsTagAnalysis.Create($"%%{strTyp}1A",    typ, DataType.Bit, 16*1 + 10)
                //$"%%{strTyp}2A",    LsTagAnalysis.Create($"%%{strTyp}2A",    typ, DataType.Bit, 16*2 + 10)

                $"%%{strTyp}00000", testNum
                $"%%{strTyp}00001", testNum
                $"%%{strTyp}00002", testNum
                $"%%{strTyp}00010", testNum
                $"%%{strTyp}00011", testNum
                $"%%{strTyp}00012", testNum
                $"%%{strTyp}00112", testNum
                $"%%{strTyp}10112", testNum
                $"%%{strTyp}1011F", testNum
            ]
            let words = [
                $"%%{strTyp}0000" , testNum
                $"%%{strTyp}0001" , testNum
                $"%%{strTyp}0002" , testNum
                $"%%{strTyp}0003" , testNum
            ]

            let testSet = (if testBit then bits else []) @ words
            let strings = testSet |> List.map (fun (s, i) -> s)|> Array.ofList
            let lsTags = strings |> map (fun t -> LsTagXgk(conn, t) :> LsTag)
            lsTags |> iter (fun t -> t.Value <- testNum)
            conn.WriteRandomTags lsTags |> ignore

            for (tag, answer) in testSet do
                let info = conn.ReadATag(tag)
                if info <> answer then
                    noop()
                info === answer


        let testWord0BitDevice(typ:DeviceType) = 
            let strTyp = typ.ToString()
            let safeWordTags = bitAddresses |> Array.map (fun addr -> sprintf "%%%s%X" strTyp addr)
            let lsTags = safeWordTags |> map (fun t -> LsTagXgk(conn, t) :> LsTag)
            lsTags |> iter (fun t -> t.Value <- true)
            conn.WriteRandomTags lsTags |> ignore
            for (tag) in safeWordTags do
                let info = conn.ReadATag(tag)
                if info <> true then
                    noop()
                info === true
            lsTags |> iter (fun t -> t.Value <- false)
            conn.WriteRandomTags lsTags |> ignore
            for (tag) in safeWordTags do
                let info = conn.ReadATag(tag)
                if info <> false then
                    noop()
                info === false


        let testWord1to4BitDevice(typ:DeviceType, bitAddress:int) = 
            let testWords = word1Addresses @ word2Addresses @ word3Addresses @ word4Addresses
            let strTyp = typ.ToString()
            let safeWordTags = testWords |> Array.map (fun addr -> sprintf "%%%s%d%X" strTyp addr bitAddress)
            let lsTags = safeWordTags |> map (fun t -> LsTagXgk(conn, t) :> LsTag)
            lsTags |> iter (fun t -> t.Value <- true)
            conn.WriteRandomTags lsTags |> ignore
            for (tag) in safeWordTags do
                let info = conn.ReadATag(tag)
                if info <> true then
                    noop()
                info === true
            lsTags |> iter (fun t -> t.Value <- false)
            conn.WriteRandomTags lsTags |> ignore
            for (tag) in safeWordTags do
                let info = conn.ReadATag(tag)
                if info <> false then
                    noop()
                info === false

        //let testType3Device(typ:DeviceType, bitAddress:string) = 
        //    let strTyp = typ.ToString()
        //    let safeWordTags = word4Addresses |> Array.map (fun addr -> sprintf "%%%s%s%X" strTyp addr bitAddress)
        //    let lsTags = safeWordTags |> map (fun t -> LsTagXgk(conn, t) :> LsTag)
        //    lsTags |> iter (fun t -> t.Value <- 1us)
        //    conn.WriteRandomTags lsTags |> ignore
        //    for (tag) in safeWordTags do
        //        let info = conn.ReadATag(tag)
        //        if info <> 1us then
        //            noop()
        //        info === 1us
        //    lsTags |> iter (fun t -> t.Value <- 0us)
        //    conn.WriteRandomTags lsTags |> ignore
        //    for (tag) in safeWordTags do
        //        let info = conn.ReadATag(tag)
        //        if info <> 0us then
        //            noop()
        //        info === 0us

        let testOnlyWordDevice(typ:DeviceType) = 
            let testWords = word1Addresses @ word2Addresses @ word3Addresses @ word4Addresses
            let strTyp = typ.ToString()
            let safeWordTags = testWords |> Array.map (fun addr -> sprintf "%%%s%d" strTyp addr)
            let lsTags = safeWordTags |> map (fun t -> LsTagXgk(conn, t) :> LsTag)
            lsTags |> iter (fun t -> t.Value <- testNum)
            conn.WriteRandomTags lsTags |> ignore
            for (tag) in safeWordTags do
                let info = conn.ReadATag(tag)
                if info <> testNum then
                    noop()
                info === testNum
            lsTags |> iter (fun t -> t.Value <- 0us)
            conn.WriteRandomTags lsTags |> ignore
            for (tag) in safeWordTags do
                let info = conn.ReadATag(tag)
                if info <> 0us then
                    noop()
                info === 0us

        let testXWordBitDevice(typ: DeviceType, bitAddr: int) = 
            let testWords = word1Addresses @ word2Addresses @ word3Addresses @ word4Addresses
            let strTyp = typ.ToString()
            let safeWordTags = testWords |> Array.map (fun addr -> sprintf "%%%sX%04d%X" strTyp addr bitAddr)
            let lsTags = safeWordTags |> map (fun t -> LsTagXgk(conn, t) :> LsTag)
            lsTags |> iter (fun t -> t.Value <- true)
            conn.WriteRandomTags lsTags |> ignore
            for (tag) in safeWordTags do
                let info = conn.ReadATag(tag)
                if info <> true then
                    noop()
                info === true
            lsTags |> iter (fun t -> t.Value <- false)
            conn.WriteRandomTags lsTags |> ignore
            for (tag) in safeWordTags do
                let info = conn.ReadATag(tag)
                if info <> false then
                    noop()
                info === false



        let testBWDLWordDevice(typ:DeviceType, word: string) =
            let testWords = word1Addresses @ word2Addresses @ word3Addresses @ word4Addresses
            let strTyp = typ.ToString()
            
            let safeWordTags = testWords |> Array.map (fun addr -> sprintf "%%%s%s%d" strTyp word addr)


            let lsTags = safeWordTags |> map (fun t -> LsTagXgk(conn, t) :> LsTag)
            lsTags |> iter (fun t -> t.Value <- testNum)
            conn.WriteRandomTags lsTags |> ignore
            for tag in safeWordTags do
                let info = conn.ReadATag(tag)
                let answer = testNum
                if info <> answer then
                    noop()
                
            lsTags |> iter (fun t -> t.Value <- 0us)
            conn.WriteRandomTags lsTags |> ignore
            for tag in safeWordTags do
                let info = conn.ReadATag(tag)
                let answer = 0us
                if info <> answer then
                    noop()
                info === answer




        let testWriteDevice(typ:DeviceType, testBit:bool) =
            let safeLWriteAddresses = [|0..4|] @[|121|]
            let strTyp = typ.ToString()

            let safeWordTags = safeLWriteAddresses |> Array.map (fun addr -> sprintf "%%%sW%d" strTyp addr)


            let lsTags = safeWordTags |> map (fun t -> LsTagXgk(conn, t) :> LsTag)
            lsTags |> iter (fun t -> t.Value <- testNum)
            conn.WriteRandomTags lsTags |> ignore
            for tag in safeWordTags do
                let info = conn.ReadATag(tag)
                let answer = testNum
                if info <> answer then
                    noop()
                info === answer
            lsTags |> iter (fun t -> t.Value <- 0us)
            conn.WriteRandomTags lsTags |> ignore
            for tag in safeWordTags do
                let info = conn.ReadATag(tag)
                let answer = 0us
                if info <> answer then
                    noop()
                info === answer


        [<Test>]
        member __.``xgk(xgbmk) Connection Test`` () =
            conn.Connect() === true

        
        [<Test>]
        member __.``xgk(xgbmk) word0bit test`` () =
            for dt in bitDeviceTypes do
                testWord0BitDevice(dt) 

        [<Test>]
        member __.``xgk(xgbmk) word1to4bit test`` () =
            for dt in bitDeviceTypes do
                for ba in bitAddresses do
                    testWord1to4BitDevice(dt,ba) 


        [<Test>]
        member __.``xgk(xgbmk) only word1to4 test`` () =
            for dt in fullDeviceTypes do
                testOnlyWordDevice(dt) 

        [<Test>]
        member __.``xgk(xgbmk) only word1to4 test without R`` () =
            for dt in fullDeviceTypesWithoutR do
                testOnlyWordDevice(dt)

        [<Test>]
        member __.``xgk(xgbmk) X word bit test`` () =
            for dt in fullDeviceTypes do
                for ba in bitAddresses do
                    testXWordBitDevice(dt,ba)     
        
        [<Test>]
        member __.``xgk(xgbmk) X word bit test without R`` () =
            for dt in fullDeviceTypesWithoutR do
                for ba in bitAddresses do
                    testXWordBitDevice(dt,ba) 

        [<Test>]
        member __.``xgk(xgbmk) X word bit test without R and U`` () =
            for dt in fullDeviceTypesWithoutRUFTCZ do
                for ba in bitAddresses do
                    testXWordBitDevice(dt,ba) 


        [<Test>]
        member __.``xgk BWDL word Test`` () =
            for dt in wordDeviceTypes do
                //testBWDLWordDevice(dt, "B");         
                testBWDLWordDevice(dt, "W");         
                //testBWDLWordDevice(dt, "D");         
                //testBWDLWordDevice(dt, "L");         

        [<Test>]
        member __.``xgk BWDL word Test withoutR`` () =
            for dt in wordDeviceTypesWithoutR do
                //testBWDLWordDevice(dt, "B");         
                testBWDLWordDevice(dt, "W");         
                //testBWDLWordDevice(dt, "D");         
                //testBWDLWordDevice(dt, "L");  

        //[<Test>]
        //member __.``xgk BWDL word Test withoutR and U`` () =
        //    for dt in fullDeviceTypesWithoutR do
        //        testBWDLWordDevice(dt, "B");         
        //        testBWDLWordDevice(dt, "W");         
        //        testBWDLWordDevice(dt, "D");         
        //        testBWDLWordDevice(dt, "L"); 


        //[<Test>]
        //member __.``xgk(xgbmk) BWDL word bit test`` () =
        //    for dt in bitDeviceTypes do
        //            testBWDLWordBitDevice(dt) 


        //[<Test>]
        //member __.``xgk(xgbmk) word3 test`` () =
        //    for dt in bitDeviceTypes do
        //        testType4Device(dt) 

        //[<Test>]
        //member __.``xgk(xgbmk) word4 test`` () =
        //    for dt in bitDeviceTypes do
        //        testType4Device(dt) 


        //[<Test>]
        //member __.``xgk(xgbmk) word2 test`` () =
        //        for ba in bitAddresses do
        //            testType3Device(dt,ba) 


        [<Test>]
        member __.``xgk %PW Read Test`` () =
            for dt in bitDeviceTypes do
                testReadDevice(dt)
            for dt in wordDeviceTypes do
                testReadDevice(dt)




        [<Test>]
        member __.``xgk PLC LsTagAnalysis Return Test`` () =
            for dt in bitDeviceTypes do
                testReadWriteTargetDevice(dt, true)
            for dt in wordDeviceTypesforWrite do
                testReadWriteTargetDevice(dt, false)




        [<Test>]
        member __.``xgk PLC Write-Read Test`` () =
            //for dt in bitDeviceTypes do
            //    testWriteDevice(dt, true)
            for dt in wordDeviceTypes do
                testWriteDevice(dt, false)            

        [<Test>]
        member __.``xgk PLC Write-Read without R Test`` () =
            //for dt in bitDeviceTypes do
            //    testWriteDevice(dt, true)
            for dt in wordDeviceTypesWithoutR do
                testWriteDevice(dt, false)

           

        [<Test>]
        member __.``xgk PLC LsTagAnalysis Return Test`` () =
            for dt in bitDeviceTypes do
                testReadWriteTargetDevice(dt, true)
            for dt in wordDeviceTypesWithoutR do
                testReadWriteTargetDevice(dt, false)
