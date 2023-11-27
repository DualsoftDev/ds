namespace Engine.Core


[<AutoOpen>]
module HmiPackageModule =


    type HMIJob = {
        Name : string

        JobPush          : HMIPush 
        SensorLamps      : HMILamp array
    }

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

    type HMIApiItem = {
        Name : string
        ApiPushLamp       : HMIPushLamp   
        
        TrendOutErrorLamp : HMILamp        
        TimeOverErrorLamp : HMILamp        
        ShortErrorLamp    : HMILamp        
        OpenErrorLamp     : HMILamp        
        ErrorTotalLamp    : HMILamp        
    }


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

