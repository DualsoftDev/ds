namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open System
open Engine.Common.FS
open System.Text.RegularExpressions

[<AutoOpen>]
module TagManagerUtil =

        //'_' 시작 TAG 이름은 사용자 정의 불가 하여 앞쪽에 중복처리 문자 
        // _(n)_ 하나씩 증가 ex)  _1_tagName, _2_tagName, _3_tagName
    let getUniqueName (name:string) (storages:Storages) = 
        let removePrefix x = Regex.Replace(x, "^_\d+_", "")
        let rec unique (name:string) (cnt:int) (storages:Storages) = 
            if storages.ContainsKey name 
                then unique $"_{cnt}_{name |> removePrefix}" (cnt+1) storages
                else name

        unique name 0 storages
            
    let bit (v:Vertex) (storages:Storages) mark flag  = 
        let name = getUniqueName $"{v.QualifiedName}({mark})" storages
        let t = DsBit(name, false, v, flag)
        storages.Add(t.Name, t) 
        t

    let timer (v:Vertex) (storages:Storages)  mark flag = 
        let name = getUniqueName $"{v.QualifiedName}({mark}:TON)" storages
        let ts = TimerStruct.Create(TimerType.TON, storages,name, 0us, 0us) 
        DsTimer($"{v.QualifiedName}({mark})", false, v, flag, ts)
    
    let counter (v:Vertex) (storages:Storages) mark flag = 
        let name = getUniqueName $"{v.QualifiedName}({mark}:CTR)" storages
        let cs = CTRStruct.Create(CounterType.CTR, storages, name, 0us, 0us) 
        DsCounter($"{v.QualifiedName}({mark})", false, v, flag, cs)
        
    let dsBit (storages:Storages) name init = 
        let name = getUniqueName name storages
        let t = DsTag<bool>($"{name}", init)
        storages.Add(t.Name, t) |> ignore 
        t

    let dsInt (storages:Storages) name init = 
        let name = getUniqueName name storages
        let t = DsTag<int>($"{name}", init)
        storages.Add(t.Name, t) |> ignore
        t

    let dsUint16 (storages:Storages) name init = 
        let name = getUniqueName name storages
        let t = DsTag<uint16>($"{name}", init)
        storages.Add(t.Name, t) |> ignore
        t
