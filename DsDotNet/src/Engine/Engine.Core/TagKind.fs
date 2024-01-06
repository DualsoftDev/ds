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
    | stop_btn                 = 0005
    | emg_btn                  = 0006
    | test_btn                 = 0007
    | ready_btn                = 0008
    | clear_btn                = 0009
    | home_btn                 = 0010

    | auto_lamp                = 0012
    | manual_lamp              = 0013
    | drive_lamp               = 0014
    | stop_lamp                = 0015
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
    ///stopType
    | sysStopError             = 0030
    | sysStopPause             = 0031

    | autoState                = 0032
    | manualState              = 0033
    | driveState               = 0034
    | stopState                = 0035
    | emgState                 = 0036
    | testState                = 0037
    | readyState               = 0038
    | idleState                = 0039
    

 
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
    |ready_mode                = 10000
    |auto_mode                 = 10001
    |manual_mode               = 10002
    |drive_mode                = 10003
    |test_mode                 = 10004
    |stop_mode                 = 10005
    |emg_mode                  = 10006
    |idle_mode                 = 10007

    |auto_btn                  = 10011
    |manual_btn                = 10012
    |drive_btn                 = 10013
    |stop_btn                  = 10014
    |ready_btn                 = 10015
    |clear_btn                 = 10016
    |emg_btn                   = 10017
    |test_btn                  = 10018
    |home_btn                  = 10019

    |auto_lamp                 = 10021
    |manual_lamp               = 10022
    |drive_lamp                = 10023
    |stop_lamp                 = 10024
    |ready_lamp                = 10025
    |clear_lamp                = 10026
    |emg_lamp                  = 10027
    |test_lamp                 = 10028
    |home_lamp                 = 10029
    
    ///stopType
    | flowStopError                = 10030
    | flowStopPause                = 10031

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
    |errorTx                   = 11012
    |errorRx                   = 11013
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


    /// 12000 ~ 12999
    [<Flags>]
    type ApiItemTag =
    |planSet                   = 12000
    //|planRst                   = 12001  //not use
    |planEnd                   = 12002
    |txErrTrendOut             = 12003
    |txErrTimeOver             = 12004
    |rxErrShort                = 12005
    |rxErrShortOn              = 12006
    |rxErrShortRising          = 12007
    |rxErrShortTemp            = 12008
    |rxErrOpen                 = 12009
    |rxErrOpenOff              = 12010
    |rxErrOpenRising           = 12011
    |rxErrOpenTemp             = 12012
    |trxErr                    = 12013

    

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

