namespace Engine.Core


[<AutoOpen>]
module HmiPackageModule =

    ///수동 동작의 단위 jobA = { Dev1.ADV, Dev2.ADV, ... }
    ///하나의 명령으로 복수의 디바이스 행위  
    ///Push & MultiLamp ex) 실린더1차 전진 Push, 실린더1차_Dev1,실린더1차_Dev2,실린더1차_Dev3 전진 램프들
    type HMIJob = {
        Name : string
        JobPushMutiLamp  : HMIPushMultiLamp 
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
        
        Jobs             : HMIJob array      
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
    }

    //디바이스 소속된 행위 Api
    type HMIApiItem = {
        Name : string
        
        TrendOutErrorLamp : HMILamp        
        TimeOverErrorLamp : HMILamp        
        ShortErrorLamp    : HMILamp        
        OpenErrorLamp     : HMILamp        
        ErrorTotalLamp    : HMILamp        
    }


    //모니터링 전용 (명령은 소속Job 통해서)
    type HMIDevice = {
        Name : string
        ApiItems   : HMIApiItem array  
    }

    //HMIPackage
    //   |- System - Flows
    //   |- IP         |- Reals    
    //   |- Ver             |- Jobs
    //   |- Devices
    //        |- ApiItems
    type HMIPackage = {
        IP                : string
        VersionDS         : string
        System            : HMISystem       //my     system
        Devices           : HMIDevice array //loaded system
    }

