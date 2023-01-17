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
    type SystemManager (sys:DsSystem)  =
        let s = Storages()
        let on     = dsBit s "_on"  true
        let off    = dsBit s "_off" false
        let auto   = dsBit s "_auto" false
        let manual = dsBit s "_manual" false
        let drive  = dsBit s "_drive" false
        let stop   = dsBit s "_stop" false
        let emg    = dsBit s "_emg" false
        let test   = dsBit s "_test" false
        let ready  = dsBit s "_ready" false
        let clear  = dsBit s "_clear" false
        let home   = dsBit s "_home" false
        let dtimeyy  = dsInt s "_yy" 0
        let dtimemm  = dsInt s "_mm" 0
        let dtimedd  = dsInt s "_dd" 0
        let dtimeh   = dsInt s "_h" 0
        let dtimem   = dsInt s "_m" 0
        let dtimes   = dsInt s "_s" 0
        let dtimems  = dsInt s "_ms" 0
        let tout   = dsUint16 s "_tout" 10000us
        
        interface ITagManager with
            member x.Target = sys
        
        member val Storages = s

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
