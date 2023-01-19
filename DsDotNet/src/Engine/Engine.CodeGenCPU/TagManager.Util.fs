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
            | DuFLOAT32 -> new DsTag<single>(name, v |> unbox)
            | DuFLOAT64 -> new DsTag<double>(name, v |> unbox)
            | DuINT8    -> new DsTag<int8>  (name, v |> unbox)
            | DuUINT8   -> new DsTag<uint8> (name, v |> unbox)
            | DuINT16   -> new DsTag<int16> (name, v |> unbox)
            | DuUINT16  -> new DsTag<uint16>(name, v |> unbox)
            | DuINT32   -> new DsTag<int32> (name, v |> unbox)
            | DuUINT32  -> new DsTag<uint32>(name, v |> unbox)
            | DuINT64   -> new DsTag<int64> (name, v |> unbox)
            | DuUINT64  -> new DsTag<uint64>(name, v |> unbox)
            | DuSTRING  -> new DsTag<string>(name, v |> unbox)
            | DuCHAR    -> new DsTag<char>  (name, v |> unbox)
            | DuBOOL    -> new DsTag<bool>  (name, v |> unbox)


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

    let dsTag (storages:Storages) name (dataType:DataType) =
        let name = getUniqueName name storages
        let t= createDsTag (name, dataType)
        storages.Add(t.Name, t)
        t


