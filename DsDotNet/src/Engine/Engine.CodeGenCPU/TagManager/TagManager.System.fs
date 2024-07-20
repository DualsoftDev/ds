namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Linq

[<AutoOpen>]
module SystemManagerModule =


    /// DsSystem Manager : System Tag  를 관리하는 컨테이어
    type SystemManager (sys:DsSystem, rootSys:DsSystem, stg:Storages, target:PlatformTarget)  =
            //시스템 TAG는 root 시스템   TAG 공용 사용 ex)curSys._ON  = rootSys._ON  
        let dsSysTag (dt:DataType)  autoAddr target (systemTag:SystemTag) (internalSysTag:bool)=
            let name =  
                if internalSysTag
                then
                    $"{systemTag}" |> validStorageName
                else 
                    getStorageName rootSys (int systemTag)


            if stg.ContainsKey(name) then stg[name]
            else
                let systemTag = systemTag |> int
                match dt with
                | (DuBOOL | DuUINT8 | DuUINT16 | DuUINT32) ->
                    createSystemPlanVar stg  name  dt  autoAddr target systemTag sys
                | _ -> failwithlog $"not support system TagType {dt}"


        //시스템 Tag는 하위 시스템과 공유
        let dsSysBit     autoAddr target (t:SystemTag) sysTag = (dsSysTag DuBOOL     autoAddr target t sysTag) :?> PlanVar<bool>
        let dsSysUint8   autoAddr target (t:SystemTag) sysTag = (dsSysTag DuUINT8    autoAddr target t sysTag) :?> PlanVar<uint8>
        let dsSysUint16  autoAddr target (t:SystemTag) sysTag = (dsSysTag DuUINT16   autoAddr target t sysTag) :?> PlanVar<uint16>
        let dsSysUint32  autoAddr target (t:SystemTag) sysTag = (dsSysTag DuUINT32   autoAddr target t sysTag) :?> PlanVar<uint32>


        let mutualCalls = getMutualInfo (sys.GetVerticesOfJobCalls().Cast<Vertex>())

        let on           = dsSysBit false sys  SystemTag._ON            true
        let off          = dsSysBit false sys  SystemTag._OFF           true
        let auto_btn     = dsSysBit true  sys  SystemTag.auto_btn       false
        let manual_btn   = dsSysBit true  sys  SystemTag.manual_btn     false
        let drive_btn    = dsSysBit true  sys  SystemTag.drive_btn      false
        let pause_btn    = dsSysBit true  sys  SystemTag.pause_btn      false
        let emg_btn      = dsSysBit true  sys  SystemTag.emg_btn        false
        let test_btn     = dsSysBit true  sys  SystemTag.test_btn       false
        let ready_btn    = dsSysBit true  sys  SystemTag.ready_btn      false
        let clear_btn    = dsSysBit true  sys  SystemTag.clear_btn      false
        let home_btn     = dsSysBit true  sys  SystemTag.home_btn       false

        let auto_lamp     = dsSysBit true  sys  SystemTag.auto_lamp     false
        let manual_lamp   = dsSysBit true  sys  SystemTag.manual_lamp   false
        let drive_lamp    = dsSysBit true  sys  SystemTag.drive_lamp    false
        let pause_lamp    = dsSysBit true  sys  SystemTag.pause_lamp    false
        let emg_lamp      = dsSysBit true  sys  SystemTag.emg_lamp      false
        let test_lamp     = dsSysBit true  sys  SystemTag.test_lamp     false
        let ready_lamp    = dsSysBit true  sys  SystemTag.ready_lamp    false
        let clear_lamp    = dsSysBit true  sys  SystemTag.clear_lamp    false
        let home_lamp     = dsSysBit true  sys  SystemTag.home_lamp     false

        //let dtimeyy  = dsSysUint8 "_RTC_TIME[0]"  false sys  SystemTag.datet_yy         //ls xgi 현재시각[년도]
        //let dtimemm  = dsSysUint8 "_RTC_TIME[1]"  false sys  SystemTag.datet_mm         //ls xgi 현재시각[월]
        //let dtimedd  = dsSysUint8 "_RTC_TIME[2]"  false sys  SystemTag.datet_dd         //ls xgi 현재시각[일]
        //let dtimeh   = dsSysUint8 "_RTC_TIME[3]"  false sys  SystemTag.datet_h          //ls xgi 현재시각[시]
        //let dtimem   = dsSysUint8 "_RTC_TIME[4]"  false sys  SystemTag.datet_m          //ls xgi 현재시각[분]
        //let dtimes   = dsSysUint8 "_RTC_TIME[5]"  false sys  SystemTag.datet_s          //ls xgi 현재시각[초]
        //let dtimewk  = dsSysUint8 "_ms"                 //ls xgi 현재시각[요일]
        //let dtimeyk  = dsSysUint8 "_ms"                 //ls xgi 현재시각[년대]

        let tout     =
            let tout = dsSysUint32   true sys SystemTag.timeout  false
            tout.Value <- RuntimeDS.TimeoutCall
            tout

        let pauseMonitor      = dsSysBit true  sys   SystemTag.pauseMonitor      false
        let autoMonitor       = dsSysBit true  sys   SystemTag.autoMonitor       false
        let manualMonitor     = dsSysBit true  sys   SystemTag.manualMonitor     false
        let driveMonitor      = dsSysBit true  sys   SystemTag.driveMonitor      false
        let errorMonitor      = dsSysBit true  sys   SystemTag.errorMonitor      false
        let emergencyMonitor  = dsSysBit true  sys   SystemTag.emergencyMonitor  false  
        let testMonitor       = dsSysBit true  sys   SystemTag.testMonitor       false
        let readyMonitor      = dsSysBit true  sys   SystemTag.readyMonitor      false
        let idleMonitor       = dsSysBit true  sys   SystemTag.idleMonitor       false
        let originMonitor     = dsSysBit true  sys   SystemTag.originMonitor     false
        let goingMonitor      = dsSysBit true  sys   SystemTag.goingMonitor      false
        
        let flicker20msec  = dsSysBit true  sys   SystemTag._T20MS      true
        let flicker100msec = dsSysBit true  sys   SystemTag._T100MS     true
        let flicker200msec = dsSysBit true  sys   SystemTag._T200MS     true
        let flicker1sec    = dsSysBit true  sys   SystemTag._T1S        true
        let flicker2sec    = dsSysBit true  sys   SystemTag._T2S        true


        let sim            = dsSysBit   true  sys SystemTag.sim            false
        let emulation      = dsSysBit   true  sys SystemTag.emulation      false


        do 
 
            on.Value <- true
            off.Value <- false

            if target = PlatformTarget.XGK
            then
                on.Address  <- "F00099"
                off.Address <- "F0009A"

                flicker20msec .Address <- "F00090"
                flicker100msec.Address <- "F00091"
                flicker200msec.Address <- "F00092"
                flicker1sec   .Address <- "F00093"
                flicker2sec   .Address <- "F00094"

        interface ITagManager with
            member x.Target = sys
            member x.Storages = stg
            
        member s.TargetType = target 
        member s.MutualCalls = mutualCalls 
        member s.GetTempBoolTag(name:string, address:string, fqdn:IQualifiedNamed) : IStorage=
                if stg.ContainsKey(name) then stg[name]
                else
                    createBridgeTag(stg, name, address, SystemTag.temp|>int, BridgeType.DummyTemp, sys, fqdn, DuBOOL).Value

        member s.GetTempTimerTag(name:string) : TimerStruct =
                timer stg name sys target
            
        member s.GetSystemTag(st:SystemTag) : IStorage=
            match st with
            | SystemTag._ON         ->    on
            | SystemTag._OFF        ->    off
            | SystemTag.auto_btn   ->    auto_btn
            | SystemTag.manual_btn ->    manual_btn
            | SystemTag.drive_btn  ->    drive_btn
            | SystemTag.pause_btn   ->   pause_btn
            | SystemTag.emg_btn    ->    emg_btn
            | SystemTag.test_btn   ->    test_btn
            | SystemTag.ready_btn  ->    ready_btn
            | SystemTag.clear_btn  ->    clear_btn
            | SystemTag.home_btn   ->    home_btn


            | SystemTag.auto_lamp   ->    auto_lamp
            | SystemTag.manual_lamp ->    manual_lamp
            | SystemTag.drive_lamp  ->    drive_lamp
            | SystemTag.pause_lamp  ->    pause_lamp
            | SystemTag.emg_lamp    ->    emg_lamp
            | SystemTag.test_lamp   ->    test_lamp
            | SystemTag.ready_lamp  ->    ready_lamp
            | SystemTag.clear_lamp  ->    clear_lamp
            | SystemTag.home_lamp   ->    home_lamp

            //| SystemTag.datet_yy        ->    dtimeyy
            //| SystemTag.datet_mm        ->    dtimemm
            //| SystemTag.datet_dd        ->    dtimedd
            //| SystemTag.datet_h         ->    dtimeh
            //| SystemTag.datet_m         ->    dtimem
            //| SystemTag.datet_s         ->    dtimes
            | SystemTag.timeout         ->    tout
            
            | SystemTag.pauseMonitor         ->    pauseMonitor
            | SystemTag.idleMonitor          ->    idleMonitor     
            | SystemTag.autoMonitor          ->    autoMonitor     
            | SystemTag.manualMonitor        ->    manualMonitor   
            | SystemTag.driveMonitor         ->    driveMonitor    
            | SystemTag.errorMonitor         ->    errorMonitor    
            | SystemTag.emergencyMonitor     ->    emergencyMonitor
            | SystemTag.testMonitor          ->    testMonitor     
            | SystemTag.readyMonitor         ->    readyMonitor    
            | SystemTag.originMonitor        ->    originMonitor   
            | SystemTag.goingMonitor         ->    goingMonitor    
            
            
            | SystemTag._T20MS     -> flicker20msec
            | SystemTag._T100MS    -> flicker100msec
            | SystemTag._T200MS    -> flicker200msec
            | SystemTag._T1S       -> flicker1sec
            | SystemTag._T2S       -> flicker2sec

            
            | SystemTag.emulation       -> emulation
            | SystemTag.sim             ->    sim
            | _ -> failwithlog $"Error : GetSystemTag {st} type not support!!"

    [<Extension>]
    type SystemManagerExt =
        [<Extension>] static member OnTag (x:ISystem) = ((x:?>DsSystem).TagManager :?> SystemManager).GetSystemTag(SystemTag._ON) :?> PlanVar<'T>
        [<Extension>] static member OffTag (x:ISystem) = ((x:?>DsSystem).TagManager :?> SystemManager).GetSystemTag(SystemTag._OFF) :?> PlanVar<'T>
       
