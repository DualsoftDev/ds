namespace Engine.CodeGenCPU

open Engine.Core
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

    let createDsVar(stg:Storages, name:string, dataType:DataType) : IStorage =
        let v = dataType.DefaultValue()
        let createParam () = {defaultStorageCreationParams(unbox v) with Name=name; }
        let t =
            match dataType with
            | DuINT8    -> PlanVar<int8>  (createParam()) :>IStorage
            | DuINT16   -> PlanVar<int16> (createParam()) :>IStorage
            | DuINT32   -> PlanVar<int32> (createParam()) :>IStorage
            | DuINT64   -> PlanVar<int64> (createParam()) :>IStorage
            | DuUINT8   -> PlanVar<uint8> (createParam()) :>IStorage
            | DuUINT16  -> PlanVar<uint16>(createParam()) :>IStorage
            | DuUINT32  -> PlanVar<uint32>(createParam()) :>IStorage
            | DuUINT64  -> PlanVar<uint64>(createParam()) :>IStorage
            | DuFLOAT32 -> PlanVar<single>(createParam()) :>IStorage
            | DuFLOAT64 -> PlanVar<double>(createParam()) :>IStorage
            | DuSTRING  -> PlanVar<string>(createParam()) :>IStorage
            | DuCHAR    -> PlanVar<char>  (createParam()) :>IStorage
            | DuBOOL    -> PlanVar<bool>  (createParam()) :>IStorage

        stg.Add(t.Name, t)
        t


    let timer  (storages:Storages)  name sys =
        let name = getUniqueName name storages
        let ts = TimerStruct.Create(TimerType.TON, storages,name, 0us, 0us, sys)
        ts

    let counter (storages:Storages) name sys =
        let name = getUniqueName name storages
        let cs = CTRStruct.Create(CounterType.CTR, storages, name, 0us, 0us, sys)
        cs

    let sysTag (storages:Storages) name (dataType:DataType)  =
        let name = getUniqueName name storages
        let t= createDsVar (storages, name, dataType)
        t

    let createPlanVar (storages:Storages) name sys =
        let name = getUniqueName name storages
        let t= createDsVar (storages, name, DuBOOL)
        t :?> PlanVar<bool>

    type InOut = | In | Out | Memory
    let createBridgeTag(stg:Storages, name, address, inOut:InOut, sys): IBridgeTag =
        let name = getUniqueName name stg
        let plcName =
            match inOut with
            | In  -> $"{name}_I"
            | Out -> $"{name}_O"
            | Memory -> failwithlog "error: Memory not supported "

        let t =
            let param = {defaultStorageCreationParams(false) with Name=plcName; Address=Some address; System=sys}
            (BridgeTag(param) :> IBridgeTag)
        stg.Add(t.Name, t)
        t

