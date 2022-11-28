[<AutoOpen>]
module Engine.Cpu.ConvertReal

open System.Linq
open System.Runtime.CompilerServices
open Engine.Cpu
 
[<AutoOpen>]
[<Extension>]
type StatementReal =
    
    
    [<Extension>] static member TaskEnd(real:DsTag, calls:DsTag seq) =
                    let sets  = calls.Select(fun f->f.Relay).ToTags()
                    Assign(FuncExt.DoAnd(sets), real.End)

    [<Extension>] static member InitStart(real:DsTag)  =
                    let sets  = [real.Going;real.Origin].ToTags()
                    let rsts  = [real.Homing].ToTags()
                    let relay = real.Relay

                    Assign(FuncExt.DoRelay(sets, rsts, relay), real.Relay)
    
     