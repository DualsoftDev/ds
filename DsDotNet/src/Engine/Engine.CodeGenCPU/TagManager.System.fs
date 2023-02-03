namespace Engine.CodeGenCPU

open System.Diagnostics
open Engine.Core
open System.Collections.Generic
open System.Runtime.CompilerServices
open System
open Engine.Common.FS

[<AutoOpen>]
module SystemManagerModule =
    

    /// DsSystem Manager : System Tag  를 관리하는 컨테이어
    type SystemManager (sys:DsSystem, stg:Storages)  =
        let dsSysTag (dt:DataType) name autoAddr target (systemTag:SystemTag) =
            if stg.ContainsKey(name) then stg[name]
            else
                let systemTag = systemTag |> int
                match dt with
                | DuBOOL      -> createPlanVar stg  name  dt  autoAddr target systemTag
                | DuUINT16    -> createPlanVar stg  name  dt  autoAddr target systemTag
                | DuUINT8     -> createPlanVar stg  name  dt  autoAddr target systemTag
                | _ -> failwithlog $"not support system TagType {dt}"


        //시스템 Tag는 하위 시스템과 공유
        let dsSysBit    name autoAddr target (t:SystemTag) = (dsSysTag DuBOOL   name  autoAddr target t) :?> PlanVar<bool>
        let dsSysUint8  name autoAddr target (t:SystemTag) = (dsSysTag DuUINT8  name  autoAddr target t) :?> PlanVar<uint8>
        let dsSysUint16 name autoAddr target (t:SystemTag) = (dsSysTag DuUINT16 name  autoAddr target t) :?> PlanVar<uint16>

        let on     = dsSysBit "_on"     false   sys   SystemTag.on
        let off    = dsSysBit "_off"    false   sys   SystemTag.off
        let auto   = dsSysBit "sysauto"   true  sys   SystemTag.auto
        let manual = dsSysBit "sysmanual" true  sys   SystemTag.manual
        let drive  = dsSysBit "sysdrive"  true  sys   SystemTag.drive
        let stop   = dsSysBit "sysstop"   true  sys   SystemTag.stop
        let emg    = dsSysBit "sysemg"    true  sys   SystemTag.emg
        let test   = dsSysBit "systest"   true  sys   SystemTag.test
        let ready  = dsSysBit "sysready"  true  sys   SystemTag.ready
        let clear  = dsSysBit "sysclear"  true  sys   SystemTag.clear
        let home   = dsSysBit "syshome"   true  sys   SystemTag.home
        let dtimeyy  = dsSysUint8 "_RTC_TIME[0]"  false  sys SystemTag.datet_yy                //ls xgi 현재시각[년도]
        let dtimemm  = dsSysUint8 "_RTC_TIME[1]"  false  sys SystemTag.datet_mm                //ls xgi 현재시각[월]
        let dtimedd  = dsSysUint8 "_RTC_TIME[2]"  false  sys SystemTag.datet_dd                //ls xgi 현재시각[일]
        let dtimeh   = dsSysUint8 "_RTC_TIME[3]"  false  sys SystemTag.datet_h                 //ls xgi 현재시각[시]
        let dtimem   = dsSysUint8 "_RTC_TIME[4]"  false  sys SystemTag.datet_m                 //ls xgi 현재시각[분]
        let dtimes   = dsSysUint8 "_RTC_TIME[5]"  false  sys SystemTag.datet_s                 //ls xgi 현재시각[초]
        //let dtimewk  = dsSysUint8 "_ms"                 //ls xgi 현재시각[요일]
        //let dtimeyk  = dsSysUint8 "_ms"                 //ls xgi 현재시각[년대]

        let tout     =
            let tout = dsSysUint16  "systout" true sys SystemTag.timeout
            tout.Value <- 10000us
            tout

        interface ITagManager with
            member x.Target = sys
            member x.Storages = stg

        member f.GetSystemTag(st:SystemTag)     =
            match st with
            |SystemTag.on         ->    on       :> IStorage
            |SystemTag.off        ->    off      :> IStorage
            |SystemTag.auto       ->    auto     :> IStorage
            |SystemTag.manual     ->    manual   :> IStorage
            |SystemTag.drive      ->    drive    :> IStorage
            |SystemTag.stop       ->    stop     :> IStorage
            |SystemTag.emg        ->    emg      :> IStorage
            |SystemTag.test       ->    test     :> IStorage
            |SystemTag.ready      ->    ready    :> IStorage
            |SystemTag.clear      ->    clear    :> IStorage
            |SystemTag.home       ->    home     :> IStorage
            |SystemTag.datet_yy   ->    dtimeyy  :> IStorage
            |SystemTag.datet_mm   ->    dtimemm  :> IStorage
            |SystemTag.datet_dd   ->    dtimedd  :> IStorage
            |SystemTag.datet_h    ->    dtimeh   :> IStorage
            |SystemTag.datet_m    ->    dtimem   :> IStorage
            |SystemTag.datet_s    ->    dtimes   :> IStorage
            |SystemTag.timeout    ->    tout     :> IStorage
            |_ -> failwithlog $"Error : GetSystemTag {st} type not support!!"
