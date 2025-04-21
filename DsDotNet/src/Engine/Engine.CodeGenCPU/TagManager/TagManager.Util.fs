namespace Engine.CodeGenCPU

open Dual.Common.Core.FS
open Engine.Core
open Engine.Common

[<AutoOpen>]
module TagManagerUtil =

    let addressExist address =
        address  <> TextNotUsed && address <> TextAddrEmpty



    /// fillAutoAddress : PLC 에 내릴 때, 자동으로 주소를 할당할 지의 여부
    let private createPlanVarHelper(stg:Storages, name:string, dataType:DataType, fillAutoAddress:bool, target:IQualifiedNamed option, tagIndex:int,  system:ISystem) : IStorage =
        let v = dataType.DefaultValue()
        let address = if fillAutoAddress then Some TextAddrEmpty else None
        let createParam () =
            {   defaultStorageCreationParams(unbox v) tagIndex with
                    Name=name; IsGlobal=true; Address=address; Target=  target; TagKind = tagIndex; System = Some system}
        let t:IStorage =
            match dataType with
            | DuINT8    -> PlanVar<int8>  (createParam())
            | DuINT16   -> PlanVar<int16> (createParam())
            | DuINT32   -> PlanVar<int32> (createParam())
            | DuINT64   -> PlanVar<int64> (createParam())
            | DuUINT8   -> PlanVar<uint8> (createParam())
            | DuUINT16  -> PlanVar<uint16>(createParam())
            | DuUINT32  -> PlanVar<uint32>(createParam())
            | DuUINT64  -> PlanVar<uint64>(createParam())
            | DuFLOAT32 -> PlanVar<single>(createParam())
            | DuFLOAT64 -> PlanVar<double>(createParam())
            | DuSTRING  -> PlanVar<string>(createParam())
            | DuCHAR    -> PlanVar<char>  (createParam())
            | DuBOOL    -> PlanVar<bool>  (createParam())

        stg.Add(t.Name, t)
        t


    let timer  (storages:Storages)  name sys (target:HwCPU) =
        let name = getPlcTagAbleName name storages
        let ts = TimerStruct.Create(TimerType.TON, storages, name, 0u, 0u, sys, target)
        ts

    let counter (storages:Storages) name sys (target:HwCPU)=
        let name = getPlcTagAbleName name storages
        let cs = CTRStruct.Create(CounterType.CTR, storages, name, 0u, 0u, sys, target)
        cs

    let createPlanVar (storages:Storages) (name:string) (dataType:DataType) (fillAutoAddress:bool) (target:IQualifiedNamed option) (tagIndex:int) (sys:ISystem) =
        let name = getPlcTagAbleName name storages
        let t= createPlanVarHelper (storages, name, dataType, fillAutoAddress, target, tagIndex, sys)
        t

    let createSystemPlanVar (storages:Storages) (name:string) (dataType:DataType) (fillAutoAddress:bool) (target:IQualifiedNamed) (tagIndex:int) (sys:ISystem) =
        let name = getPlcTagAbleName name storages
        let t= createPlanVarHelper (storages, name, dataType, fillAutoAddress, Some target, tagIndex, sys)
        t

    type BridgeType = | TaskDevice | Button | Lamp | Condition | Action | DummyTemp
    let createBridgeTag(stg:Storages, (name: string), address:string, tagKind:int, bridgeType:BridgeType, sys, fqdn:IQualifiedNamed, duType:DataType): ITag option=
        if address = TextNotUsed || address = "" then
            None
        else
            let validTagName =
                let nameCandidate =
                    match bridgeType with
                    | DummyTemp -> name  //plc b접 처리를 위한 임시 물리주소 변수
                    | TaskDevice ->
                        match DU.tryGetEnumValue<TaskDevTag>(tagKind).Value with
                        | TaskDevTag.actionIn     -> name |> getInActionName
                        | TaskDevTag.actionOut    -> name |> getOutActionName
                        | _ -> failwithlog "error: TaskDevTag create "

                    | (Button | Lamp | Condition | Action) ->
                        match DU.tryGetEnumValue<HwSysTag>(tagKind).Value with
                        | HwSysTag.HwSysIn      -> name |> getInActionName
                        | HwSysTag.HwSysOut     -> name |> getOutActionName
                        | _ -> failwithlog "error: HwSysTag create "

                getPlcTagAbleName nameCandidate stg

            if stg.ContainsKey validTagName then
                stg[validTagName] :?> ITag  |> Some
            else
                let t = createTagByBoxedValue validTagName {Object = duType.DefaultValue()} tagKind address sys fqdn
                stg.Add(t.Name, t)
                Some t
