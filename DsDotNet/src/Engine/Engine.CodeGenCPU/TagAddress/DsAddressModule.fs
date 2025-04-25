namespace Engine.CodeGenCPU

open Dual.Common.Core.FS
open PLC.CodeGen.Common
open Dual.PLC.Common.FS
open XgtProtocol
open Engine.CodeGenCPU.DsAddressUtil
open Engine.Core
open System.Linq
open Engine.Core.MapperDataModule

[<AutoOpen>]
module DsAddressModule =


    let private getValidHwItem (hwItem:HwSystemDef) (skipIn:bool) (skipOut:bool) (target:HwTarget)=
        let inAddr  = getValidAddress(hwItem.InAddress,  hwItem.InDataType,  hwItem.Name, skipIn,  IOType.Memory, target)
        let outAddr = getValidAddress(hwItem.OutAddress, hwItem.OutDataType, hwItem.Name, skipOut, IOType.Memory, target)
        inAddr, outAddr
            
    let updateHwAddress (hwItem: HwSystemDef) (inAddr, outAddr) (target:HwTarget)   =
        hwItem.InAddress <- inAddr
        hwItem.OutAddress <- outAddr

        let inA, outA =
            match hwItem with
            | :? ConditionDef as c -> getValidHwItem c  false true  target
            | :? ActionDef as a    -> getValidHwItem a  true false  target
            | :? ButtonDef as b    -> getValidHwItem b  false false target
            | :? LampDef as l      -> getValidHwItem l  true  false target
            | _ -> failWithLog $"Error {hwItem.Name} not support"

        hwItem.InAddress <- inA
        hwItem.OutAddress <- outA

    let assignAutoAddress (sys: DsSystem, startMemory:int, offsetOpModeLampBtn:int, target:HwTarget) =
        setMemoryIndex(startMemory);

        for b in sys.HWButtons do
            let inA = if b.InAddress = "" then TextAddrEmpty else b.InAddress
            let outA = TextNotUsed
            updateHwAddress b (inA, outA)  target

        for l in sys.HWLamps do
            let inA = TextNotUsed
            let outA = if l.OutAddress = "" then TextAddrEmpty else l.OutAddress
            updateHwAddress l (inA, outA)  target

        for c in sys.HWConditions do
            let inA = if c.InAddress = "" then TextAddrEmpty else c.InAddress
            let outA = TextNotUsed
            updateHwAddress c (inA, outA)  target

        for ce in sys.HWActions do
            let inA = TextNotUsed
            let outA = if ce.OutAddress = "" then TextAddrEmpty else ce.OutAddress
            updateHwAddress ce (inA, outA)  target

        let devsJob =  sys.GetTaskDevsWithoutSkipAddress()
        let mutable extCnt = 0
        for g in devsJob.GroupBy(fun (_dev, job) -> job) do
            g |> Seq.iteri(fun i (dev, job) ->

                let inSkip, outSkip = getSkipInfo(i, job)

                dev.InAddress  <- getValidAddress(dev.InAddress,  dev.InDataType,  dev.QualifiedName, inSkip,  IOType.In, target)
                dev.OutAddress <- getValidAddress(dev.OutAddress, dev.OutDataType, dev.QualifiedName, outSkip, IOType.Out, target)

                if dev.IsRootOnlyDevice
                then
                    if dev.InAddress = TextAddrEmpty && not(inSkip) then
                        dev.InAddress  <-  getExternalTempMemory(target, extCnt)
                        extCnt <- extCnt+1

                    dev.OutAddress <- TextNotUsed
                    )

        setMemoryIndex(startMemory + offsetOpModeLampBtn);


    let checkDataType name (taskDevParamDataType:DataType) (dataType:DataType)=
          if taskDevParamDataType <> dataType
                then failWithLog $"error datatype : {name}\r\n [{taskDevParamDataType.ToPLCText()}]  <> {dataType.ToPLCText()}]"

    let updatePptTaskDevParam (dev:TaskDev) (inSym:string option, inDataType:DataType)  (outSym:string option, outDataType:DataType)  =
        if inSym.IsSome then  dev.SetInSymbol(inSym.Value)
        if outSym.IsSome then  dev.SetOutSymbol(outSym.Value)

        checkDataType $"IN {dev.QualifiedName}" dev.InDataType inDataType
        checkDataType $"OUT {dev.QualifiedName}" dev.OutDataType outDataType

    let getPptDevDataTypeText (dev:TaskDev) =   DsTaskDevTypeModule.getTaskDevDataTypeText dev.TaskDevParamIO
    let getPptHwDevDataTypeText (hwDev:HwSystemDef) = DsTaskDevTypeModule.getTaskDevDataTypeText hwDev.TaskDevParamIO

    let updatePptHwParam (hwDev:HwSystemDef) (inSym:string option, inDataType:DataType)  (outSym:string option, outDataType:DataType)  =
        if inSym.IsSome 
        then hwDev.TaskDevParamIO.InParam.Symbol <- inSym.Value
        if outSym.IsSome 
        then hwDev.TaskDevParamIO.InParam.Symbol <- outSym.Value

        checkDataType  $"IN {hwDev.QualifiedName}" hwDev.InDataType inDataType
        checkDataType  $"OUT {hwDev.QualifiedName}" hwDev.OutDataType outDataType
