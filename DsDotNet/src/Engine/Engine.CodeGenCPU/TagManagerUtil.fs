namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open System
open Engine.Common.FS

[<AutoOpen>]
module TagManagerUtil =

    let bit (v:Vertex)  mark flag  = 
        let t = DsBit($"{v.QualifiedName}({mark})", false, v, flag)
        v.Parent.GetSystem().Storages.Add(t.Name, t) 
        t

    let timer (v:Vertex)  mark flag = 
        let storages = v.Parent.GetSystem().Storages
        let ts = TimerStruct.Create(TimerType.TON, storages, $"{v.QualifiedName}({mark}:TON)", 0us, 0us) 
        DsTimer($"{v.QualifiedName}({mark})", false, v, flag, ts)
    
    let counter (v:Vertex)  mark flag = 
        let storages = v.Parent.GetSystem().Storages
        let cs = CTRStruct.Create(CounterType.CTR, storages, $"{v.QualifiedName}({mark}:CTR)", 0us, 0us) 
        DsCounter($"{v.QualifiedName}({mark})", false, v, flag, cs)
        
    let dsBit (system:DsSystem) name init = 
        if system.Storages.ContainsKey(name) 
        then system.Storages[name] :?> DsTag<bool>
        else 
            let t = DsTag<bool>($"{name}", init)
            system.Storages.Add(t.Name, t) |> ignore
            t

    let dsInt (system:DsSystem) name init = 
        if system.Storages.ContainsKey(name) 
        then system.Storages[name] :?> DsTag<int>
        else 
            let t = DsTag<int>($"{name}", init)
            system.Storages.Add(t.Name, t) |> ignore
            t

    let dsUint16 (system:DsSystem) name init = 
        if system.Storages.ContainsKey(name) 
        then system.Storages[name] :?> DsTag<uint16>
        else 
            let t = DsTag<uint16>($"{name}", init)
            system.Storages.Add(t.Name, t) |> ignore
            t
