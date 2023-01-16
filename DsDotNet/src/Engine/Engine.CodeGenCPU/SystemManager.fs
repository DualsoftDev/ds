namespace Engine.CodeGenCPU

open System.Diagnostics
open Engine.Core
open System.Collections.Generic
open System.Runtime.CompilerServices

[<AutoOpen>]
module SystemManagerModule =
                                            
  
    /// DsSystem Manager : System Tag  를 관리하는 컨테이어
    type SystemManager (s:DsSystem)  =
    
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
        let home   = dsBit s "home" false
        let dtimeyy  = dsInt s "_yy" 0
        let dtimemm  = dsInt s "_mm" 0
        let dtimedd  = dsInt s "_dd" 0
        let dtimeh   = dsInt s "_h" 0
        let dtimem   = dsInt s "_m" 0
        let dtimes   = dsInt s "_s" 0
        let dtimems  = dsInt s "_ms" 0
        let tout   = dsUint16 s "_tout" 10000us
        let flowManager  = HashSet<FlowManager>()
        do 
            s.Flows 
            |> Seq.iter(fun f -> flowManager.Add(FlowManager(f)) |>ignore)

        member s.FlowManager  = flowManager   

        member s._on       = on     
        member s._off      = off    
        member s._auto     = auto   
        member s._manual   = manual 
        member s._drive    = drive  
        member s._stop     = stop   
        member s._emg      = emg    
        member s._test     = test   
        member s._ready    = ready  
        member s._clear    = clear  
        member s._home     = home   
        member s._dtimeyy  = dtimeyy
        member s._dtimemm  = dtimemm
        member s._dtimedd  = dtimedd
        member s._dtimeh   = dtimeh 
        member s._dtimem   = dtimem 
        member s._dtimes   = dtimes 
        member s._dtimems  = dtimems
        member s._tout     = tout   
