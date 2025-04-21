namespace MelsecProtocol

// 열거형 정의
type McFrame =
    | MC3E = 0x0050
    | MC4E = 0x0054

type PlcDeviceType =
    | M = 0x90 | SM = 0x91 | L = 0x92 | F = 0x93 | V = 0x94 | S = 0x98 | X = 0x9C | Y = 0x9D | B = 0xA0
    | SB = 0xA1 | DX = 0xA2 | DY = 0xA3 | D = 0xA8 | SD = 0xA9 | R = 0xAF | ZR = 0xB0 | W = 0xB4
    | SW = 0xB5 | TC = 0xC0 | TS = 0xC1 | TN = 0xC2 | CC = 0xC3 | CS = 0xC4 | CN = 0xC5
    | SC = 0xC6 | SS = 0xC7 | SN = 0xC8 | Z = 0xCC | TT = 0xCD | TM = 0xCE | CT = 0xCF
    | CM = 0xD0 | A = 0xD1 | Max = 0xFF
    
      

type DeviceAccessCommand =
    | BatchRead = 0x0401
    | BatchWrite = 0x1401
    | RandomRead = 0x0403
    | RandomWrite = 0x1402
    | RemoteRun = 0x1001
    | RemoteStop = 0x1002
    | RemotePause = 0x1003
    | RemoteLatchClear = 0x1005
    | RemoteReset = 0x1006
    | ReadCpuModelName = 0x0101

type DeviceAccessType =
    | Bit = 0
    | Word = 1

module MelsecProtocolCore = 
    
    let deviceMap =
        dict [
            "M", PlcDeviceType.M
            "SM", PlcDeviceType.SM
            "L", PlcDeviceType.L
            "F", PlcDeviceType.F
            "V", PlcDeviceType.V
            "S", PlcDeviceType.S
            "X", PlcDeviceType.X
            "Y", PlcDeviceType.Y
            "B", PlcDeviceType.B
            "SB", PlcDeviceType.SB
            "DX", PlcDeviceType.DX
            "DY", PlcDeviceType.DY
            "D", PlcDeviceType.D
            "SD", PlcDeviceType.SD
            "R", PlcDeviceType.R
            "ZR", PlcDeviceType.ZR
            "W", PlcDeviceType.W
            "SW", PlcDeviceType.SW
            "T", PlcDeviceType.TC
            "ST", PlcDeviceType.TC
            "TC", PlcDeviceType.TC
            "TS", PlcDeviceType.TS
            "TN", PlcDeviceType.TN
            "CC", PlcDeviceType.CC
            "CS", PlcDeviceType.CS
            "CN", PlcDeviceType.CN
            "SC", PlcDeviceType.SC
            "SS", PlcDeviceType.SS
            "SN", PlcDeviceType.SN
            "Z", PlcDeviceType.Z
            "TT", PlcDeviceType.TT
            "TM", PlcDeviceType.TM
            "C", PlcDeviceType.CT
            "CT", PlcDeviceType.CT
            "CM", PlcDeviceType.CM
            "A", PlcDeviceType.A
        ]
