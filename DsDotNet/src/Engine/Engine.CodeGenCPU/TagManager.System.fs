namespace Engine.CodeGenCPU

open System.Diagnostics
open Engine.Core
open System.Collections.Generic
open System.Runtime.CompilerServices

[<AutoOpen>]
module SystemManagerModule =
                         
    [<AutoOpen>]
    type SysBitTag = 
    |ON
    |OFF
    |AUTO
    |MANUAL
    |DRIVE
    |STOP
    |EMG
    |TEST 
    |READY
    |CLEAR
    |HOME

    [<AutoOpen>]
    type SysDataTimeTag = 
    |DATET_YY 
    |DATET_MM 
    |DATET_DD 
    |DATET_H 
    |DATET_M 
    |DATET_S 
    |DATET_MS

    [<AutoOpen>]
    type SysErrorTag = 
    |TIMEOUT
  
    /// DsSystem Manager : System Tag  를 관리하는 컨테이어
    type SystemManager (sys:DsSystem, stg:Storages)  =
            //시스템 Tag는 하위 시스템과 공유
        let dsSysBit  name init = if stg.ContainsKey(name) then stg[name] :?> DsTag<bool> else dsBit stg name init
        let dsSysInt  name init = if stg.ContainsKey(name) then stg[name] :?> DsTag<int>  else dsInt stg name init
        let dsSysUint name init = if stg.ContainsKey(name) then stg[name] :?> DsTag<uint16> else dsUint16 stg name init

        let on     = dsSysBit "_on"  true
        let off    = dsSysBit "_off" false
        let auto   = dsSysBit "_auto" false
        let manual = dsSysBit "_manual" false
        let drive  = dsSysBit "_drive" false
        let stop   = dsSysBit "_stop" false
        let emg    = dsSysBit "_emg" false
        let test   = dsSysBit "_test" false
        let ready  = dsSysBit "_ready" false
        let clear  = dsSysBit "_clear" false
        let home   = dsSysBit "_home" false
        let dtimeyy  = dsSysInt "_yy" 0
        let dtimemm  = dsSysInt "_mm" 0
        let dtimedd  = dsSysInt "_dd" 0
        let dtimeh   = dsSysInt "_h" 0
        let dtimem   = dsSysInt "_m" 0
        let dtimes   = dsSysInt "_s" 0
        let dtimems  = dsSysInt "_ms" 0
        let tout   = dsSysUint  "_tout" 10000us
        
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
            |DATET_MS   ->    dtimems  

        member f.GetSysErrTag(st:SysErrorTag)     = 
            match st with
            |TIMEOUT    ->    tout  
