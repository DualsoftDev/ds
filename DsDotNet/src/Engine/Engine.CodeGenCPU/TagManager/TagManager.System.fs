namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS

[<AutoOpen>]
module SystemManagerModule =


    /// DsSystem Manager : System Tag  를 관리하는 컨테이어
    type SystemManager (sys:DsSystem, stg:Storages)  =
        let dsSysTag (dt:DataType) name autoAddr target (systemTag:SystemTag) =
            if stg.ContainsKey(name) then stg[name]
            else
                let systemTag = systemTag |> int
                match dt with
                | (DuBOOL | DuUINT16 | DuUINT8) ->
                    createPlanVar stg  name  dt  autoAddr target systemTag sys
                | _ -> failwithlog $"not support system TagType {dt}"


        //시스템 Tag는 하위 시스템과 공유
        let dsSysBit    name autoAddr target (t:SystemTag) = (dsSysTag DuBOOL   name  autoAddr target t) :?> PlanVar<bool>
        let dsSysUint8  name autoAddr target (t:SystemTag) = (dsSysTag DuUINT8  name  autoAddr target t) :?> PlanVar<uint8>
        let dsSysUint16 name autoAddr target (t:SystemTag) = (dsSysTag DuUINT16 name  autoAddr target t) :?> PlanVar<uint16>

        //let on     = let onBit = dsSysBit "_onalways"     false   sys   SystemTag.on
        //             onBit.Value <- true  //항상 ON
        //             onBit
        let on           = dsSysBit   "_onalways"         false sys  SystemTag.on
        let off          = dsSysBit   "_offalways"        false sys  SystemTag.off
        let auto_btn     = dsSysBit   "sysauto_btn"       true  sys  SystemTag.auto_btn
        let manual_btn   = dsSysBit   "sysmanual_btn"     true  sys  SystemTag.manual_btn
        let drive_btn    = dsSysBit   "sysdrive_btn"      true  sys  SystemTag.drive_btn
        let stop_btn     = dsSysBit   "sysstop_btn"       true  sys  SystemTag.stop_btn
        let emg_btn      = dsSysBit   "sysemg_btn"        true  sys  SystemTag.emg_btn
        let test_btn     = dsSysBit   "systest_btn"       true  sys  SystemTag.test_btn
        let ready_btn    = dsSysBit   "sysready_btn"      true  sys  SystemTag.ready_btn
        let clear_btn    = dsSysBit   "sysclear_btn"      true  sys  SystemTag.clear_btn
        let home_btn     = dsSysBit   "syshome_btn"       true  sys  SystemTag.home_btn

        let auto_lamp     = dsSysBit   "sysauto_lamp"       true  sys  SystemTag.auto_lamp
        let manual_lamp   = dsSysBit   "sysmanual_lamp"     true  sys  SystemTag.manual_lamp
        let drive_lamp    = dsSysBit   "sysdrive_lamp"      true  sys  SystemTag.drive_lamp
        let stop_lamp     = dsSysBit   "sysstop_lamp"       true  sys  SystemTag.stop_lamp
        let emg_lamp      = dsSysBit   "sysemg_lamp"        true  sys  SystemTag.emg_lamp
        let test_lamp     = dsSysBit   "systest_lamp"       true  sys  SystemTag.test_lamp
        let ready_lamp    = dsSysBit   "sysready_lamp"      true  sys  SystemTag.ready_lamp
        let clear_lamp    = dsSysBit   "sysclear_lamp"      true  sys  SystemTag.clear_lamp
        let home_lamp     = dsSysBit   "syshome_lamp"       true  sys  SystemTag.home_lamp


        

        let dtimeyy  = dsSysUint8 "_RTC_TIME[0]"  false sys  SystemTag.datet_yy         //ls xgi 현재시각[년도]
        let dtimemm  = dsSysUint8 "_RTC_TIME[1]"  false sys  SystemTag.datet_mm         //ls xgi 현재시각[월]
        let dtimedd  = dsSysUint8 "_RTC_TIME[2]"  false sys  SystemTag.datet_dd         //ls xgi 현재시각[일]
        let dtimeh   = dsSysUint8 "_RTC_TIME[3]"  false sys  SystemTag.datet_h          //ls xgi 현재시각[시]
        let dtimem   = dsSysUint8 "_RTC_TIME[4]"  false sys  SystemTag.datet_m          //ls xgi 현재시각[분]
        let dtimes   = dsSysUint8 "_RTC_TIME[5]"  false sys  SystemTag.datet_s          //ls xgi 현재시각[초]
        //let dtimewk  = dsSysUint8 "_ms"                 //ls xgi 현재시각[요일]
        //let dtimeyk  = dsSysUint8 "_ms"                 //ls xgi 현재시각[년대]

        let tout     =
            let tout = dsSysUint16  "systout" true sys SystemTag.timeout 
            //type CountUnitType = uint16  => 32bit (msec 단위 필요) //test ahn
            tout.Value <- 10000us
            tout

        let sysStopError    = dsSysBit "sysStopError"   true  sys   SystemTag.sysStopError
        let sysStopPause    = dsSysBit "sysStopPause"   true  sys   SystemTag.sysStopPause
        let sysDrive    = dsSysBit "sysDrive"   true  sys   SystemTag.sysDrive
        
        let sim    = dsSysBit "syssim"   true  sys   SystemTag.sim
        let flicker200msec  = dsSysBit "_T200MS" true  sys   SystemTag.flicker200ms
        let flicker1sec    = dsSysBit "_T1S"   true  sys   SystemTag.flicker1s
        let flicker2sec    = dsSysBit "_T2S"   true  sys   SystemTag.flicker2s
        do 
            flicker200msec.Address <- "%FX146"
            flicker1sec.Address <- "%FX147"
            flicker2sec.Address <- "%FX148"
            on.Value <- true
            off.Value <- false

        interface ITagManager with
            member x.Target = sys
            member x.Storages = stg

        member s.GetTempBoolTag(name:string, address:string, fqdn:IQualifiedNamed) : IStorage=
                if stg.ContainsKey(name) then stg[name]
                else
                    createBridgeTag(stg, name, address, SystemTag.temp|>int, BridgeType.DummyTemp, sys, fqdn).Value
            
        member s.GetSystemTag(st:SystemTag) : IStorage=
            match st with
            | SystemTag.on         ->    on
            | SystemTag.off        ->    off
            | SystemTag.auto_btn   ->    auto_btn
            | SystemTag.manual_btn ->    manual_btn
            | SystemTag.drive_btn  ->    drive_btn
            | SystemTag.stop_btn   ->    stop_btn
            | SystemTag.emg_btn    ->    emg_btn
            | SystemTag.test_btn   ->    test_btn
            | SystemTag.ready_btn  ->    ready_btn
            | SystemTag.clear_btn  ->    clear_btn
            | SystemTag.home_btn   ->    home_btn


            | SystemTag.auto_lamp   ->    auto_lamp
            | SystemTag.manual_lamp ->    manual_lamp
            | SystemTag.drive_lamp  ->    drive_lamp
            | SystemTag.stop_lamp   ->    stop_lamp
            | SystemTag.emg_lamp    ->    emg_lamp
            | SystemTag.test_lamp   ->    test_lamp
            | SystemTag.ready_lamp  ->    ready_lamp
            | SystemTag.clear_lamp  ->    clear_lamp
            | SystemTag.home_lamp   ->    home_lamp


            | SystemTag.datet_yy        ->    dtimeyy
            | SystemTag.datet_mm        ->    dtimemm
            | SystemTag.datet_dd        ->    dtimedd
            | SystemTag.datet_h         ->    dtimeh
            | SystemTag.datet_m         ->    dtimem
            | SystemTag.datet_s         ->    dtimes
            | SystemTag.timeout         ->    tout
            | SystemTag.sysStopError    ->    sysStopError
            | SystemTag.sysStopPause    ->    sysStopPause
            | SystemTag.sysDrive        ->    sysDrive
            | SystemTag.flicker200ms    -> flicker200msec
            | SystemTag.flicker1s       -> flicker1sec
            | SystemTag.flicker2s       -> flicker2sec
            | SystemTag.sim             ->    sim
            | _ -> failwithlog $"Error : GetSystemTag {st} type not support!!"
