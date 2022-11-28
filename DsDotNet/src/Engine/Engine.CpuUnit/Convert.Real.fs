[<AutoOpen>]
module Engine.Cpu.ConvertReal

open System.Linq
open System.Runtime.CompilerServices
open Engine.Cpu
 
[<AutoOpen>]
[<Extension>]
type StatementReal =
    
    
    [<Extension>] static member GetTaskEndStatement(realTag:DsTag, calls:DsTag seq) =
                    let sets  = calls.Select(fun f->f.Relay).ToTags()
                    Assign(FuncExt.DoAnd(sets), realTag.End)

    [<Extension>] static member GetInitStartStatement(realTag:DsTag)  =
                    let sets  = [realTag.Going;realTag.Origin].ToTags()
                    let rsts  = [realTag.Homing].ToTags()
                    let relay = realTag.Relay

                    Assign(FuncExt.GetRelayExpr(sets, rsts, relay), realTag.Relay)
    
     