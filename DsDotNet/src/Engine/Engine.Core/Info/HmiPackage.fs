namespace Engine.Core

open System.Runtime.CompilerServices
open System.Collections.Generic
open Dual.Common.Core.FS


[<AutoOpen>]
module HmiPackageModule =

    let getPushLampTags     (x:HMIPushLamp)     = seq {yield fst x; yield snd x}
    let getPushLampModeTags (x:HMIPushLampMode) = seq {yield! getPushLampTags(fst x); yield snd x; }
    
    //디바이스 를 호출한 행위 
    type HMICall = {
        Name : string
        
        TimeOnShortageErrorLamp : HMILamp        
        TimeOnOverErrorLamp : HMILamp       
        TimeOffShortageErrorLamp : HMILamp        
        TimeOffOverErrorLamp : HMILamp      
        ShortErrorLamp    : HMILamp        
        OpenErrorLamp     : HMILamp        
        ErrorTotalLamp    : HMILamp   

    } with
        member x.CollectTags () =
            seq {
                yield x.TimeOnShortageErrorLamp        
                yield x.TimeOnOverErrorLamp      
                yield x.TimeOffShortageErrorLamp        
                yield x.TimeOffOverErrorLamp    
                yield x.ShortErrorLamp           
                yield x.OpenErrorLamp          
                yield x.ErrorTotalLamp   
            }


    //모니터링 전용 (명령은 소속Job 통해서)
    type HMIDevice = {
        Name : string
        ActionIN : HMILamp option     
        ActionOUT : HMILamp option     
    } 
    with 
        member x.CollectTags () =
            seq {
                    if x.ActionIN.IsSome then yield x.ActionIN.Value        
                    if x.ActionOUT.IsSome then yield x.ActionOUT.Value
                }

    ///수동 동작의 단위 jobA = { Dev1.ADV, Dev2.ADV, ... }
    ///하나의 명령으로 복수의 디바이스 행위  
    ///Push & MultiLamp ex) 실린더1차 전진 Push, 실린더1차_Dev1,실린더1차_Dev2,실린더1차_Dev3 전진 램프들
    type HMIJob = {
        Name : string
        JobPushMutiLamp   : HMIPushMultiLamp   //강제시작 forceStart & inTags
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
        Error            : HMILamp 
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
                yield x.Error
                        //yield! x.Devices |> Seq.collect (fun d -> d.CollectTags())  필요시 HMIPackage Devices : string array 이름으로 별도로 찾아야함 
                yield! x.Jobs |> Seq.collect (fun j -> j.CollectTags())
            }


    ///공정흐름 단위 SystemA = { Flow1, Flow2, ... }
    type HMIFlow = {
        Name    : string
        AutoManualSelectLampMode : HMISelectLampMode      
        DrivePushLampMode        : HMIPushLampMode     
        EmergencyPushLampMode    : HMIPushLampMode 
        TestPushLampMode         : HMIPushLampMode      
        ReadyPushLampMode        : HMIPushLampMode  

        ClearPushLamp            : HMIPushLamp     
        PausePushLamp            : HMIPushLamp 
        
        IdleLampMode             : HMILamp 
        OriginLampMode           : HMILamp 
        ErrorLampMode            : HMILamp 

        Reals            : HMIReal array
        /// Flow 내의 direct child calls
        DirectCalls      : HMICall array
    } with
        member x.CollectTags () =
            seq {
                yield! getPushLampModeTags (fst x.AutoManualSelectLampMode)
                yield! getPushLampModeTags (snd x.AutoManualSelectLampMode)
                yield! getPushLampModeTags x.DrivePushLampMode
                yield! getPushLampModeTags x.EmergencyPushLampMode
                yield! getPushLampModeTags x.TestPushLampMode
                yield! getPushLampModeTags x.ReadyPushLampMode
                yield! getPushLampTags x.ClearPushLamp
                yield! getPushLampTags x.PausePushLamp
                yield x.IdleLampMode
                yield x.OriginLampMode
                yield x.ErrorLampMode

                yield! x.Reals |> Seq.collect (fun r->r.CollectTags())
            }

    
    //명령 전용 (모니터링은 개별Flow 통해서)
    type HMISystem = {
        Name : string
        AutoManualSelectLamp  : HMISelectLamp      
        DrivePushLamp         : HMIPushLamp     
        PausePushLamp         : HMIPushLamp      
        ClearPushLamp         : HMIPushLamp     
        EmergencyPushLamp     : HMIPushLamp 
        TestPushLamp          : HMIPushLamp      
        HomePushLamp          : HMIPushLamp      
        ReadyPushLamp         : HMIPushLamp 

        Flows                 : HMIFlow array
        Jobs                  : HMIJob array
    } with
        member x.CollectTags () =
            seq {
                yield! getPushLampTags (fst x.AutoManualSelectLamp)
                yield! getPushLampTags (snd x.AutoManualSelectLamp)
                yield! getPushLampTags x.DrivePushLamp
                yield! getPushLampTags x.PausePushLamp
                yield! getPushLampTags x.ClearPushLamp
                yield! getPushLampTags x.EmergencyPushLamp
                yield! getPushLampTags x.TestPushLamp
                yield! getPushLampTags x.HomePushLamp
                yield! getPushLampTags x.ReadyPushLamp
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
            logDebug "Building TagMap"
            tagMap.Clear()
            seq {
                yield! x.System.CollectTags()   
                yield! x.Devices |> Seq.collect (fun d->d.CollectTags()) 
            } |> iter (fun t ->
                let key = (t.Name, t.Kind)
                match tagMap.TryGetValue(key) with
                | true, tag when t <> tag -> ()
                | true, _ ->
                    logWarn $"Duplicate Tag: {t.Name}/{t.KindDescription}"
                | _ -> tagMap.Add(key, t))         

        member x.UpdateTag(name:string, kind:int, newValue:obj) =
            logDebug $"--------- Updating Tag: {name}:{kind}={newValue}"
            tagMap[(name, kind)].SetValue(newValue)

        member x.UpdateTag(newTag:TagWeb) =
            x.UpdateTag(newTag.Name, newTag.Kind, newTag.Value)


[<Extension>]
type HmiPackageModuleExt =
    ///HMIPushLampMode*HMIPushLampMode //ex)  selectPushLampA*selectModeA/selectPushLampB*selectModeB 
    [<Extension>] static member GetAuto (x:HMIFlow) : TagWeb = fst x.AutoManualSelectLampMode |> fst |> fst
    [<Extension>] static member GetManual (x:HMIFlow) : TagWeb = snd x.AutoManualSelectLampMode |> fst |> fst

    [<Extension>] static member GetAuto (x:HMISystem) : TagWeb = fst x.AutoManualSelectLamp |> fst
    [<Extension>] static member GetManual (x:HMISystem) : TagWeb = snd x.AutoManualSelectLamp |> fst
    
    
    [<Extension>] static member GetButton (x:HMIPushLampMode) : TagWeb = fst x |> fst
    [<Extension>] static member GetButton (x:HMIPushLamp) : TagWeb = fst x 
