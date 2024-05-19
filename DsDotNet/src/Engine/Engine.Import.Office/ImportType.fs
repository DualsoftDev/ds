// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office

open System
open System.Linq
open Dual.Common.Core.FS
open Engine.Core
open System.Collections.Generic
open Engine.CodeGenCPU

[<AutoOpen>]
module ImportType =

    [<Flags>]
    type IOColumn =
        | Case = 0
        | Flow = 1
        | Name = 2
        | DataType = 3
        | Input = 4
        | Output = 5
        | InSymbol = 6
        | OutSymbol = 7

    [<Flags>]
    type ErrorColumn =
        | No = 0
        | Name = 1
        | ErrorAddress = 2

    [<Flags>]
    type TextColumn =
        | Name = 0
        | Empty1 = 1
        | Empty2 = 2
        | Empty3 = 3
        | Color = 4
        | Ltalic = 5
        | UnderLine = 6
        | StrikeOut = 7
        | Bold = 8

    [<Flags>]
    type ManualColumn_I =
        | Name = 0
        | DataType = 1
        | Input = 2

    [<Flags>]
    type ManualColumn_O =
        | Name = 0
        | DataType = 1
        | Output = 2   

    [<Flags>]
    type ManualColumn_M =
        | Name = 0
        | DataType = 1
        | Manual = 2

    [<Flags>]
    type ManualColumn_ControlPanel =
        | Name = 0
        | DataType = 1
        | Manual = 2

    type DevParamRawItem  = string*DataType*string //address, dataType, func

    let getDevName (row: Data.DataRow) = 
        let flowName = row.[(int) IOColumn.Flow]
        if flowName <> "" && flowName <> TextSkip
        then
            $"{flowName}_{row.[(int) IOColumn.Name]}"
        else 
            $"{row.[(int) IOColumn.Name]}"
            

    let checkPPTDataType (devParamRaw:DevParamRawItem) (devParam:DevParam) = 
        let address, typePPT = devParamRaw |>fun (addr,t,_) -> addr, t
        if (address <> TextSkip) && (typePPT <> devParam.DevType)  
        then    
            failWithLog $"error datatype : {devParamRaw} <> {devParam.DevType}"


    let getPPTDevParamInOut (inParamRaw:DevParamRawItem) (outParamRaw:DevParamRawItem) = 
        let paramFromText paramRaw =
            let addr, (dataType:DataType), func = paramRaw
            if func <> ""
            then 
                getDevParam $"{addr}:{func}" 
            else
                addr|>defaultDevParam

        let inP =  paramFromText inParamRaw
        let outP = paramFromText outParamRaw

        checkPPTDataType  inParamRaw inP
        checkPPTDataType  outParamRaw outP
        
        inP, outP


    let checkDataType name (devParam:DevParam) (dataType:DataType)= 
        
        if devParam.DevValueNType.IsSome && devParam.DevType <> dataType
            then failWithLog $"error datatype : {name}\r\n [{devParam.DevType.ToText()}]  <> {dataType.ToText()}]"


    let updatePPTDevParam (dev:TaskDev) (inSym:string option, inDataType:DataType)  (outSym:string option, outDataType:DataType)  = 
        dev.InParam <- changeDevParam dev.InParam dev.InParam.DevAddress inSym
        dev.OutParam <- changeDevParam dev.OutParam dev.OutParam.DevAddress outSym

        checkDataType dev.Name dev.InParam inDataType   
        checkDataType dev.Name dev.OutParam outDataType

    let getPPTDataTypeText (inP:DevParam) (outP:DevParam) =
        let inTypeText  = inP.DevType.ToPLCText() 
        let outTypeText = outP.DevType.ToPLCText() 
        if inTypeText = outTypeText 
        then inTypeText
        else $"{inTypeText}:{outTypeText}"

    let getPPTTDevDataTypeText (dev:TaskDev) = getPPTDataTypeText dev.InParam dev.OutParam
    let getPPTHwDevDataTypeText (hwDev:HwSystemDef) = getPPTDataTypeText hwDev.InParam hwDev.OutParam

    let updatePPTHwParam (hwDev:HwSystemDef) (inSym:string option, inDataType:DataType)  (outSym:string option, outDataType:DataType)  = 
        hwDev.InParam <- changeDevParam hwDev.InParam hwDev.InParam.DevAddress inSym
        hwDev.OutParam <- changeDevParam hwDev.OutParam hwDev.OutParam.DevAddress outSym

        checkDataType hwDev.Name hwDev.InParam inDataType   
        checkDataType hwDev.Name hwDev.OutParam outDataType
