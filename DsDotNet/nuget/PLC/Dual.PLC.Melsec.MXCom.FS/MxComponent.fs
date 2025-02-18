namespace DsMxComm

open System
open System.Diagnostics
open System.Text
open System.Threading
open System.Timers
open ActUtlType64Lib
open ActProgType64Lib



type PlcMxComponent(hostAddress: string, portNumber: int, cpuName:string) =
    let mutable errorCode = 0
    let mutable errorMessage = ""
    let mutable connected = false
    //let actProgType = ActUtlType64Lib.ActUtlType64Class()
    let actProgType = ActProgType64Lib.ActProgType64Class()


     /// 오류 코드에 대한 메시지 매핑
    let errorMessages = dict [
        0xF0000001, "라이선스 없음 에러: PC에 라이선스가 부여되지 않았습니다. 라이선스 키 FD에서 PC에 라이선스를 부여하십시오."
        0xF0000002, "설정 데이터 읽기 에러: 논리 국번의 설정 데이터 읽기에 실패하였습니다. 올바른 논리 국번을 지정하거나 통신 설정 유틸리티에서 논리 국번을 설정하십시오."
        0xF0000003, "오픈 완료 에러: 오픈 상태에서 Open 메소드를 실행하였습니다. 통신 대상 CPU를 변경하는 경우 Close 후 Open 메소드를 실행하십시오."
        0xF0000004, "미오픈 에러: Open 메소드를 실행하지 않았습니다. Open 메소드 실행 후 해당 메소드를 실행하십시오."
        0xF0000005, "초기화 에러: MX Component 내부 유지 오브젝트의 초기화에 실패하였습니다. 프로그램을 종료하고 PC를 재기동하거나, MX Component를 재설치하십시오."
        0xF0000006, "메모리 확보 에러: MX Component 내부 메모리 확보에 실패하였습니다. 프로그램을 종료하고 PC를 재기동하거나, 다른 프로그램을 종료하여 사용 가능한 메모리를 확보하십시오."
        0x01801006, "Specified module error"
        0xF1000020, "GX Simulator3 did not start error"
        
    ]
    do 
        
        let a = actProgType.ActBaudRate
        let a =  actProgType.ActConnectUnitNumber
        let a =  actProgType.ActControl
        let a =  actProgType.ActCpuTimeOut
        let a =  actProgType.ActDataBits
        let a =  actProgType.ActDestinationIONumber
        let a =  actProgType.ActDestinationPortNumber
        let a =  actProgType.ActDidPropertyBit
        let a =  actProgType.ActDsidPropertyBit
        let a =  actProgType.ActHostAddress: string
        let a =  actProgType.ActIONumber
        let a =  actProgType.ActIntelligentPreferenceBit
        let a =  actProgType.ActMultiDropChannelNumber
        let a =  actProgType.ActNetworkNumber
        let a =  actProgType.ActPacketType
        let a =  actProgType.ActParity
        let a =  actProgType.ActPassword: string
        let a =  actProgType.ActPortNumber
        let a =  actProgType.ActProtocolType
        let a =  actProgType.ActSourceNetworkNumber
        let a =  actProgType.ActSourceStationNumber
        let a =  actProgType.ActStationNumber
        let a =  actProgType.ActStopBits
        let a =  actProgType.ActSumCheck
        let a =  actProgType.ActTargetSimulator
        let a =  actProgType.ActThroughNetworkType
        let a =  actProgType.ActTimeOut
        let a =  actProgType.ActUnitNumber
        let a =  actProgType.ActUnitType

            // MX Component version4 プログラミングマニュアル
        // 4.3.7 接続局がEthernet ポート内蔵QCPU のEthernet 通信（TCP）
        //ActCpuType := $90;          // Q03UDECPU
        //ActDestinationIONumber := 0;// 固定
        //ActDestinationPortNumber := 5007; // TCP
        //ActDidPropertyBit := 1;
        //ActDsidPropertyBit := 1;
        //ActHostAddress := '169.254.251.71';
        //ActIntelligentPreferenceBit := 0; //固定
        //ActIONumber := 1023; //シングルＣＰＵ時固定
        //ActMultiDropChannelNumber := 0; //固定
        //ActNetworkNumber := 0;  // 固定（自局）
        //ActPassword := '';
        //ActProtocolType := 5;   // PROTOCOL_TCPIP
 
        //ActStationNumber := 255;// 固定（自局）
        //ActThroughNetworkType := 0;
        //ActTimeOut := 500;      // ms単位でユーザ任意
        //ActUnitNumber := 0;//
        //ActUnitType := $2C;      // UNIT_QNETHER

        //PortNumber=5500+ System No. x 10+ PLC No
        //<Example> For System No. = 1, PLC No = 1
        //5511=5500+1 x 10+1
        //actProgType.ActHostAddress <- "127.0.0.1"
        //actProgType.ActLogicalStationNumber <- 0
        //actProgType.
        actProgType.ActCpuType <- MxTypeModule.CpuTypeMap[cpuName]   // CPU 타입 설정 (예: Q03UDVCPU = 209)
        //actProgType.ActPortNumber <- 5511
        //actProgType.ActStationNumber <- 0
        actProgType.ActUnitType <- (int)UnitType.UNIT_SIMULATOR3
        actProgType.ActNetworkNumber<- 0;  // 固定（自局）
        actProgType.ActProtocolType <-6;   // PROTOCOL_SHAREDMEMORY
        actProgType.ActStationNumber <-255;// 固定（自局）
        actProgType.ActTimeOut <- 500;      // ms単位でユーザ任意

        //actProgType.ActTargetSimulator <- 0//0x01
        //actProgType.ActHostAddress <- hostAddress
        //actProgType.ActPortNumber <- portNumber
        //actProgType.ActControl <- 1
        ()

            /// PLC 연결 설정 및 초기화
    //member x.Configure(cpuType: int, protocolType: byte, unitType: byte) =
    //    actProgType.ActCpuType <- cpuType   // CPU 타입 설정 (예: Q03UDVCPU = 209)
    //    actProgType.ActHostAddress <- hostAddress
    //    actProgType.ActDestinationPortNumber <- portNumber
    //    //actProgType.ActProtocolType <- protocolType  // 예: PROTOCOL_TCPIP(0x05)
        //actProgType.ActUnitType <- unitType          // 예: UNIT_QNETHER(0x2C)


    member x.IsConnected = connected
    member x.ErrorCode = errorCode
    member x.ErrorMessage = errorMessage

    /// 오류 코드 설정 메서드
    member this.SetError(code: int) =
        errorCode <- code
        errorMessage <- 
            match errorMessages.TryGetValue(code) with
            | true, message -> sprintf "0x%08X [HEX] - %s" code message
            | _ -> sprintf "0x%08X [HEX] - 알 수 없는 오류" code

        printfn "PLC Error: %s" errorMessage

    member x.Open() =
        try
            actProgType.Close() |> ignore
            let result = actProgType.Open()
            if result = 0 then
                connected <- true
                printfn "PLC 연결 성공!"
                true
            else
                x.SetError(result)
                false
        with ex ->
            printfn "예외 발생: %s" ex.Message
            false

    member x.Close() =
        if not connected then true
        else
            let result = actProgType.Close()
            if result = 0 then
                connected <- false
                true
            else
                x.SetError(result)
                false



    member x.ReadDevice(device: string) =
        try
            let mutable value = 0
            let result = actProgType.GetDevice(device, &value)
            if result = 0 then Some value else None
        with ex ->
            printfn "예외 발생: %s" ex.Message
            None

    member x.WriteDevice(device: string, value: int) =
        try
            let result = actProgType.SetDevice(device, value)
            result = 0
        with ex ->
            printfn "예외 발생: %s" ex.Message
            false

    member x.ReadDeviceBlock(device: string, size: int) =
        try
            let values = Array.zeroCreate<int> size
            let result = actProgType.ReadDeviceBlock(device, size, &values.[0])
            if result = 0 then Some values else None
        with ex ->
            printfn "예외 발생: %s" ex.Message
            None

    member x.WriteDeviceBlock(device: string, values: int[]) =
        try
            let result = actProgType.WriteDeviceBlock(device, values.Length, ref values.[0])
            result = 0
        with ex ->
            printfn "예외 발생: %s" ex.Message
            false

    member x.ReadDeviceBlock2(device: string, size: int) =
        try
            let values = Array.zeroCreate<int16> size
            let result = actProgType.ReadDeviceBlock2(device, size, &values.[0])
            if result = 0 then Some values else None
        with ex ->
            printfn "예외 발생: %s" ex.Message
            None

    member x.WriteDeviceBlock2(device: string, values: int16[]) =
        try
            let result = actProgType.WriteDeviceBlock2(device, values.Length, ref values.[0])
            result = 0
        with ex ->
            printfn "예외 발생: %s" ex.Message
            false
            ///512개의 Word 디바이스를 한번에 읽어옴
    member x.ReadDeviceRandom(devices: string[]) =
        try
            let values = Array.zeroCreate<int16> devices.Length
            let deviceList = String.Join("\n", devices)

            let result = actProgType.ReadDeviceRandom2(deviceList, devices.Length, &values[0])
            if result = 0 then  values else failwith $"ReadDeviceRandom Failed: {deviceList}"
        with ex ->
            failwithf "예외 발생: %s" ex.Message
            ///512개의 Word 디바이스를 한번에 쓰기
    member x.WriteDeviceRandom(devices: string[], values: int16[]) =
        try
            let deviceList = String.Join("\n", devices)
            let result = actProgType.WriteDeviceRandom2(deviceList, values.Length, &values[0])
            result = 0
        with ex ->
            printfn "예외 발생: %s" ex.Message
            false

    member x.SetCpuStatus(status: CpuStsType) =
        try
            let result = actProgType.SetCpuStatus(int status)
            result = 0
        with ex ->
            printfn "예외 발생: %s" ex.Message
            false

    member x.IsOnline() =
        try
            let mutable data = 0
            let result = actProgType.GetDevice("SM400", &data)
            if result = 0 && data = 1 then
                connected <- true
                true
            else
                connected <- false
                false
        with ex ->
            printfn "예외 발생: %s" ex.Message
            false

    member x.ReadBuffer(startIO: int, address: int, size: int) =
        try
            let mutable value = 0s
            let result = actProgType.ReadBuffer(startIO, address, size, &value)
            if result = 0 then Some value else None
        with ex ->
            printfn "예외 발생: %s" ex.Message
            None

    member x.WriteBuffer(startIO: int, address: int, size: int, value: int16) =
        try
            let result = actProgType.WriteBuffer(startIO, address, size, ref value)
            result = 0
        with ex ->
            printfn "예외 발생: %s" ex.Message
            false
