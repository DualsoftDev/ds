namespace Engine.CodeGenCPU

open System.Diagnostics
open Engine.Core
open System.Collections.Generic
open System.Runtime.CompilerServices
open System
open Engine.Common.FS

[<AutoOpen>]
module SystemManagerModule =
    [<Flags>]
    type SysBitTag =
    | ON
    | OFF
    | AUTO
    | MANUAL
    | DRIVE
    | STOP
    | EMG
    | TEST
    | READY
    | CLEAR
    | HOME

    type SysDataTimeTag =
    | DATET_YY
    | DATET_MM
    | DATET_DD
    | DATET_H
    | DATET_M
    | DATET_S

    type SysErrorTag =
    | TIMEOUT

    /// DsSystem Manager : System Tag  를 관리하는 컨테이어
    type SystemManager (sys:DsSystem, stg:Storages)  =
            //시스템 Tag는 하위 시스템과 공유
        let dsSysBit    name fillAutoAddress = (if stg.ContainsKey(name) then stg[name] else createPlanVar stg  name  DuBOOL   fillAutoAddress) :?> PlanVar<bool>
        let dsSysUint8  name fillAutoAddress = (if stg.ContainsKey(name) then stg[name] else createPlanVar stg  name  DuUINT8  fillAutoAddress) :?> PlanVar<uint8>
        let dsSysUint16 name fillAutoAddress = (if stg.ContainsKey(name) then stg[name] else createPlanVar stg  name  DuUINT16 fillAutoAddress) :?> PlanVar<uint16>

       // let on     = let tmpOn = dsSysBit "_on" in tmpOn.Value <- true; tmpOn
        let on     = dsSysBit "_on"     false
        let off    = dsSysBit "_off"    false
        let auto   = dsSysBit "sysauto"   true
        let manual = dsSysBit "sysmanual" true
        let drive  = dsSysBit "sysdrive"  true
        let stop   = dsSysBit "sysstop"   true
        let emg    = dsSysBit "sysemg"    true
        let test   = dsSysBit "systest"   true
        let ready  = dsSysBit "sysready"  true
        let clear  = dsSysBit "sysclear"  true
        let home   = dsSysBit "syshome"   true
        let dtimeyy  = dsSysUint8 "_RTC_TIME[0]"  false   //ls xgi 현재시각[년도]
        let dtimemm  = dsSysUint8 "_RTC_TIME[1]"  false   //ls xgi 현재시각[월]
        let dtimedd  = dsSysUint8 "_RTC_TIME[2]"  false   //ls xgi 현재시각[일]
        let dtimeh   = dsSysUint8 "_RTC_TIME[3]"  false   //ls xgi 현재시각[시]
        let dtimem   = dsSysUint8 "_RTC_TIME[4]"  false   //ls xgi 현재시각[분]
        let dtimes   = dsSysUint8 "_RTC_TIME[5]"  false   //ls xgi 현재시각[초]
        //let dtimewk  = dsSysUint8 "_ms"                 //ls xgi 현재시각[요일]
        //let dtimeyk  = dsSysUint8 "_ms"                 //ls xgi 현재시각[년대]

        let tout     =
            let tout = dsSysUint16  "systout" true
            tout.Value <- 10000us
            tout

        interface ITagManager with
            member x.Target = sys
            member x.Storages = stg

        member f.GetSysBitTag(st:SysBitTag)     =
            match st with
            |ON         ->    on
            |OFF        ->    off
            |AUTO       ->    auto
            |MANUAL     ->    manual
            |DRIVE      ->    drive
            |STOP       ->    stop
            |EMG        ->    emg
            |TEST       ->    test
            |READY      ->    ready
            |CLEAR      ->    clear
            |HOME       ->    home

        member f.GetSysDateTag(st:SysDataTimeTag)     =
            match st with
            |DATET_YY   ->    dtimeyy
            |DATET_MM   ->    dtimemm
            |DATET_DD   ->    dtimedd
            |DATET_H    ->    dtimeh
            |DATET_M    ->    dtimem
            |DATET_S    ->    dtimes

        member f.GetSysErrTag(st:SysErrorTag)     =
            match st with
            |TIMEOUT    ->    tout
