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

    let createDsTag(stg:Storages, name:string, dataType:DataType) : IStorage =
        let v = dataType.DefaultValue()
        let t =
            match dataType with
            | DuFLOAT32 -> PlanTag<single>(name, v |> unbox) :>IStorage
            | DuFLOAT64 -> PlanTag<double>(name, v |> unbox) :>IStorage
            | DuINT8    -> PlanTag<int8>  (name, v |> unbox) :>IStorage
            | DuUINT8   -> PlanTag<uint8> (name, v |> unbox) :>IStorage
            | DuINT16   -> PlanTag<int16> (name, v |> unbox) :>IStorage
            | DuUINT16  -> PlanTag<uint16>(name, v |> unbox) :>IStorage
            | DuINT32   -> PlanTag<int32> (name, v |> unbox) :>IStorage
            | DuUINT32  -> PlanTag<uint32>(name, v |> unbox) :>IStorage
            | DuINT64   -> PlanTag<int64> (name, v |> unbox) :>IStorage
            | DuUINT64  -> PlanTag<uint64>(name, v |> unbox) :>IStorage
            | DuSTRING  -> PlanTag<string>(name, v |> unbox) :>IStorage
            | DuCHAR    -> PlanTag<char>  (name, v |> unbox) :>IStorage
            | DuBOOL    -> PlanTag<bool>  (name, v |> unbox) :>IStorage

        stg.Add(t.Name, t)
        t



    let timer  (storages:Storages)  name  =
        let name = getUniqueName name storages
        let ts = TimerStruct.Create(TimerType.TON, storages,name, 0us, 0us)
        ts

    let counter (storages:Storages) name =
        let name = getUniqueName name storages
        let cs = CTRStruct.Create(CounterType.CTR, storages, name, 0us, 0us)
        cs

    let sysTag (storages:Storages) name (dataType:DataType) =
        let name = getUniqueName name storages
        let t= createDsTag (storages, name, dataType)
        t

    let planTag (storages:Storages) name =
        let name = getUniqueName name storages
        let t= createDsTag (storages, name, DuBOOL)
        t :?> PlanTag<bool>

    type InOut = | In | Out | Memory
    let actionTag(stg:Storages, name, address, inOut:InOut): ITagWithAddress =
        let name = getUniqueName name stg
        let plcName =
            match inOut with
            | In  -> $"{name}_I"
            | Out -> $"{name}_O"
            | Memory -> failwithlog "error: Memory not supported "

        let t= (ActionTag(plcName, address, false) :> ITagWithAddress)
        stg.Add(t.Name, t)
        t

