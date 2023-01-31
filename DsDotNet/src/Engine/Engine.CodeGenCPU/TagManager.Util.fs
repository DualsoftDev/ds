namespace Engine.CodeGenCPU

open Engine.Core
open Engine.Common.FS
open System.Text.RegularExpressions
open System
open Engine.Common.FS.ForwardDecl.ShowForwardDeclSample

[<AutoOpen>]
module TagManagerUtil =

    //'_' 시작 TAG 이름은 사용자 정의 불가 하여 앞쪽에 중복처리 문자
    // _(n)_ 하나씩 증가 ex)  _1_tagName, _2_tagName, _3_tagName
    //UniqueName.generate 대신사용
    //let getUniqueName (storages:Storages) (name:string) =
    //    let removePrefix x = Regex.Replace(x, "^_\d+_", "")
    //    let rec unique (name:string) (cnt:int) (storages:Storages) =
    //        if storages.ContainsKey name
    //            then unique $"_{cnt+1}_{name |> removePrefix}" (cnt+1) storages
    //            else name

    //    unique name 0 storages

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
        let vName = name |> getValidName
        if name.StartsWith("_") then
            vName
        else
            let rec generateUntilValid() =
                let candidate = UniqueName.generate vName
                if storages.ContainsKey candidate then
                    generateUntilValid()
                else
                    candidate
            generateUntilValid()


    /// address :
    /// 1. None 이면 자동으로 주소를 할당하지 않음
    /// 2. "" 이면 자동으로 주소를 할당
    /// 3. 그외의 문자열이면 그것 자체의 주소를 사용
    let private createPlanVarHelper(stg:Storages, name:string, dataType:DataType, fillAutoAddress:bool) : IStorage =
        let v = dataType.DefaultValue()
        let address = if fillAutoAddress then Some "" else None
        let createParam () = {defaultStorageCreationParams(unbox v) with Name=name; IsGlobal=true; Address=address}
        let t =
            match dataType with
            | DuINT8    -> PlanVar<int8>  (createParam()) :> IStorage
            | DuINT16   -> PlanVar<int16> (createParam()) :> IStorage
            | DuINT32   -> PlanVar<int32> (createParam()) :> IStorage
            | DuINT64   -> PlanVar<int64> (createParam()) :> IStorage
            | DuUINT8   -> PlanVar<uint8> (createParam()) :> IStorage
            | DuUINT16  -> PlanVar<uint16>(createParam()) :> IStorage
            | DuUINT32  -> PlanVar<uint32>(createParam()) :> IStorage
            | DuUINT64  -> PlanVar<uint64>(createParam()) :> IStorage
            | DuFLOAT32 -> PlanVar<single>(createParam()) :> IStorage
            | DuFLOAT64 -> PlanVar<double>(createParam()) :> IStorage
            | DuSTRING  -> PlanVar<string>(createParam()) :> IStorage
            | DuCHAR    -> PlanVar<char>  (createParam()) :> IStorage
            | DuBOOL    -> PlanVar<bool>  (createParam()) :> IStorage

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

    let createPlanVar (storages:Storages) (name:string) (dataType:DataType) (fillAutoAddress:bool) =
        let name = getPlcTagAbleName name storages
        let t= createPlanVarHelper (storages, name, dataType, fillAutoAddress)
        t

    let createPlanVarBool (storages:Storages) name (fillAutoAddress:bool) =
        createPlanVar storages name DuBOOL fillAutoAddress :?> PlanVar<bool>

    //[<Obsolete("Fix me: 임시")>]
    //let createPlanVar (storages:Storages) (name:string) (dataType:DataType)  =
    //    let name = getPlcTagAbleName name storages
    //    let t= createPlanVarHelper (storages, name, dataType, true)
    //    t

    //let createPlanVarBool (storages:Storages) name  =
    //    createPlanVar storages name DuBOOL :?> PlanVar<bool>

    type InOut = | In | Out | Memory
    type BridgeType = | Device | Button | Lamp | Condition
    let createBridgeTag(stg:Storages, name, addr:string, inOut:InOut, bridge:BridgeType, sys): ITag option=
        let address =
            let addr = addr.ToUpper()
            match bridge with
            | Device    -> if addr <> "" then Some addr else failwithlog $"Error Device {name} 주소가 없습니다."
            | Button    -> if addr <> "" then Some addr else None
            | Lamp      -> if addr <> "" then Some addr else failwithlog $"Error Lamp {name}  주소가 없습니다."
            | Condition -> if addr <> "" then Some addr else failwithlog $"Error Condition {name} 주소가 없습니다."

        if address.IsSome
        then
            let name =
                match inOut with
                | In  -> $"{name}_I"
                | Out -> $"{name}_O"
                | Memory -> failwithlog "error: Memory not supported "

            let plcName = getPlcTagAbleName name stg
            let t =
                let param = {defaultStorageCreationParams(false) with Name=plcName; Address=address; System=sys}
                (Tag(param) :> ITag)
            stg.Add(t.Name, t)
            Some t
        else None

