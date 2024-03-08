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

    let skipValueChangedForTagKind = -1
    

    [<Flags>]
    /// 0 ~ 9999
    type SystemTag  =
    | on                       = 0000
    | off                      = 0001
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
    | homingMonitor              = 0041
    | goingMonitor               = 0042

    ///flicker
    | flicker200ms              = 0100
    | flicker1s                 = 0101
    | flicker2s                 = 0102

    ///temp (not logic 정의를 위한 plc 임시변수)
    | temp                     = 9998
    ///simulation
    | sim                      = 9999
    /// 10000 ~ 10999
    [<Flags>]
    type FlowTag    =  
    |auto_btn                  = 10001
    |manual_btn                = 10002
    |drive_btn                 = 10003
    |pause_btn                 = 10004
    |ready_btn                 = 10005
    |clear_btn                 = 10006
    |emg_btn                   = 10007
    |test_btn                  = 10008
    |home_btn                  = 10009

    |auto_lamp                 = 10021
    |manual_lamp               = 10022
    |drive_lamp                = 10023
    |pause_lamp                = 10024
    |ready_lamp                = 10025
    |clear_lamp                = 10026
    |emg_lamp                  = 10027
    |test_lamp                 = 10028
    |home_lamp                 = 10029
    
    | flowStopError            = 10040
    | flowStopConditionErr     = 10041
    | flowStopConditionErrLamp = 10042
    | flowPause                = 10043


      //복수 mode  존재 불가
    |idle_mode                 = 10100
    |auto_mode                 = 10101
    |manual_mode               = 10102

    //복수 state  존재 가능
    |drive_state                = 10103
    |test_state                 = 10104
    |error_state                = 10105
    |emergency_state            = 10106
    |ready_state                = 10107
    |origin_state               = 10108
    |homing_state               = 10109
    |going_state                = 10110

    /// 11000 ~ 11999
    [<Flags>]
    type VertexTag  =
    |startTag                  = 11000
    |resetTag                  = 11001
    |endTag                    = 11002
    
    //|spare                   = 11003
    //|spare                   = 11004
    //|spare                   = 11005

    |ready                     = 11006
    |going                     = 11007
    |finish                    = 11008
    |homing                    = 11009
    |origin                    = 11010
    |pause                     = 11011
    |errorTRx                  = 11014
    |realOriginAction          = 11016
    |relayReal                 = 11017

    |forceStart                = 11018
    |forceReset                = 11019
    |forceOn                   = 11020
    |forceOff                  = 11021

    |counter                   = 11023
    |timerOnDelay              = 11024
    |goingRealy                = 11025
    |realData                  = 11026
    |realSync                  = 11027
    |callMemo                  = 11028

    |txErrTrendOut             = 11033
    |txErrTimeOver             = 11034
    |rxErrShort                = 11035
    |rxErrShortOn              = 11036
    |rxErrShortRising          = 11037
    |rxErrShortTemp            = 11038
    |rxErrOpen                 = 11039
    |rxErrOpenOff              = 11040
    |rxErrOpenRising           = 11041
    |rxErrOpenTemp             = 11042


    /// 12000 ~ 12999
    [<Flags>]
    type ApiItemTag =
    |planSet                   = 12000
    //|planRst                   = 12001  //not use
    |planEnd                   = 12002
    |sensorLinking             = 12003
    |sensorLinked              = 12004
  
    

    /// 13000 ~ 13999
    [<Flags>]
    type LinkTag    =
    |LinkStart                 = 13000
    |LintReset                 = 13001

    /// 14000 ~ 14999
    [<Flags>]
    type ActionTag    =
    |ActionIn                 = 14000
    |ActionOut                = 14001
    |ActionMemory             = 14002

    /// 15000 ~ 14999
    [<Flags>]
    type HwSysTag    =
    |HwSysIn                     = 15000
    |HwSysOut                    = 15001

    
    /// 16000 ~ 16999
    [<Flags>]
    type VariableTag    =
    |PcSysVariable                = 16000
    |PcUserVariable               = 16001
    |PlcSysVariable               = 16002
    |PlcUserVariable              = 16003

