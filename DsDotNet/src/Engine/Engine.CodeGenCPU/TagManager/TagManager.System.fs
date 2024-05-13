namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Linq

[<AutoOpen>]
module SystemManagerModule =


    /// DsSystem Manager : System Tag  를 관리하는 컨테이어
    type SystemManager (sys:DsSystem, stg:Storages, target:PlatformTarget)  =
        let dsSysTag (dt:DataType) name autoAddr target (systemTag:SystemTag) =
            if stg.ContainsKey(name) then stg[name]
            else
                let systemTag = systemTag |> int
                match dt with
                | (DuBOOL | DuUINT8 | DuUINT16 | DuUINT32) ->
                    createSystemPlanVar stg  name  dt  autoAddr target systemTag sys
                | _ -> failwithlog $"not support system TagType {dt}"


        //시스템 Tag는 하위 시스템과 공유
        let dsSysBit    name autoAddr target (t:SystemTag) = (dsSysTag DuBOOL   name  autoAddr target t) :?> PlanVar<bool>
        let dsSysUint8  name autoAddr target (t:SystemTag) = (dsSysTag DuUINT8  name  autoAddr target t) :?> PlanVar<uint8>
        let dsSysUint16 name autoAddr target (t:SystemTag) = (dsSysTag DuUINT16 name  autoAddr target t) :?> PlanVar<uint16>
        let dsSysUint32 name autoAddr target (t:SystemTag) = (dsSysTag DuUINT32 name  autoAddr target t) :?> PlanVar<uint32>


        let mutualCalls = getMutualInfo (sys.GetVerticesOfJobCalls().Cast<Vertex>())


        //let on     = let onBit = dsSysBit "_onalways"     false   sys   SystemTag.on
        //             onBit.Value <- true  //항상 ON
        //             onBit
        let on           = dsSysBit   "_ON"         false sys  SystemTag.on
        let off          = dsSysBit   "_OFF"        false sys  SystemTag.off
        let auto_btn     = dsSysBit   "sysauto_btn"       true  sys  SystemTag.auto_btn
        let manual_btn   = dsSysBit   "sysmanual_btn"     true  sys  SystemTag.manual_btn
        let drive_btn    = dsSysBit   "sysdrive_btn"      true  sys  SystemTag.drive_btn
        let pause_btn     = dsSysBit   "syspause_btn"     true  sys  SystemTag.pause_btn
        let emg_btn      = dsSysBit   "sysemg_btn"        true  sys  SystemTag.emg_btn
        let test_btn     = dsSysBit   "systest_btn"       true  sys  SystemTag.test_btn
        let ready_btn    = dsSysBit   "sysready_btn"      true  sys  SystemTag.ready_btn
        let clear_btn    = dsSysBit   "sysclear_btn"      true  sys  SystemTag.clear_btn
        let home_btn     = dsSysBit   "syshome_btn"       true  sys  SystemTag.home_btn

        let auto_lamp     = dsSysBit   "sysauto_lamp"       true  sys  SystemTag.auto_lamp
        let manual_lamp   = dsSysBit   "sysmanual_lamp"     true  sys  SystemTag.manual_lamp
        let drive_lamp    = dsSysBit   "sysdrive_lamp"      true  sys  SystemTag.drive_lamp
        let pause_lamp     = dsSysBit   "syspause_lamp"     true  sys  SystemTag.pause_lamp
        let emg_lamp      = dsSysBit   "sysemg_lamp"        true  sys  SystemTag.emg_lamp
        let test_lamp     = dsSysBit   "systest_lamp"       true  sys  SystemTag.test_lamp
        let ready_lamp    = dsSysBit   "sysready_lamp"      true  sys  SystemTag.ready_lamp
        let clear_lamp    = dsSysBit   "sysclear_lamp"      true  sys  SystemTag.clear_lamp
        let home_lamp     = dsSysBit   "syshome_lamp"       true  sys  SystemTag.home_lamp


        

        //let dtimeyy  = dsSysUint8 "_RTC_TIME[0]"  false sys  SystemTag.datet_yy         //ls xgi 현재시각[년도]
        //let dtimemm  = dsSysUint8 "_RTC_TIME[1]"  false sys  SystemTag.datet_mm         //ls xgi 현재시각[월]
        //let dtimedd  = dsSysUint8 "_RTC_TIME[2]"  false sys  SystemTag.datet_dd         //ls xgi 현재시각[일]
        //let dtimeh   = dsSysUint8 "_RTC_TIME[3]"  false sys  SystemTag.datet_h          //ls xgi 현재시각[시]
        //let dtimem   = dsSysUint8 "_RTC_TIME[4]"  false sys  SystemTag.datet_m          //ls xgi 현재시각[분]
        //let dtimes   = dsSysUint8 "_RTC_TIME[5]"  false sys  SystemTag.datet_s          //ls xgi 현재시각[초]
        //let dtimewk  = dsSysUint8 "_ms"                 //ls xgi 현재시각[요일]
        //let dtimeyk  = dsSysUint8 "_ms"                 //ls xgi 현재시각[년대]

        let tout     =
            let tout = dsSysUint32  "systout" true sys SystemTag.timeout 
            tout.Value <- RuntimeDS.TimeoutCall
            tout

        let pauseMonitor      = dsSysBit "pauseMonitor"     true  sys   SystemTag.pauseMonitor
        let autoMonitor       = dsSysBit "autoMonitor"      true  sys   SystemTag.autoMonitor   
        let manualMonitor     = dsSysBit "manualMonitor"    true  sys   SystemTag.manualMonitor 
        let driveMonitor      = dsSysBit "driveMonitor"     true  sys   SystemTag.driveMonitor  
        let errorMonitor      = dsSysBit "errorMonitor"     true  sys   SystemTag.errorMonitor   
        let emergencyMonitor  = dsSysBit "emergencyMonitor" true  sys   SystemTag.emergencyMonitor    
        let testMonitor       = dsSysBit "testMonitor"      true  sys   SystemTag.testMonitor   
        let readyMonitor      = dsSysBit "readyMonitor"     true  sys   SystemTag.readyMonitor  
        let idleMonitor       = dsSysBit "idleMonitor"      true  sys   SystemTag.idleMonitor  
        let originMonitor     = dsSysBit "originMonitor"    true  sys   SystemTag.originMonitor  
        let homingMonitor     = dsSysBit "homingMonitor"    true  sys   SystemTag.homingMonitor  
        let goingMonitor      = dsSysBit "goingMonitor"     true  sys   SystemTag.goingMonitor  
        
        let flicker20msec  = dsSysBit "_T20MS" true  sys   SystemTag.flicker20ms
        let flicker100msec = dsSysBit "_T100MS" true  sys  SystemTag.flicker100ms
        let flicker200msec = dsSysBit "_T200MS" true  sys  SystemTag.flicker200ms
        let flicker1sec    = dsSysBit "_T1S"   true  sys   SystemTag.flicker1s
        let flicker2sec    = dsSysBit "_T2S"   true  sys   SystemTag.flicker2s


        let sim            = dsSysBit "syssim"   true  sys SystemTag.sim
        let emulation      = dsSysBit "sysemulation"  true  sys   SystemTag.emulation


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
            | SystemTag.on         ->    on
            | SystemTag.off        ->    off
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
            | SystemTag.homingMonitor        ->    homingMonitor   
            | SystemTag.goingMonitor         ->    goingMonitor    
            
            | SystemTag.flicker20ms     -> flicker20msec
            | SystemTag.flicker100ms    -> flicker100msec
            | SystemTag.flicker200ms    -> flicker200msec
            | SystemTag.flicker1s       -> flicker1sec
            | SystemTag.flicker2s       -> flicker2sec

            
            | SystemTag.emulation       -> emulation
            | SystemTag.sim             ->    sim
            | _ -> failwithlog $"Error : GetSystemTag {st} type not support!!"

    [<Extension>]
    type SystemManagerExt =
        [<Extension>] static member OnTag (x:ISystem) = ((x:?>DsSystem).TagManager :?> SystemManager).GetSystemTag(SystemTag.on) :?> PlanVar<'T>
        [<Extension>] static member OffTag (x:ISystem) = ((x:?>DsSystem).TagManager :?> SystemManager).GetSystemTag(SystemTag.off) :?> PlanVar<'T>
       
