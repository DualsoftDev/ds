namespace Engine.Core

open System.Runtime.CompilerServices
open System.Collections.Generic
open Dual.Common.Core.FS


[<AutoOpen>]
module HmiPackageModule =

    //디바이스 소속된 행위 Api
    type HMIApiItem = {
        Name : string
        
        TrendOutErrorLamp : HMILamp        
        TimeOverErrorLamp : HMILamp        
        ShortErrorLamp    : HMILamp        
        OpenErrorLamp     : HMILamp        
        ErrorTotalLamp    : HMILamp        
    } with
        member x.CollectTags () =
            seq {
                yield x.TrendOutErrorLamp        
                yield x.TimeOverErrorLamp        
                yield x.ShortErrorLamp           
                yield x.OpenErrorLamp          
                yield x.ErrorTotalLamp   
            }


    //모니터링 전용 (명령은 소속Job 통해서)
    type HMIDevice = {
        Name : string
        ApiItems   : HMIApiItem array  
    } with
        member x.CollectTags () = x.ApiItems |> Seq.collect (fun ai -> ai.CollectTags())

    ///수동 동작의 단위 jobA = { Dev1.ADV, Dev2.ADV, ... }
    ///하나의 명령으로 복수의 디바이스 행위  
    ///Push & MultiLamp ex) 실린더1차 전진 Push, 실린더1차_Dev1,실린더1차_Dev2,실린더1차_Dev3 전진 램프들
    type HMIJob = {
        Name : string
        JobPushMutiLamp  : HMIPushMultiLamp 
    } with
        member x.CollectTags () =
            let push, lamps = x.JobPushMutiLamp
            seq {
                yield push
                yield! lamps
            }

    ///작업 단위 FlowA = { Real1, Real2, ... }
    type HMIReal = {
        Name : string

        StartPush        : HMIPush
        ResetPush        : HMIPush
        ONPush           : HMIPush
        OFFPush          : HMIPush
                         
        ReadyLamp        : HMILamp 
        GoingLamp        : HMILamp 
        FinishLamp       : HMILamp 
        HomingLamp       : HMILamp 
        OriginLamp       : HMILamp 
        PauseLamp        : HMILamp 
        ErrorTxLamp      : HMILamp 
        ErrorRxLamp      : HMILamp

        Devices          : HMIDevice array //loaded system
        Jobs             : HMIJob array      
    } with
        member x.CollectTags () =
            seq {
                yield x.StartPush        
                yield x.ResetPush        
                yield x.ONPush           
                yield x.OFFPush          

                yield x.ReadyLamp   
                yield x.GoingLamp   
                yield x.FinishLamp  
                yield x.HomingLamp  
                yield x.OriginLamp  
                yield x.PauseLamp   
                yield x.ErrorTxLamp 
                yield x.ErrorRxLamp
                        //yield! x.Devices |> Seq.collect (fun d -> d.CollectTags())  필요시 HMIPackage Devices : string array 이름으로 별도로 찾아야함 
                yield! x.Jobs |> Seq.collect (fun j -> j.CollectTags())
            }


    ///공정흐름 단위 SystemA = { Flow1, Flow2, ... }
    type HMIFlow = {
        Name             : string
        AutoManualSelect : HMISelect      
        DrivePush        : HMIPush     
        StopPush         : HMIPush      
        ClearPush        : HMIPush     
        EmergencyPush    : HMIPush 
        TestPush         : HMIPush      
        HomePush         : HMIPush      
        ReadyPush        : HMIPush   
        
        DriveLamp        : HMILamp 
        AutoLamp         : HMILamp 
        ManualLamp       : HMILamp 
        StopLamp         : HMILamp 
        EmergencyLamp    : HMILamp 
        TestLamp         : HMILamp 
        ReadyLamp        : HMILamp 
        IdleLamp         : HMILamp 

        Reals            : HMIReal array      
    } with
        member x.CollectTags () =
            seq {
                yield fst x.AutoManualSelect
                yield snd x.AutoManualSelect
                yield x.DrivePush
                yield x.StopPush
                yield x.ClearPush
                yield x.EmergencyPush
                yield x.TestPush
                yield x.HomePush
                yield x.ReadyPush

                yield x.DriveLamp        
                yield x.AutoLamp         
                yield x.ManualLamp       
                yield x.StopLamp         
                yield x.EmergencyLamp    
                yield x.TestLamp         
                yield x.ReadyLamp        
                yield x.IdleLamp         

                yield! x.Reals |> Seq.collect (fun r->r.CollectTags())
            }

    
    //명령 전용 (모니터링은 개별Flow 통해서)
    type HMISystem = {
        Name : string
        AutoManualSelect  : HMISelect      
        DrivePush         : HMIPush     
        StopPush          : HMIPush      
        ClearPush         : HMIPush     
        EmergencyPush     : HMIPush 
        TestPush          : HMIPush      
        HomePush          : HMIPush      
        ReadyPush         : HMIPush 

        Flows             : HMIFlow array
    } with
        member x.CollectTags () =
            seq {
                yield fst x.AutoManualSelect
                yield snd x.AutoManualSelect
                yield x.DrivePush
                yield x.StopPush
                yield x.ClearPush
                yield x.EmergencyPush
                yield x.TestPush
                yield x.HomePush
                yield x.ReadyPush
                yield! x.Flows |> Seq.collect (fun f->f.CollectTags())
            }

  

    //HMIPackage
    //   |- System - Flows
    //   |- IP         |- Reals    
    //   |- Ver             |- Jobs
    //   |- Devices
    //        |- ApiItems
    type HMIPackage(ip: string, versionDS: string, system: HMISystem, devices: HMIDevice array) =
        let tagMap = Dictionary<(string*int), TagWeb>()     // FQDN, Kind -> TagWeb
        member val IP = ip with get, set
        member val VersionDS = versionDS  with get, set
        member val System = system  with get, set
        member val Devices = devices  with get, set

        member x.BuildTagMap () =
            printfn "--------- Building TagMap"
            tagMap.Clear()
            seq {
                yield! x.System.CollectTags()   
                yield! x.Devices |> Seq.collect (fun d->d.CollectTags()) 
            } |> iter (fun t ->
                let key = (t.Name, t.Kind)
                match tagMap.TryGetValue(key) with
                | true, tag when t <> tag -> ()
                | true, tag ->
                    verifyM "Duplicate Tag" (tag = t) 
                | _ -> tagMap.Add(key, t))         //cache.[key] <- getItem()

        member x.UpdateTag(name:string, kind:int, newValue:obj) =
            printfn $"--------- Updating Tag: {name}:{kind}={newValue}"
            tagMap[(name, kind)].SetValue(newValue)

        member x.UpdateTag(newTag:TagWeb) =
            x.UpdateTag(newTag.Name, newTag.Kind, newTag.Value)


[<Extension>]
type HmiPackageModuleExt =
    [<Extension>] static member GetAuto (x:HMIFlow) : HMIPush = fst x.AutoManualSelect
    [<Extension>] static member GetManual (x:HMIFlow) : HMIPush = snd x.AutoManualSelect

    [<Extension>] static member GetAuto (x:HMISystem) : HMIPush = fst x.AutoManualSelect
    [<Extension>] static member GetManual (x:HMISystem) : HMIPush = snd x.AutoManualSelect
