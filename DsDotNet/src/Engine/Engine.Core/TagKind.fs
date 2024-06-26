namespace Engine.Core

open Dual.Common.Core.FS
open System
open System.Runtime.CompilerServices
open System.Reactive.Subjects

[<AutoOpen>]
module TagKindList =

    let [<Literal>] TagStartSystem       = 0
    let [<Literal>] TagStartFlow         = 10000
    let [<Literal>] TagStartVertex       = 11000
    let [<Literal>] TagStartApi          = 12000
    let [<Literal>] TagStartAction       = 14000
    let [<Literal>] TagStartActionHwTag  = 15000
    let [<Literal>] TagStartVariable     = 16000
    let [<Literal>] TagStartJob          = 17000

    let skipValueChangedForTagKind = -1
    

    [<Flags>]
    /// 0 ~ 9999
    type SystemTag  =
    | _ON                      = 0000 //system on  LS 특수비트랑 이름 통일
    | _OFF                     = 0001 //system off  LS 특수비트랑 이름 통일
    | auto_btn                 = 0002
    | manual_btn               = 0003
    | drive_btn                = 0004
    | pause_btn                = 0005
    | emg_btn                  = 0006
    | test_btn                 = 0007
    | ready_btn                = 0008
    | clear_btn                = 0009
    | home_btn                 = 0010

    | auto_lamp                = 0012
    | manual_lamp              = 0013
    | drive_lamp               = 0014
    | pause_lamp               = 0015
    | emg_lamp                 = 0016
    | test_lamp                = 0017
    | ready_lamp               = 0018
    | clear_lamp               = 0019
    | home_lamp                = 0020
    ///sysdatatimetag
    | datet_yy                 = 0021
    | datet_mm                 = 0022
    | datet_dd                 = 0023
    | datet_h                  = 0024
    | datet_m                  = 0025
    | datet_s                  = 0026
    ///systxErrTimetag             
    | timeout                  = 0027

    | pauseMonitor               = 0031
    | idleMonitor                = 0032
    | autoMonitor                = 0033
    | manualMonitor              = 0034
    | driveMonitor               = 0035
    | testMonitor                = 0036
    | errorMonitor               = 0037
    | emergencyMonitor           = 0038
    | readyMonitor               = 0039
    | originMonitor              = 0040
    | goingMonitor               = 0041


    ///flicker                   
    | _T20MS                     = 0100  //system timer  LS 특수비트랑 이름 통일
    | _T100MS                    = 0101  //system timer  LS 특수비트랑 이름 통일
    | _T200MS                    = 0102  //system timer  LS 특수비트랑 이름 통일
    | _T1S                       = 0103  //system timer  LS 특수비트랑 이름 통일
    | _T2S                       = 0104  //system timer  LS 특수비트랑 이름 통일
    ///emulation
    | emulation                 = 9997
    ///simulation
    | sim                       = 9998
    ///temp (not logic 정의를 위한 plc 임시변수)
    | temp                      = 9999

    /// 10000 ~ 10999
    [<Flags>]
    type FlowTag    =  
    | auto_btn                  = 10001
    | manual_btn                = 10002
    | drive_btn                 = 10003
    | pause_btn                 = 10004
    | ready_btn                 = 10005
    | clear_btn                 = 10006
    | emg_btn                   = 10007
    | test_btn                  = 10008
    | home_btn                  = 10009

    | auto_lamp                 = 10021
    | manual_lamp               = 10022
    | drive_lamp                = 10023
    | pause_lamp                = 10024
    | ready_lamp                = 10025
    | clear_lamp                = 10026
    | emg_lamp                  = 10027
    | test_lamp                 = 10028
    | home_lamp                 = 10029
    
    | flowStopError             = 10041
    | flowReadyCondition        = 10042
    | flowDriveCondition        = 10043



      //복수 mode  존재 불가
    | idle_mode                 = 10100
    | auto_mode                 = 10101
    | manual_mode               = 10102

    //복수 state  존재 가능
    | drive_state                = 10103
    | test_state                 = 10104
    | error_state                = 10105
    | emergency_state            = 10106
    | ready_state                = 10107
    | origin_state               = 10108
    | going_state                = 10109
    | pause_state                = 10110

    /// 11000 ~ 11999
    [<Flags>]
    type VertexTag  =
    | startTag                  = 11000
    | resetTag                  = 11001
    | endTag                    = 11002
    

    | ready                     = 11006
    | going                     = 11007
    | finish                    = 11008
    | homing                    = 11009
    | origin                    = 11010
    | pause                     = 11011
    | errorTRx                  = 11014
    | realOriginAction          = 11015
    | realOriginInit            = 11016
    | realOriginButton          = 11017
    | relayReal                 = 11018
    
    | forceStart                = 11019
    | forceReset                = 11020
    | forceOn                   = 11021
    | forceOff                  = 11022

    | counter                   = 11023
    | timerOnDelay              = 11024
    | goingRealy                = 11025
    | realData                  = 11026
    | realLink                  = 11027
    | callMemo                  = 11028

    | callCommandEnd            = 11030
    | callOperatorValue         = 11031
    
    | dummyCoinSTs              = 11032
    | dummyCoinRTs              = 11033
    | dummyCoinETs              = 11034

    | actionSync                = 11035
    | actionStart               = 11036
    | actionEnd                 = 11037

    | txErrOnTimeShortage        = 11050
    | txErrOnTimeOver            = 11051
    | txErrOffTimeShortage       = 11052
    | txErrOffTimeOver           = 11053
    | rxErrShort                 = 11054
    | rxErrShortRising           = 11055
    | rxErrOpen                  = 11056
    | rxErrOpenRising            = 11057
    | workErrOriginGoing         = 11060

    /// 12000 ~ 12999
    [<Flags>]
    type ApiItemTag =
    | planSet                   = 12000
    | planEnd                   = 12002
    | sensorLinking             = 12003
    | sensorLinked              = 12004
      

    /// 14000 ~ 14999
    [<Flags>]
    type ActionTag    =
    | ActionIn                 = 14000
    | ActionOut                = 14001
    | ActionMemory             = 14002

    /// 15000 ~ 14999
    [<Flags>]
    type HwSysTag    =
    | HwSysIn                     = 15000
    | HwSysOut                    = 15001
    | HwStopConditionErrLamp      = 15010
    | HwStopEmergencyErrLamp      = 15011
    
    /// 16000 ~ 16999
    [<Flags>]
    type VariableTag    =
    | PcSysVariable                = 16000
    | PcUserVariable               = 16001
    | PlcSysVariable               = 16002
    | PlcUserVariable              = 16003
    
