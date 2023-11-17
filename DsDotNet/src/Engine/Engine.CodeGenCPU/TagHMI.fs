namespace Engine.CodeGenCPU

open Dual.Common.Core.FS
open System
open System.Linq
open System.Runtime.CompilerServices
open System.Reactive.Subjects
open Engine.Core

[<AutoOpen>]
module TagHMIModule =

    type HMIPush = TagWeb
    type HMILamp = TagWeb
    type HMIFlickerLamp = TagWeb
    type HMIButton = HMIPush*HMIFlickerLamp
    type HMIDevice = (HMIPush option)*(HMILamp option)  //input, output


    type HmiTagPackage = {
        AutoButtons      : HMIButton array 
        ManualButtons    : HMIButton array 
        DriveButtons     : HMIButton array 
        StopButtons      : HMIButton array 
        ClearButtons     : HMIButton array 
        EmergencyButtons : HMIButton array 
        TestButtons      : HMIButton array 
        HomeButtons      : HMIButton array 
        ReadyButtons     : HMIButton array 
        
        DriveLamps       : HMILamp array 
        AutoLamps        : HMILamp array 
        ManualLamps      : HMILamp array 
        StopLamps        : HMILamp array 
        EmergencyLamps   : HMILamp array 
        TestLamps        : HMILamp array 
        ReadyLamps       : HMILamp array 
        IdleLamps        : HMILamp array 

        RealBtns         : HMIPush array 
        DeviceBtns       : HMIDevice array 
        //JobBtns          : HMIPush array  나중에
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
                intag, outtag )

        let getButtons(xs:ButtonDef seq) = xs.Select(fun s-> s.InTag.GetWebTag(), s.OutTag.GetWebTag())
        let getLamps(xs:LampDef seq) = xs.Select(fun s-> s.OutTag.GetWebTag())
        {
            AutoButtons        = getButtons(sys.AutoButtons     ) |> toArray
            ManualButtons      = getButtons(sys.ManualButtons   ) |> toArray
            DriveButtons       = getButtons(sys.DriveButtons    ) |> toArray
            StopButtons        = getButtons(sys.StopButtons     ) |> toArray
            ClearButtons       = getButtons(sys.ClearButtons    ) |> toArray
            EmergencyButtons   = getButtons(sys.EmergencyButtons) |> toArray
            TestButtons        = getButtons(sys.TestButtons     ) |> toArray
            HomeButtons        = getButtons(sys.HomeButtons     ) |> toArray
            ReadyButtons       = getButtons(sys.ReadyButtons    ) |> toArray

            DriveLamps         = getLamps(sys.DriveLamps    ) |> toArray
            AutoLamps          = getLamps(sys.AutoLamps     ) |> toArray
            ManualLamps        = getLamps(sys.ManualLamps   ) |> toArray
            StopLamps          = getLamps(sys.StopLamps     ) |> toArray
            EmergencyLamps     = getLamps(sys.EmergencyLamps) |> toArray
            TestLamps          = getLamps(sys.TestLamps     ) |> toArray
            ReadyLamps         = getLamps(sys.ReadyLamps    ) |> toArray
            IdleLamps          = getLamps(sys.IdleLamps     ) |> toArray


            RealBtns           =  getButtonsForReal(sys.GetVertices().OfType<Real>())  |> toArray
            DeviceBtns         =  getButtonsForTaskDev(sys.Jobs.SelectMany(fun j->j.DeviceDefs))  |> toArray
            
        }
    
    
    