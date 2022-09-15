// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office



[<AutoOpen>]
module ImportType =

    ///인과의 노드 종류
    type BtnType =
        | StartBTN            //시작 버튼
        | ResetBTN            //리셋 버튼
        | AutoBTN             //자동 버튼
        | EmergencyBTN         //비상 버튼
       

    let BtnToType(txt:string) =
            match txt with
            | "비상" -> StartBTN
            | "자동" -> ResetBTN
            | "시작" -> AutoBTN
            | "리셋" -> EmergencyBTN
            |_-> failwithf "BtnToType Error"
    