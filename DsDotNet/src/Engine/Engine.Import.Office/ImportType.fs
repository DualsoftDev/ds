// Copyright (c) Dualsoft  All Rights Reserved.
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
    type ManualColumn =
        | Name = 0
        | DataType = 1
        | Address = 2

    [<Flags>]
    type AutoColumn =
        | Name = 0
        | DataType = 1
        | Address = 2

    [<Flags>]
    type ManualColumn_ControlPanel =
        | Name = 0
        | DataType = 1
        | Manual = 2


    type MasterPageMacro = 
        {
            Macro : string
            MacroRelace : string
            Page : int
        }

    type TaskDevParaRawItem  = string*DataType*string //address, dataType, func

    let getDevName (row: Data.DataRow) = 
        let flowName = row.[(int) IOColumn.Flow].ToString()
        let name = row.[(int) IOColumn.Name].ToString()
        
        if row.[(int) IOColumn.Case].ToString() = TextXlsAddress
        then

            if name.Split(".").Length <> 2 then
                failwithlog ErrID._75
            else 
                $"{flowName}{TextDeviceSplit}{name}"
        else 
            name

    let getMultiDeviceName (loadedName:string) index = 
        //index 2자리로 표현
        let indexStr = index.ToString().PadLeft(2, '0')
        $"{loadedName}_{indexStr}"
                    

    let checkPPTDataType (taskDevParamRaw:TaskDevParaRawItem) (taskDevParam:TaskDevPara) = 
        let address, typePPT = taskDevParamRaw |>fun (addr,t,_) -> addr, t
        if (address <> TextSkip) && (typePPT <> taskDevParam.Type)  
        then    
            failWithLog $"error datatype : {taskDevParamRaw} <> {taskDevParam.Type}"


    let getPPTTaskDevParaInOut (inParamRaw:TaskDevParaRawItem) (outParamRaw:TaskDevParaRawItem) = 
        let paramFromText paramRaw =
            let addr, (dataType:DataType), func = paramRaw
            if func <> ""
            then 
                getTaskDevPara $"{addr}:{func}" |> snd
            else
                defaultTaskDevPara()

        let inP =  paramFromText inParamRaw
        let outP = paramFromText outParamRaw

        checkPPTDataType  inParamRaw inP
        checkPPTDataType  outParamRaw outP
        
        inP, outP


    let checkDataType name (taskDevParamDataType:DataType) (dataType:DataType)= 
          if taskDevParamDataType <> dataType
                then failWithLog $"error datatype : {name}\r\n [{taskDevParamDataType.ToPLCText()}]  <> {dataType.ToPLCText()}]"


    let updatePPTTaskDevPara (dev:TaskDev) (inSym:string option, inDataType:DataType)  (outSym:string option, outDataType:DataType)  = 

        dev.SetInSymbol(inSym) 
        dev.SetOutSymbol(outSym)

        checkDataType $"IN {dev.QualifiedName}" dev.InDataType inDataType   
        checkDataType $"OUT {dev.QualifiedName}" dev.OutDataType outDataType

    let getPPTDataTypeText (inType:DataType) (outType:DataType) =
        let inTypeText  = inType.ToPLCText() 
        let outTypeText = outType.ToPLCText() 
        if inTypeText = outTypeText 
        then inTypeText
        else $"{inTypeText}:{outTypeText}"

    let getPPTTDevDataTypeText (dev:TaskDev) =   getPPTDataTypeText dev.InDataType dev.OutDataType
    let getPPTHwDevDataTypeText (hwDev:HwSystemDef) = getPPTDataTypeText hwDev.InDataType hwDev.OutDataType

    let updatePPTHwParam (hwDev:HwSystemDef) (inSym:string option, inDataType:DataType)  (outSym:string option, outDataType:DataType)  = 

        let inParam = changeSymbolTaskDevPara (hwDev.TaskDevParamIO.InParam) inSym  
        let outParam = changeSymbolTaskDevPara (hwDev.TaskDevParamIO.OutParam) inSym 

        hwDev.TaskDevParamIO <-  TaskDevParamIO(Some inParam, Some outParam)

        checkDataType  $"IN {hwDev.QualifiedName}" hwDev.InDataType inDataType   
        checkDataType  $"OUT {hwDev.QualifiedName}" hwDev.OutDataType outDataType
        
            
    let nameCheck (shape: Shape, nodeType: NodeType, iPage: int, name:string) =
        
        if not(nodeType.IsLoadSys) && name.Split(".").Length > 3 then
                failwithlog ErrID._73

        if name.Contains(";") then
                failwithlog ErrID._18

        //REAL other flow 아니면 이름에 '.' 불가
        let checkDotErr () =
            if nodeType <> REALExF && name.Contains(".") then
                failwithlog ErrID._19
        

        match nodeType with
        | REAL -> checkDotErr();
        | REALExF ->
            if name.Contains(".") |> not then
                failwithlog ErrID._54
        | CALL | AUTOPRE ->
          // ok :  dev.api(10,403)[XX]  err : dev(10,403)[XX] 순수CMD 호출은 속성입력 금지
            if not(name.Contains(".")) &&  GetSquareBrackets(name, false).IsSome
            then
                failwithlog ErrID._70

        | OPEN_EXSYS_CALL
        | OPEN_EXSYS_LINK
        | COPY_DEV ->
            let name, number = GetTailNumber(shape.InnerText)

            if GetSquareBrackets(name, false).IsNone then
                failwithlog ErrID._7
            try
                GetBracketsRemoveName(name) + ".pptx" |> PathManager.getValidFile |> ignore
            with ex ->
                shape.ErrorName(ex.Message, iPage)

        | IF_DEVICE
        | DUMMY
        | BUTTON
        | CONDITION
        | LAYOUT
        | LAMP -> checkDotErr()
