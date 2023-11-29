namespace Engine.CodeGenCPU

open Dual.Common.Core.FS
open System.Linq
open System.Runtime.CompilerServices
open System.Collections.Generic
open Engine.Core
open Engine.Info
open System

// todo : remove me

//[<AutoOpen>]
//[<Extension>]
//type TagHMIExt =

//    [<Obsolete("Use GetHMIPackage")>]
//    [<Extension>]
//    static member GetHmiTagPackage(sys:DsSystem, kindDescriptions:Dictionary<int, string>) =
//        let getButtonsForReal(xs:Real seq) =
//            xs.SelectMany(fun s->  [s.V.SF;s.V.RF;s.V.ON;s.V.OFF].Select(fun t->t.GetWebTag(kindDescriptions))) |> toArray
//        let getButtonsForTaskDev(xs:TaskDev seq) =
//            xs
//            |> map(fun s-> 
//                let intag = if s.InTag.IsNull() then null else s.InTag.GetWebTag(kindDescriptions)
//                let outtag = if s.OutTag.IsNull() then null else s.OutTag.GetWebTag(kindDescriptions)
//                intag, outtag )
//            |> toArray

//        let getButtons(xs:ButtonDef seq) = xs.Select(fun s-> s.InTag.GetWebTag(kindDescriptions), s.OutTag.GetWebTag(kindDescriptions)) |> toArray
//        let getLamps(xs:LampDef seq) = xs.Select(fun s-> s.OutTag.GetWebTag(kindDescriptions)) |> toArray


//        {
//            AutoButtons        = getButtons(sys.AutoHWButtons     )
//            ManualButtons      = getButtons(sys.ManualHWButtons   )
//            DriveButtons       = getButtons(sys.DriveHWButtons    )
//            StopButtons        = getButtons(sys.StopHWButtons     )
//            ClearButtons       = getButtons(sys.ClearHWButtons    )
//            EmergencyButtons   = getButtons(sys.EmergencyHWButtons)
//            TestButtons        = getButtons(sys.TestHWButtons     )
//            HomeButtons        = getButtons(sys.HomeHWButtons     )
//            ReadyButtons       = getButtons(sys.ReadyHWButtons    )

//            DriveLamps         = getLamps(sys.DriveHWLamps    )
//            AutoLamps          = getLamps(sys.AutoHWLamps     )
//            ManualLamps        = getLamps(sys.ManualHWLamps   )
//            StopLamps          = getLamps(sys.StopHWLamps     )
//            EmergencyLamps     = getLamps(sys.EmergencyHWLamps)
//            TestLamps          = getLamps(sys.TestHWLamps     )
//            ReadyLamps         = getLamps(sys.ReadyHWLamps    )
//            IdleLamps          = getLamps(sys.IdleHWLamps     )

//            RealBtns           = getButtonsForReal(sys.GetVertices().OfType<Real>())
//            DeviceBtns         = getButtonsForTaskDev(sys.Jobs.SelectMany(fun j->j.DeviceDefs))
//        }
    
    
    