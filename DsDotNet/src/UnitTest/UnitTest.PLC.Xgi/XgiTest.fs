namespace T

open NUnit.Framework
open Dsu.PLC.LS
open FSharpPlus
open AddressConvert
open Engine.Common.FS


[<AutoOpen>]
module XGITest =
    type XGITest() =
        inherit PLCTestBase("192.168.0.100")                //xgi
        let conn = base.Conn

        
        let bitDeviceTypes = [ I;Q;]        
        let specialDeviceTypes = [U;]
        let wordDeviceTypes = [M; L; N; K; R; A; W; F;]


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
        


        let testWordRW(typ:DeviceType, addresses:int[], value: uint16, forceCurAddr: bool) = 
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
        member __.``Xgi Connection Test`` () =
            conn.Connect() === true

        [<Test>]
        member __.``xgi a word read`` () =
            let mutable pass = false;
            
            try
                conn.ReadATag("%MW4") |> ignore
                pass <- true
            with
                | ex ->
                    ignore ex // 예외 처리 코드
                    pass <- false
            pass === true


        [<Test>]
        member __.``xgi a bit read`` () =
            let mutable pass = false;
            
            try
                conn.ReadATag("%MX123") |> ignore
                pass <- true
            with
                | ex ->
                    ignore ex // 예외 처리 코드
                    pass <- false
            pass === true




        [<Test>]
        member __.``xgi read-write bit test fail`` () =
            for dt in bitDeviceTypes do
                for ba in bitAddresses do
                    (fun () -> testBitRW(dt, shortAddresses, ba, false) ) |> ShouldFailWithSubstringT "option"  
                    //System.Exception : Exception messsage match failed on System.ArgumentException: 옵션 값이 None입니다. (Parameter 'option')
                    
        [<Test>]
        member __.``xgi read-write bit test success`` () =
            for dt in bitDeviceTypes do
                for ba in bitAddresses do
                    //testBitRW(dt, fullAddresses, ba, true) 
                    testBitRW(dt, shortAddresses, ba, true) 
                    
        [<Test>]
        member __.``xgi read-write word test fail`` () =
            for dt in bitDeviceTypes do
                for ba in bitAddresses do
                    (fun () -> testWordRW(dt, shortAddresses, 13us , false) ) |> ShouldFailWithSubstringT "option"  
                    //System.Exception : Exception messsage match failed on System.ArgumentException: 옵션 값이 None입니다. (Parameter 'option')
                    
        [<Test>]
        member __.``xgi read-write word test success`` () =
            for dt in bitDeviceTypes do
                for ba in bitAddresses do
                    testWordRW(dt, fullAddresses, 13us, true) 
                    testWordRW(dt, shortAddresses, 13us, true) 