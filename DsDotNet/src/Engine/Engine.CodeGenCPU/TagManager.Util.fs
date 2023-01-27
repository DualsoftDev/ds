namespace Engine.CodeGenCPU

open Engine.Core
open Engine.Common.FS
open System.Text.RegularExpressions
open System

[<AutoOpen>]
module TagManagerUtil =

        //'_' 시작 TAG 이름은 사용자 정의 불가 하여 앞쪽에 중복처리 문자
        // _(n)_ 하나씩 증가 ex)  _1_tagName, _2_tagName, _3_tagName
    let getUniqueName (storages:Storages) (name:string) =
        let removePrefix x = Regex.Replace(x, "^_\d+_", "")
        let rec unique (name:string) (cnt:int) (storages:Storages) =
            if storages.ContainsKey name
                then unique $"_{cnt+1}_{name |> removePrefix}" (cnt+1) storages
                else name

        unique name 0 storages

    let getValidName (name:string) =
        let ableChar(c:char) =
            Char.IsNumber(c) ||
            ['[';']'].ToResizeArray().Contains(c)
        [
            for c in name do
                if ableChar(c) || c.IsValidIdentifier()
                then yield c.ToString()
                else yield "_"

        ] |> String.concat ""

    let getPlcTagAbleName (name:string) (storages:Storages) =
        if name.StartsWith("_")
        then name |> getValidName
        else name |> getValidName |> getUniqueName  storages
        // UniqueName.generate <kwak> Storages 에서 중복 피하는걸 를 어떻게 쓸지 잘모르겠어요 ㅜ


    let private createPlanVarHelper(stg:Storages, name:string, dataType:DataType) : IStorage =
        let v = dataType.DefaultValue()
        let createParam () = {defaultStorageCreationParams(unbox v) with Name=name; IsGlobal=true;}
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
        let name = getPlcTagAbleName name storages
        let ts = TimerStruct.Create(TimerType.TON, storages,name, 0us, 0us, sys)
        ts

    let counter (storages:Storages) name sys =
        let name = getPlcTagAbleName name storages
        let cs = CTRStruct.Create(CounterType.CTR, storages, name, 0us, 0us, sys)
        cs

    let createPlanVar (storages:Storages) (name:string) (dataType:DataType)  =
        let name = getPlcTagAbleName name storages
        let t= createPlanVarHelper (storages, name, dataType)
        t

    let createPlanVarBool (storages:Storages) name  =
        createPlanVar storages name DuBOOL :?> PlanVar<bool>

    type InOut = | In | Out | Memory
    let createBridgeTag(stg:Storages, name, address, inOut:InOut, sys): ITag =
        let name =
            match inOut with
            | In  -> $"{name}_I"
            | Out -> $"{name}_O"
            | Memory -> failwithlog "error: Memory not supported "

        let plcName = getPlcTagAbleName name stg
        let t =
            let param = {defaultStorageCreationParams(false) with Name=plcName; Address=Some address; System=sys}
            (Tag(param) :> ITag)
        stg.Add(t.Name, t)
        t

