namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System.Reactive.Linq
open System
open Dual.Common.Core.FS.ForwardDecl.ShowForwardDeclSample

[<AutoOpen>]
module TagManagerUtil =


    let getPlcTagAbleName (name:string) (storages:Storages) =
        let getValidName (name:string) =
            name 
            |> Seq.map (fun c ->
                match c with
                | _ when Char.IsNumber(c) 
                        || c.IsValidIdentifier() -> c.ToString()
                        //|| ['['; ']'].ToResizeArray().Contains(c)   //arrayType 오인
                | _ -> "_")
            |> String.concat ""

        let rec generateUntilValid(inputName:string) =
            if storages.ContainsKey inputName then
                generateUntilValid(UniqueName.generate inputName)
            else
                inputName

        name |> getValidName |> generateUntilValid


    /// fillAutoAddress : PLC 에 내릴 때, 자동으로 주소를 할당할 지의 여부
    let private createPlanVarHelper(stg:Storages, name:string, dataType:DataType, fillAutoAddress:bool, target:IQualifiedNamed, tagIndex:int,  system:ISystem) : IStorage =
        let v = dataType.DefaultValue()
        let address = if fillAutoAddress then Some TextAddrEmpty else None
        let createParam () = {defaultStorageCreationParams(unbox v) tagIndex with Name=name; IsGlobal=true; Address=address; Target= Some target; TagKind = tagIndex;System= system}
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


    let timer  (storages:Storages)  name sys target =
        let name = getPlcTagAbleName name storages
        let ts = TimerStruct.Create(TimerType.TON, storages, name, 0u, 0u, sys, target)
        ts

    let counter (storages:Storages) name sys target=
        let name = getPlcTagAbleName name storages
        let cs = CTRStruct.Create(CounterType.CTR, storages, name, 0u, 0u, sys, target)
        cs

    let createPlanVar (storages:Storages) (name:string) (dataType:DataType) (fillAutoAddress:bool) (target:IQualifiedNamed) (tagIndex:int) (sys:ISystem) =

        let name = getPlcTagAbleName name storages
        let t= createPlanVarHelper (storages, name, dataType, fillAutoAddress, target, tagIndex, sys)
        t

    let createSystemPlanVar (storages:Storages) (name:string) (dataType:DataType) (fillAutoAddress:bool) (target:IQualifiedNamed) (tagIndex:int) (sys:ISystem) =
        let t= createPlanVarHelper (storages, name, dataType, fillAutoAddress, target, tagIndex, sys)
        t

    type BridgeType = | Device | Button | Lamp | Condition | DummyTemp
    let createBridgeTag(stg:Storages, name, address:string, tagKind:int, bridgeType:BridgeType, sys, fqdn:IQualifiedNamed, duType:DataType): ITag option=
        if address = TextSkip || address = "" 
        then None
        else
            let name =
                match bridgeType with
                | DummyTemp -> name  //plc b접 처리를 위한 임시 물리주소 변수
                | Device ->   match DU.tryGetEnumValue<ActionTag>(tagKind).Value with
                              | ActionTag.ActionIn     -> $"{name}_I"
                              | ActionTag.ActionOut    -> $"{name}_O"
                              | ActionTag.ActionMemory -> failwithlog "error: Memory not supported "
                              | _ -> failwithlog "error: ActionTag create "

                | Button | Lamp | Condition
                    ->   match DU.tryGetEnumValue<HwSysTag>(tagKind).Value with
                              | HwSysTag.HwSysIn      -> $"{name}_IN"
                              | HwSysTag.HwSysOut     -> $"{name}_OUT"
                              | _ -> failwithlog "error: HwSysTag create "
       
            let plcAddrName = getPlcTagAbleName name stg
            let t = createTagByBoxedValue plcAddrName {Object = duType.DefaultValue()} tagKind address sys fqdn
            stg.Add(t.Name, t)
            Some t
            
