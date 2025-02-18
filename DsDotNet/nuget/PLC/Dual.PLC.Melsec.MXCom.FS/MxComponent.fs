namespace DsMxComm

open System
open System.Diagnostics
open System.Text
open System.Threading
open System.Timers
open ActUtlType64Lib

type CpuStsType =
    | RUN = 0
    | STOP = 1
    | PAUSE = 2

type PlcMxComponent(logicalStationNumber: int)  =
    let mutable errorCode = 0
    let mutable errorMessage = ""
    let mutable connected = false
    let mutable logicalStation = logicalStationNumber
    let actUtlType = new ActUtlType64Class()
    let timer = new Timer(1000.0)
     /// 오류 코드에 대한 메시지 매핑
    let errorMessages = dict [
        0xF0000001, "라이선스 없음 에러: PC에 라이선스가 부여되지 않았습니다. 라이선스 키 FD에서 PC에 라이선스를 부여하십시오."
        0xF0000002, "설정 데이터 읽기 에러: 논리 국번의 설정 데이터 읽기에 실패하였습니다. 올바른 논리 국번을 지정하거나 통신 설정 유틸리티에서 논리 국번을 설정하십시오."
        0xF0000003, "오픈 완료 에러: 오픈 상태에서 Open 메소드를 실행하였습니다. 통신 대상 CPU를 변경하는 경우 Close 후 Open 메소드를 실행하십시오."
        0xF0000004, "미오픈 에러: Open 메소드를 실행하지 않았습니다. Open 메소드 실행 후 해당 메소드를 실행하십시오."
        0xF0000005, "초기화 에러: MX Component 내부 유지 오브젝트의 초기화에 실패하였습니다. 프로그램을 종료하고 PC를 재기동하거나, MX Component를 재설치하십시오."
        0xF0000006, "메모리 확보 에러: MX Component 내부 메모리 확보에 실패하였습니다. 프로그램을 종료하고 PC를 재기동하거나, 다른 프로그램을 종료하여 사용 가능한 메모리를 확보하십시오."
    ]
    do 
        actUtlType.ActLogicalStationNumber <- logicalStation

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
            actUtlType.Close() |> ignore
            let result = actUtlType.Open()
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
        timer.Stop()
        if not connected then true
        else
            let result = actUtlType.Close()
            if result = 0 then
                connected <- false
                true
            else
                x.SetError(result)
                false



    member x.ReadDevice(device: string) =
        try
            let mutable value = 0
            let result = actUtlType.GetDevice(device, &value)
            if result = 0 then Some value else None
        with ex ->
            printfn "예외 발생: %s" ex.Message
            None

    member x.WriteDevice(device: string, value: int) =
        try
            let result = actUtlType.SetDevice(device, value)
            result = 0
        with ex ->
            printfn "예외 발생: %s" ex.Message
            false

    member x.ReadDeviceBlock(device: string, size: int) =
        try
            let values = Array.zeroCreate<int> size
            let result = actUtlType.ReadDeviceBlock(device, size, &values.[0])
            if result = 0 then Some values else None
        with ex ->
            printfn "예외 발생: %s" ex.Message
            None

    member x.WriteDeviceBlock(device: string, values: int[]) =
        try
            let result = actUtlType.WriteDeviceBlock(device, values.Length, ref values.[0])
            result = 0
        with ex ->
            printfn "예외 발생: %s" ex.Message
            false

    member x.ReadDeviceBlock2(device: string, size: int) =
        try
            let values = Array.zeroCreate<int16> size
            let result = actUtlType.ReadDeviceBlock2(device, size, &values.[0])
            if result = 0 then Some values else None
        with ex ->
            printfn "예외 발생: %s" ex.Message
            None

    member x.WriteDeviceBlock2(device: string, values: int16[]) =
        try
            let result = actUtlType.WriteDeviceBlock2(device, values.Length, ref values.[0])
            result = 0
        with ex ->
            printfn "예외 발생: %s" ex.Message
            false
            ///512개의 Word 디바이스를 한번에 읽어옴
    member x.ReadDeviceRandom(devices: string[]) =
        try
            let values = Array.zeroCreate<int16> devices.Length
            let deviceList = String.Join("\n", devices)

            let result = actUtlType.ReadDeviceRandom2(deviceList, devices.Length, &values[0])
            if result = 0 then  values else failwith $"ReadDeviceRandom Failed: {deviceList}"
        with ex ->
            failwithf "예외 발생: %s" ex.Message
            ///512개의 Word 디바이스를 한번에 쓰기
    member x.WriteDeviceRandom(devices: string[], values: int16[]) =
        try
            let deviceList = String.Join("\n", devices)
            let result = actUtlType.WriteDeviceRandom2(deviceList, values.Length, &values[0])
            result = 0
        with ex ->
            printfn "예외 발생: %s" ex.Message
            false

    member x.SetCpuStatus(status: CpuStsType) =
        try
            let result = actUtlType.SetCpuStatus(int status)
            result = 0
        with ex ->
            printfn "예외 발생: %s" ex.Message
            false

    member x.IsOnline() =
        try
            let mutable data = 0
            let result = actUtlType.GetDevice("SM400", &data)
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
            let result = actUtlType.ReadBuffer(startIO, address, size, &value)
            if result = 0 then Some value else None
        with ex ->
            printfn "예외 발생: %s" ex.Message
            None

    member x.WriteBuffer(startIO: int, address: int, size: int, value: int16) =
        try
            let result = actUtlType.WriteBuffer(startIO, address, size, ref value)
            result = 0
        with ex ->
            printfn "예외 발생: %s" ex.Message
            false
