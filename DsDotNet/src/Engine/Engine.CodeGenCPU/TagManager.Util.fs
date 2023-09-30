namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System.Reactive.Linq
open System.Text.RegularExpressions
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
                        || c.IsValidIdentifier() 
                        || ['['; ']'].ToResizeArray().Contains(c) -> c.ToString()
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
        let address = if fillAutoAddress then Some "" else None
        let createParam () = {defaultStorageCreationParams(unbox v) with Name=name; IsGlobal=true; Address=address; Target= Some target; TagKind = tagIndex;System= system}
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


    let timer  (storages:Storages)  name sys pres=
        let name = getPlcTagAbleName name storages
        let ts = TimerStruct.Create(TimerType.TON, storages, name, pres, 0us, sys)
        ts

    let counter (storages:Storages) name sys =
        let name = getPlcTagAbleName name storages
        let cs = CTRStruct.Create(CounterType.CTR, storages, name, 0us, 0us, sys)
        cs

    let createPlanVar (storages:Storages) (name:string) (dataType:DataType) (fillAutoAddress:bool) (target:IQualifiedNamed) (tagIndex:int) (sys:ISystem) =
        let name = getPlcTagAbleName name storages
        let t= createPlanVarHelper (storages, name, dataType, fillAutoAddress, target, tagIndex, sys)
        t


    type BridgeType = | Device | Button | Lamp | Condition
    let mutable inCnt = -1;
    let mutable outCnt = -1;
    let mutable memCnt = -1;
    let resetSimDevCnt() = inCnt<- 10000;outCnt<- 10000;memCnt<- 10000;
    let createBridgeTag(stg:Storages, name, addr:string, inOut:ActionTag, bridge:BridgeType, sys, task:IQualifiedNamed option): ITag option=
        let address =           //todo ahn 자동 주소 경고 띄우기 
            if addr <> ""  
            then
                let addr = addr.ToUpper()
                match bridge with
                | Device    -> if addr <> "" then Some addr else failwithlog $"Error Device {name} 주소가 없습니다."
                | Button    -> if addr <> "" then Some addr else None
                | Lamp      -> if addr <> "" then Some addr else failwithlog $"Error Lamp {name}  주소가 없습니다."
                | Condition -> if addr <> "" then Some addr else failwithlog $"Error Condition {name} 주소가 없습니다."

            else
                match inOut with
                | ActionTag.ActionIn  -> inCnt<-inCnt+1
                                         if RuntimeDS.Package.IsPackagePLC()
                                         then
                                             Some($"%%IW{inCnt/16}.{inCnt%16}") //일단 LS 규격으로
                                         else 
                                             Some($"I{inCnt/16}.{inCnt%16}") 

                | ActionTag.ActionOut -> outCnt<-outCnt+1
                                         if RuntimeDS.Package.IsPackagePLC()
                                         then
                                             Some($"%%QW{outCnt/16}.{outCnt%16}") //일단 LS 규격으로
                                         else 
                                             Some($"O{outCnt/16}.{outCnt%16}")

                | ActionTag.ActionMemory -> failwithlog "error: Memory not supported "
                | _ -> failwithlog "error: ActionTag create "
       

        if address.IsSome
        then
            let name =
                match inOut with
                | ActionTag.ActionIn    -> $"{name}_I"
                | ActionTag.ActionOut   -> $"{name}_O"
                | ActionTag.ActionMemory   -> failwithlog "error: Memory not supported "
                | _ -> failwithlog "error: ActionTag create "

            let plcName = getPlcTagAbleName name stg
            let t =
                let param = {defaultStorageCreationParams(false) with Name=plcName; Address=address; System=sys; TagKind = (int)inOut; Target = task}
                (Tag(param) :> ITag)
            stg.Add(t.Name, t)
            Some t
        else None

