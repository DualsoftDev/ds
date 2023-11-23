namespace Engine.CodeGenCPU

open Dual.Common.Core.FS
open System.Linq
open System.Runtime.CompilerServices
open System.Collections.Generic
open Engine.Core
open Engine.Info

[<AutoOpen>]
[<Extension>]
type TagHMIExt =

    [<Extension>]
    static member GetHmiTagPackage(sys:DsSystem, kindDescriptions:Dictionary<int, string>) =
        let getButtonsForReal(xs:Real seq) =
            xs.SelectMany(fun s->  [s.V.SF;s.V.RF;s.V.ON;s.V.OFF].Select(fun t->t.GetWebTag(kindDescriptions))) |> toArray
        let getButtonsForTaskDev(xs:TaskDev seq) =
            xs
            |> map(fun s-> 
                let intag = if s.InTag.IsNull() then null else s.InTag.GetWebTag(kindDescriptions)
                let outtag = if s.OutTag.IsNull() then null else s.OutTag.GetWebTag(kindDescriptions)
                intag, outtag )
            |> toArray

        let getButtons(xs:ButtonDef seq) = xs.Select(fun s-> s.InTag.GetWebTag(kindDescriptions), s.OutTag.GetWebTag(kindDescriptions)) |> toArray
        let getLamps(xs:LampDef seq) = xs.Select(fun s-> s.OutTag.GetWebTag(kindDescriptions)) |> toArray


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

            RealBtns           = getButtonsForReal(sys.GetVertices().OfType<Real>())
            DeviceBtns         = getButtonsForTaskDev(sys.Jobs.SelectMany(fun j->j.DeviceDefs))
        }
    
    
    