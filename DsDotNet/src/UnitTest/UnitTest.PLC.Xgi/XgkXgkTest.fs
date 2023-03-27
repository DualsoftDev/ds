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

        let bitDeviceTypes = [ P; M; L; K; F;]       //S Step제어용 디바이스 수집 불가    //T C 불가
        let wordDeviceTypes = [D; R; U; T; C; T; Z]               // '사용설명서_XGK_XGB_명령어집_국문_v2.8.pdf', pp.2-12
        let wordDeviceTypesforWrite = [D; U; T; C; Z]

        let shortAddresses = [|1;2;12;301;999|]
        let fullAddresses = [|1001;1014;|]
        let bitAddresses = [|3;7;0xA;0xF|]


        let testBitRead(typ:DeviceType, addresses:int[], addBit:int, forceCurAddr: bool) =
            let mutable pass: bool = false
            let strTyp = typ.ToString()

            let safeWordTags = 
                if forceCurAddr then
                    addresses |> Array.map (fun addr -> sprintf "%%%s%04d%X" strTyp addr addBit)
                else
                    addresses |> Array.map (fun addr -> sprintf "%%%s%d%X" strTyp addr addBit)
            try
                for tag in safeWordTags do
                    conn.ReadATag(tag) |> ignore
                    pass <- true
            with
                | ex ->
                    ignore ex // 예외 처리 코드
                    pass <- false
            pass === true

        let testBitRW(typ:DeviceType, addresses:int[], addBit:int, forceCurAddr: bool) =
            let strTyp = typ.ToString()
            let safeWordTags = 
                if forceCurAddr then
                    addresses |> Array.map (fun addr -> sprintf "%%%s%04d%X" strTyp addr addBit)
                else
                    addresses |> Array.map (fun addr -> sprintf "%%%s%d%X" strTyp addr addBit)
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


        let testWordRead(typ:DeviceType, addresses:int[], forceCurAddr: bool) =
            let mutable pass: bool = false
            let strTyp = typ.ToString()
            let safeWordTags =
                if forceCurAddr then
                    addresses |> Array.map (fun addr -> sprintf "%%%s%04d" strTyp addr)
                else
                    addresses |> Array.map (fun addr -> sprintf "%%%s%d" strTyp addr)
            try
                for tag in safeWordTags do
                    conn.ReadATag(tag) |> ignore
                    pass <- true
            with
                | ex ->
                    ignore ex // 예외 처리 코드
                    pass <- false
            pass === true
        


        let testWordRW(typ:DeviceType, addresses:int[], value: int16, forceCurAddr: bool) = 
            let strTyp = typ.ToString()
            let safeWordTags = 
                if forceCurAddr then
                    addresses |> Array.map (fun addr -> sprintf "%%%s%04d" strTyp addr)
                else
                    addresses |> Array.map (fun addr -> sprintf "%%%s%d" strTyp addr)

            let lsTags = safeWordTags |> map (fun t -> LsTagXgk(conn, t) :> LsTag)
            lsTags |> iter (fun t -> t.Value <- value)
            conn.WriteRandomTags lsTags |> ignore
            for (tag) in safeWordTags do
                let info = conn.ReadATag(tag)
                if info <> value then
                    noop()
                info === value
            lsTags |> iter (fun t -> t.Value <- 0us)
            conn.WriteRandomTags lsTags |> ignore
            for (tag) in safeWordTags do
                let info = conn.ReadATag(tag)
                if info <> 0us then
                    noop()
                info === 0us

        [<Test>]
        member __.``xgk(xgbmk) Connection Test`` () =
            conn.Connect() === true

        [<Test>]
        member __.``xgk(xgbmk) read bit test fail`` () =
            for dt in bitDeviceTypes do
                for ba in bitAddresses do
                    (fun () -> testBitRead(dt, shortAddresses, ba, false) ) |> ShouldFailWithSubstringT "Equals false"  
                    
        [<Test>]
        member __.``xgk(xgbmk) read bit test success`` () =
            for dt in bitDeviceTypes do
                for ba in bitAddresses do
                    testBitRead(dt, fullAddresses, ba, true) 
                    testBitRead(dt, shortAddresses, ba, true)       

        [<Test>]
        member __.``xgk(xgbmk) read word test fail`` () =
            for dt in bitDeviceTypes do
                (fun () -> testWordRead(dt, shortAddresses, false) ) |> ShouldFailWithSubstringT "Equals false"  
                    
        [<Test>]
        member __.``xgk(xgbmk) read word test success`` () =
            for dt in bitDeviceTypes do
                testWordRead(dt, fullAddresses, true)
                testWordRead(dt, shortAddresses, true)
 
 
        [<Test>]
        member __.``xgk(xgbmk) a word read`` () =
            let mutable pass = false;
            
            try
                conn.ReadATag("%M0033") |> ignore
                pass <- true
            with
                | ex ->
                    ignore ex // 예외 처리 코드
                    pass <- false
            pass === true


        [<Test>]
        member __.``xgk(xgbmk) a bit read`` () =
            let mutable pass = false;
            
            try
                conn.ReadATag("%M0033B") |> ignore
                pass <- true
            with
                | ex ->
                    ignore ex // 예외 처리 코드
                    pass <- false
            pass === true