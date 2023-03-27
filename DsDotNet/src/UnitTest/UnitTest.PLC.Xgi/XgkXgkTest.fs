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

        let errAddresses = [|1;2;12;301;999|]
        let wordAddresses = [|0001;0014;0301;1002|]
        let bitAddresses = [|3;7;0xA;0xF|]


        

        let testBitRead(typ:DeviceType, addresses:int[], addBit:int) =
            let mutable pass: bool = false
            let strTyp = typ.ToString()
            let safeWordTags = addresses |> Array.map (fun addr -> sprintf "%%%s%d%X" strTyp addr addBit)
            try
                for tag in safeWordTags do
                    conn.ReadATag(tag) |> ignore
                    pass <- true
            with
                | ex ->
                    ignore ex // 예외 처리 코드
                    pass <- false
            pass === true

        let testBitRW(typ:DeviceType, addresses:int[], addBit:int) = 
            let strTyp = typ.ToString()
            let safeWordTags = addresses |> Array.map (fun addr -> sprintf "%%%s%d%X" strTyp addr addBit)
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


        let testWordRead(typ:DeviceType, addresses:int[]) = 
            let mutable pass: bool = false
            let strTyp = typ.ToString()
            let safeWordTags = addresses |> Array.map (fun addr -> sprintf "%%%s%d" strTyp addr)
            try
                for tag in safeWordTags do
                    conn.ReadATag(tag) |> ignore
                    pass <- true
            with
                | ex ->
                    ignore ex // 예외 처리 코드
                    pass <- false
            pass === true
        

        let testWordRW(typ:DeviceType, addresses:int[], value: int16) = 
            let strTyp = typ.ToString()
            let safeWordTags = addresses |> Array.map (fun addr -> sprintf "%%%s%d" strTyp addr)
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

        
