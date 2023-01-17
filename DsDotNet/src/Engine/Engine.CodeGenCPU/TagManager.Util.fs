namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open System
open Engine.Common.FS

[<AutoOpen>]
module TagManagerUtil =

    let bit (v:Vertex) (storages:Storages) mark flag  = 
        let t = DsBit($"{v.QualifiedName}({mark})", false, v, flag)
        storages.Add(t.Name, t) 
        t

    let timer (v:Vertex) (storages:Storages)  mark flag = 
        let name =  $"{v.QualifiedName}({mark}:TON)"
        let ts = TimerStruct.Create(TimerType.TON, storages,name, 0us, 0us) 
        DsTimer($"{v.QualifiedName}({mark})", false, v, flag, ts)
    
    let counter (v:Vertex) (storages:Storages) mark flag = 
        let name = $"{v.QualifiedName}({mark}:CTR)"
        let cs = CTRStruct.Create(CounterType.CTR, storages, name, 0us, 0us) 
        DsCounter($"{v.QualifiedName}({mark})", false, v, flag, cs)
        
    let dsBit (storages:Storages) name init = 
        //if storages.ContainsKey(name) 
        //then storages[name] :?> DsTag<bool>
        //else 
            let t = DsTag<bool>($"{name}", init)
            storages.Add(t.Name, t) |> ignore
            t

    let dsInt (storages:Storages) name init = 
        //if storages.ContainsKey(name) 
        //then storages[name] :?> DsTag<int>
        //else 
            let t = DsTag<int>($"{name}", init)
            storages.Add(t.Name, t) |> ignore
            t

    let dsUint16 (storages:Storages) name init = 
        //if storages.ContainsKey(name) 
        //then storages[name] :?> DsTag<uint16>
        //else 
            let t = DsTag<uint16>($"{name}", init)
            storages.Add(t.Name, t) |> ignore
            t