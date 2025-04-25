// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open System
open Dual.Common.Core.FS
open Engine.Core
open Engine.Core.MapperDataModule

[<AutoOpen>]
module ImportType =

    

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


    let getDevName (row: Data.DataRow) =
        let flowName = row.[(int) IOColumn.Flow].ToString()
        let name = row.[(int) IOColumn.Name].ToString()

        if row.[(int) IOColumn.Case].ToString() = TextTagIOAddress
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



    let nameCheck (shape: Shape, nodeType: NodeType, iPage: int, name:string) =

        if not(nodeType.IsLoadSys) && name.Split(".").Length > 3 && nodeType <> NodeType.AUTOPRE then
                failwithlog ErrID._73

        if name.Contains(";") then
                failwithlog ErrID._18

        //REAL other flow 아니면 이름에 '.' 불가
        let checkDotErr () =
            if nodeType <> REALExF && name.Contains(".") then
                failwithlog ErrID._19


        match nodeType with
        | REAL -> checkDotErr(); 
                  if name.EndsWith(")") then failWithLog $"Error: {name} Work는 () 속성을 입력할 수 없습니다. ex) [T(500ms), C(5)] "
        | REALExF ->
            if name.Contains(".") |> not then
                failwithlog ErrID._54

            if name.EndsWith(")") || name.EndsWith("]") 
            then failWithLog $"다른 Flow Work는 속성을 입력할 수 없습니다. 해당 원본 Work에 입력하세요"

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
        | CONDITIONorAction
        | LAYOUT
        | LAMP -> checkDotErr()
