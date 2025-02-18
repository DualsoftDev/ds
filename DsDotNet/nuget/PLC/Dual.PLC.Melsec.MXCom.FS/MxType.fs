namespace DsMxComm

open System
open System.Collections.Generic

open System.Collections.Generic
[<AutoOpen>]
module MxTypeModule =
    type CpuStsType =
        | RUN = 0
        | STOP = 1
        | PAUSE = 2

        /// MELSEC 통신 포트 번호 (ActPortNumber 값)
    type PortNumber =
        | PORT_1 = 1
        | PORT_2 = 2
        | PORT_3 = 3
        | PORT_4 = 4
        | PORT_5 = 5
        | PORT_6 = 6
        | PORT_7 = 7
        | PORT_8 = 8
        | PORT_9 = 9
        | PORT_10 = 10

    /// MELSEC 제어 설정 (ActControl 값)
    type ControlSetting =
        | TRC_DTR = 1
        | TRC_RTS = 2
        | TRC_DRT_AND_RTS = 7
        | TRC_DTR_OR_RTS = 8

    /// MELSEC 유닛 타입 (ActUnitType 값)
    type UnitType =
        | UNIT_RJ71C24 = 0x1000
        | UNIT_QJ71C24 = 0x19
        | UNIT_LJ71C24 = 0x54
        | UNIT_RJ71EN71 = 0x1001
        | UNIT_QJ71E71 = 0x1A
        | UNIT_LJ71E71 = 0x5C
        | UNIT_RETHER = 0x1002
        | UNIT_QNETHER = 0x2C
        | UNIT_LNETHER = 0x52
        | UNIT_FXETHER = 0x4A
        | UNIT_FXVENET = 0x2004
        | UNIT_QNCPU = 0x13
        | UNIT_LNCPU = 0x50
        | UNIT_QNMOTION = 0x1C
        | UNIT_FXCPU = 0x0F
        | UNIT_RUSB = 0x1004
        | UNIT_QNUSB = 0x16
        | UNIT_LNUSB = 0x51
        | UNIT_SIMULATOR2 = 0x30
        | UNIT_SIMULATOR3 = 0x31

    /// MELSEC 통신 프로토콜 (ActProtocolType 값)
    type ProtocolType =
        | PROTOCOL_SERIAL = 0x04
        | PROTOCOL_USB = 0x0D
        | PROTOCOL_TCPIP = 0x05
        | PROTOCOL_UDPIP = 0x08
        | PROTOCOL_MNETH = 0x0F
        | PROTOCOL_MNETG = 0x14
        | PROTOCOL_CCIETSN = 0x1C
        | PROTOCOL_CCIEF = 0x15

    /// MELSEC CPU 타입 Dictionary (Key: CPU 이름, Value: Decimal 값)
    let CpuTypeMap = dict [
        "R00CPU", 4609
        "R01CPU", 4610
        "R02CPU", 4611
        "R04CPU", 4097
        "R04ENCPU", 4104
        "R08CPU", 4098
        "R08ENCPU", 4105
        "R08PCPU", 4354
        "R08PSFCPU", 4369
        "R08SFCPU", 4386
        "R16CPU", 4099
        "R16ENCPU", 4106
        "R16PCPU", 4355
        "R16PSFCPU", 4370
        "R16SFCPU", 4387
        "R32CPU", 4100
        "R32ENCPU", 4107
        "R32PCPU", 4356
        "R32PSFCPU", 4371
        "R32SFCPU", 4388
        "R120CPU", 4101
        "R120ENCPU", 4108
        "R120PCPU", 4357
        "R120PSFCPU", 4372
        "R120SFCPU", 4389
        "R16MTCPU", 4113
        "R32MTCPU", 4114
        "R12CV", 4129
        "L04HCPU", 4625
        "L08HCPU", 4626
        "L16HCPU", 4627
        "Q00JCPU", 48
        "Q00UJCPU", 128
        "Q00CPU", 49
        "Q00UCPU", 129
        "Q01CPU", 50
        "Q01UCPU", 130
        "Q02CPU", 34
        "Q02PHCPU", 69
        "Q02UCPU", 131
        "Q03UDCPU", 112
        "Q03UDECPU", 144
        "Q03UDVCPU", 209
        "Q04UDHCPU", 113
        "Q04UDEHCPU", 145
        "Q04UDVCPU", 210
        "Q06CPU", 35
        "Q06PHCPU", 70
        "Q06UDHCPU", 114
        "Q06UDEHCPU", 146
        "Q06UDVCPU", 211
        "Q10UDHCPU", 117
        "Q10UDEHCPU", 149
        "Q12CPU", 39
        "Q12PHCPU", 65
        "Q12PRHCPU", 67
        "Q13UDHCPU", 115
        "Q13UDEHCPU", 147
        "Q13UDVCPU", 212
        "Q20UDHCPU", 118
        "Q20UDEHCPU", 150
        "Q25CPU", 37
        "Q25PHCPU", 66
        "Q25PRHCPU", 68
        "Q26UDHCPU", 116
        "Q26UDEHCPU", 148
        "Q26UDVCPU", 213
        "Q50UDEHCPU", 152
        "Q100UDEHCPU", 154
        "Q02A", 321
        "Q06A", 322
        "L02SCPU", 163
        "L02CPU", 161
        "L06CPU", 165
        "L26CPU", 164
        "L26CPUBT", 162
        "Q12DC_V", 88
        "Q24DHC_V", 89
        "Q24DHC_LS", 91
        "Q24DHC_VG", 92
        "Q26DHC_LS", 93
        "QS001CPU", 96
        "Q172CPU", 1569
        "Q173CPU", 1570
        "Q172HCPU", 1569
        "Q173HCPU", 1570
        "Q172DCPU", 1573
        "Q173DCPU", 1574
        "Q172DSCPU", 1578
        "Q173DSCPU", 1579
        "FX0CPU", 513
        "FX0NCPU", 514
        "FX1CPU", 515
        "FX1SCPU", 518
        "FX1NCPU", 519
        "FX2CPU", 516
        "FX2NCPU", 517
        "FX3SCPU", 522
        "FX3GCPU", 521
        "FX3UCCPU", 520
        "FX5UCPU", 528
        "FX5UJCPU", 529
        "BOARD", 1025
    ]
