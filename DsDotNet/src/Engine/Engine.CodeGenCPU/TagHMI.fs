namespace Engine.CodeGenCPU

open Dual.Common.Core.FS
open System
open System.Linq
open System.Runtime.CompilerServices
open System.Reactive.Subjects
open Engine.Core

[<AutoOpen>]
module TagHMIModule =

    type HMIButton = TagWeb*TagWeb  //btnPush btnFlicker
    type HMIPush = TagWeb           //btnPush
    type HMILamp = TagWeb           //lamp
    type HMIDevice = (TagWeb option)*(TagWeb option)  //input, output


    type HmiTagPackage = {
        AutoButtons      : HMIButton seq 
        ManualButtons    : HMIButton seq 
        DriveButtons     : HMIButton seq 
        StopButtons      : HMIButton seq 
        ClearButtons     : HMIButton seq 
        EmergencyButtons : HMIButton seq 
        TestButtons      : HMIButton seq 
        HomeButtons      : HMIButton seq 
        ReadyButtons     : HMIButton seq 
        
        DriveLamps       : HMILamp seq 
        AutoLamps        : HMILamp seq 
        ManualLamps      : HMILamp seq 
        StopLamps        : HMILamp seq 
        EmergencyLamps   : HMILamp seq 
        TestLamps        : HMILamp seq 
        ReadyLamps       : HMILamp seq 
        IdleLamps        : HMILamp seq 

        RealBtns         : HMIPush seq 
        DeviceBtns       : HMIDevice seq 
        //JobBtns          : HMIPush seq  나중에
    }


[<AutoOpen>]
[<Extension>]
type TagHMIExt =

    [<Extension>]
    static member GetHmiTagPackage(sys:DsSystem) =
        let getButtonsForReal(xs:Real seq) =
                        xs.SelectMany(fun s->  [s.V.SF;s.V.RF;s.V.ON;s.V.OFF].Select(fun t->t.GetWebTag()))
        let getButtonsForTaskDev(xs:TaskDev seq) =
                        xs.Select(fun s-> 
                                let intag = if s.InTag.IsNull() then None else  Some (s.InTag.GetWebTag())
                                let outtag = if s.OutTag.IsNull() then None else  Some (s.OutTag.GetWebTag())
                                intag, outtag
                                )

        let getButtons(xs:ButtonDef seq) = xs.Select(fun s-> s.InTag.GetWebTag(), s.OutTag.GetWebTag())
        let getLamps(xs:LampDef seq) = xs.Select(fun s-> s.OutTag.GetWebTag())
        {
            AutoButtons        = getButtons(sys.AutoButtons     )
            ManualButtons      = getButtons(sys.ManualButtons   )
            DriveButtons       = getButtons(sys.DriveButtons    )
            StopButtons        = getButtons(sys.StopButtons     )
            ClearButtons       = getButtons(sys.ClearButtons    )
            EmergencyButtons   = getButtons(sys.EmergencyButtons)
            TestButtons        = getButtons(sys.TestButtons     )
            HomeButtons        = getButtons(sys.HomeButtons     )
            ReadyButtons       = getButtons(sys.ReadyButtons    )

            DriveLamps         = getLamps(sys.DriveLamps    )
            AutoLamps          = getLamps(sys.AutoLamps     )
            ManualLamps        = getLamps(sys.ManualLamps   )
            StopLamps          = getLamps(sys.StopLamps     )
            EmergencyLamps     = getLamps(sys.EmergencyLamps)
            TestLamps          = getLamps(sys.TestLamps     )
            ReadyLamps         = getLamps(sys.ReadyLamps    )
            IdleLamps          = getLamps(sys.IdleLamps     )


            RealBtns           =  getButtonsForReal(sys.GetVertices().OfType<Real>())
            DeviceBtns         =  getButtonsForTaskDev(sys.Jobs.SelectMany(fun j->j.DeviceDefs))
            
        }
    
    
    