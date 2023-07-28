module PacketDebug

open PacketImpl
open Dual.Common.Core.FS
open System.Text


type Printer = string -> unit

// pk : header 정보를 포함하는 packet.  이중 최초 20byte 가 header 정보임.
let private printHeaderImpl (pk:byte []) (p:Printer) =
    let companyId = Encoding.Default.GetString(pk.[0..7])
    assert (companyId = lsis)

    pk.[8..9].ToUInt16()   |> sprintf "Reserved[8..9] = 0x%x"       |> p
    pk.[10..11].ToUInt16() |> sprintf "PLC info[10..11] = 0x%x"     |> p
    pk.[12]                |> sprintf "CpuType = 0x%x"              |> p
    pk.[13]                |> sprintf "FrameType = 0x%x"            |> p
    pk.[14..15].ToUInt16() |> sprintf "InvokeId[14..15] = 0x%x"     |> p
    pk.[16..17].ToUInt16() |> sprintf "BlockLength[16..17] = %d"    |> p
    pk.[18]                |> sprintf "Module Position[18] = 0x%x"  |> p
    pk.[19]                |> sprintf "BCC[19] = 0x%x"              |> p


/// PLC status query 애 대한 response packet 을 print.  8.2.5
/// bf : XGT Status Data buffer.  response packet 의 33번째 byte 부터
let private printStatusDataImpl (bf:byte []) (p:Printer) =
    let slotInfo  = bf.[ 0.. 3].ToUInt32()
    let cpuType   = bf.[ 4.. 5].ToUInt16()
    let connState = bf.[ 6.. 7].ToUInt16()
    let sysState  = bf.[ 8..11].ToUInt32()
    let sysError  = bf.[12..15].ToUInt32()
    let sysWarn   = bf.[16..19].ToUInt32()
    let osVersion = bf.[20..21].ToUInt16()
    let reserved  = bf.[22..23].ToUInt16()

    /// Least-significant-bit : 가장 우측 bit
    let lsb x = x &&& 1u
    slotInfo  |> sprintf "Slot[0..3] = 0x%x" |> p
    (slotInfo &&& 0x0000_000Fu) >>> 0  |> sprintf "\tLocal -> Remote : Slot = %d" |> p
    (slotInfo &&& 0x0000_00F0u) >>> 4  |> sprintf "\tLocal -> Remote : Base = %d" |> p

    // Remote -> Local 의 값은 XG5000 에서 PLC 연결 유무에 따라 값이 바뀌는 것 같음.
    (slotInfo &&& 0x0000_0F00u) >>> 8  |> sprintf "\tRemote -> Local : Slot = %d" |> p
    (slotInfo &&& 0x0000_F000u) >>> 12 |> sprintf "\tRemote -> Local : Base = %d" |> p

    (slotInfo &&& 0x000F_0000u) >>> 16 |> sprintf "\tSlot = %d"                   |> p
    (slotInfo &&& 0x00F0_0000u) >>> 20 |> sprintf "\tBase = %d"                   |> p
    (slotInfo &&& 0xFF00_0000u) >>> 24 |> sprintf "\tReserved = %d"               |> p


    (*
     * XGK 의 경우
Slot[0..3] = 0x3900ffff
    Local -> Remote : Slot = 15
    Local -> Remote : Base = 15
    Remote -> Local : Slot = 15
    Remote -> Local : Base = 15
    Slot = 0
    Base = 0
    Reserved = 57
CpuType = 0xa004
XG5000연결상태 = 0x100
PLC 모드와 운전상태[8..11] = 0xa2001051
    RUN = 1
    STOP = 0
    ERROR = 0
    DEBUG = 0
    Local Control = 1
    Remote mode = 1
    Run 중 수정 완료 = 0
    Run 중 수정 완료 = 0
시스템에러(중고장)[12..15] = 0x0
시스템경고[16..19] = 0x0
OS 버젼= 0x39
예약영역 = 0x1
     * XGI 의 경우
CpuType = 0xa415
XG5000연결상태 = 0x21
PLC 모드와 운전상태[8..11] = 0x64004441
    RUN = 1
    STOP = 0
    ERROR = 0
    DEBUG = 0
    Local Control = 0
    Remote mode = 1
    Run 중 수정 완료 = 1
    Run 중 수정 완료 = 0
시스템에러(중고장)[12..15] = 0x0
시스템경고[16..19] = 0x0
OS 버젼= 0x16
예약영역 = 0x0
    *)
    cpuType   |> sprintf "CpuType = 0x%x"                  |> p
    connState |> sprintf "XG5000연결상태 = 0x%x"            |> p
    // 관측 결과, XG5000 으로 PLC 접속한경우 0x102 의 값이 나왔고, 연결안하면 0x100 의 값이 나옴을 확인
    // XGI 인 경우, 0x21
    // 0 인 경우는 ??
    assert(connState = 0x100us || connState = 0x102us || connState = 0x21us || connState = 0x0us)

    sysState  |> sprintf "PLC 모드와 운전상태[8..11] = 0x%x" |> p
    (sysState >>>  0) |> lsb |> sprintf "\tRUN = %d" |> p
    (sysState >>>  1) |> lsb |> sprintf "\tSTOP = %d" |> p
    (sysState >>>  2) |> lsb |> sprintf "\tERROR = %d" |> p
    (sysState >>>  3) |> lsb |> sprintf "\tDEBUG = %d" |> p
    (sysState >>>  4) |> lsb |> sprintf "\tLocal Control = %d" |> p
    (sysState >>>  6) |> lsb |> sprintf "\tRemote mode = %d" |> p
    (sysState >>> 10) |> lsb |> sprintf "\tRun 중 수정 완료 = %d" |> p
    (sysState >>> 11) |> lsb |> sprintf "\tRun 중 수정 완료 = %d" |> p
    sysError  |> sprintf "시스템에러(중고장)[12..15] = 0x%x" |> p
    sysWarn   |> sprintf "시스템경고[16..19] = 0x%x"        |> p
    osVersion |> sprintf "OS 버젼= 0x%x"                    |> p
    reserved  |> sprintf "예약영역 = 0x%x"                  |> p


let printHeader (buffer:byte []) =
    printHeaderImpl buffer (logInfo "%s")


/// PLC status query 애 대한 response packet 을 print.  8.2.5
/// pk : Response packet bytes
let printStatusData (cpu:CpuType) (pk:byte []) =
    let p = logInfo "%s"
    printHeaderImpl pk p
    let command = pk.[20..21].ToUInt16()
    let expectedCommand = Command.StatusResponse.ToUInt16()
    // XGI 의 경우, 0xb1 대신 0x55 를 반환함.  문서 내용과 상충함
    //assert(command = expectedCommand)

    let dataType = pk.[22..23].ToUInt16()
    let reserved = pk.[24..25].ToUInt16()
    assert(dataType = 0us)  // don't care : Data type
    assert(reserved = 0us)  // don't care : Reserved

    let errorState = pk.[26..27].ToUInt16()
    let dontcare = pk.[28..29].ToUInt16()     // don't care : Reserved
    let dataLength = pk.[30..31].ToUInt16()

    match cpu with
    | CpuType.XgbMk ->
        (* 문서 내용과 상충 함*)
        assert(errorState = 0xFFFFus)
        assert(dontcare = 0x78us)  // don't care : Reserved
        assert(dataLength = 0us)
    | _ ->
        assert(errorState = 0us)
        assert(dontcare = 0us)
        assert(dataLength = 24us)


    printStatusDataImpl pk.[32..] p

