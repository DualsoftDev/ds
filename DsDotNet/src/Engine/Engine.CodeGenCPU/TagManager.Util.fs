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
                then unique $"_{cnt+1}_{name |> removePrefix}" (cnt+1) storages
                else name

        unique name 0 storages

    let createDsTag(name:string, dataType:DataType) : IStorage =
            let v = dataType.DefaultValue()
            match dataType with
            | DuFLOAT32 -> new PlanTag<single>(name, v |> unbox)
            | DuFLOAT64 -> new PlanTag<double>(name, v |> unbox)
            | DuINT8    -> new PlanTag<int8>  (name, v |> unbox)
            | DuUINT8   -> new PlanTag<uint8> (name, v |> unbox)
            | DuINT16   -> new PlanTag<int16> (name, v |> unbox)
            | DuUINT16  -> new PlanTag<uint16>(name, v |> unbox)
            | DuINT32   -> new PlanTag<int32> (name, v |> unbox)
            | DuUINT32  -> new PlanTag<uint32>(name, v |> unbox)
            | DuINT64   -> new PlanTag<int64> (name, v |> unbox)
            | DuUINT64  -> new PlanTag<uint64>(name, v |> unbox)
            | DuSTRING  -> new PlanTag<string>(name, v |> unbox)
            | DuCHAR    -> new PlanTag<char>  (name, v |> unbox)
            | DuBOOL    -> new PlanTag<bool>  (name, v |> unbox)


    //let bit (v:Vertex) (storages:Storages) mark   =
    //    let name = getUniqueName $"{v.QualifiedName}({mark})" storages
    //    let t = PlanTag(name, false)
    //    storages.Add(t.Name, t)
    //    t

    let timer  (storages:Storages)  name  =
       // let name = getUniqueName $"{v.QualifiedName}({mark}:TON)" storages
        let name = getUniqueName name storages
        let ts = TimerStruct.Create(TimerType.TON, storages,name, 0us, 0us)
        ts

    let counter (storages:Storages) name =
        let name = getUniqueName name storages
        let cs = CTRStruct.Create(CounterType.CTR, storages, name, 0us, 0us)
        cs

    let sysTag (storages:Storages) name (dataType:DataType) =
        let name = getUniqueName name storages
        let t= createDsTag (name, dataType)
        storages.Add(t.Name, t)
        t

    let planTag (storages:Storages) name =
        let name = getUniqueName name storages
        let t= createDsTag (name, DuBOOL)
        storages.Add(t.Name, t)
        t :?> PlanTag<bool>


