namespace T

open NUnit.Framework
open Dsu.PLC.LS
open FSharpPlus
open AddressConvert

[<AutoOpen>]
module XGKTest = 
    type XGKTest() = 
        inherit PLCTestBase("192.168.0.101")
        let conn = base.Conn

        let mem = ["P";"M";"L";"K";"F";"S";"D";"U";"N";"Z";"T";"C";"R"]

        let safeLWordAddresses = [|0..255|]
        let safeDWordAddresses = [| for lw in safeLWordAddresses do yield! [2*lw; 2*lw+1] |]
        let safeWordAddresses =  [| for dw in safeDWordAddresses do yield! [2*dw; 2*dw+1] |]


        [<Test>]
        member __.``xgk(xgbmk) Connection Test`` () =
            conn.Connect() === true

///더미 메모리로 값 반환 확인,  메모리별확인, 주소별 확인 (두 type) 

        [<Test>]
        member __.``xgk MW0 value return Test`` () =
            let tag = conn.ReadATagUI8("%MW0")
            ShouldNotBeNull tag

        [<Test>]
        member __.``xgk %PW  Write-Read Test`` () =
            let mutable _some : LsTagAnalysis = {Tag = ""; Device = DeviceType.ZR; DataType = DataType.Continuous; BitOffset = 0}
            //write

            //read
            let mutable tag : string = "%MW00"
            tryParseTag CpuType.XgbMk tag |> Option.get |> fun x -> _some <- x
            _some.Tag === "%MW00"
            _some.Device === DeviceType.M
            _some.DataType === DataType.Word
            _some.BitOffset === 0
              
            
           
            




//[<AutoOpen>]
//module PLCTestsingleXgk =
//    type PLCTestBase() =
//        let ip = "192.168.0.101"
//        //let conn : LsConnection = new LsConnection(LsConnectionParameters(ip))
//        [<SetUp>]
//        member X.Setup() =
//            let conn : LsConnection = new LsConnection(LsConnectionParameters(ip)) |> ignore
            
            
//        [<TearDown>]
//        member X.TearDown() =
//            conn.Disconnect() |> ignore
            
//        [<Test>]
//        member __.``xgk(xgbmk) Connection Test`` () =
//            conn.Connect() === true   

//        [<Test>]
//        member __.``xgk(xgbmk) return some test`` () =
//            ignore
            
