namespace PLC.CodeGen.LSXGI

open System.Security
open Engine.Core

[<AutoOpen>]
module XgiPrologModule =
    /// XML 특수 문자 escape.  '&' 등
    let escapeXml xml = SecurityElement.Escape xml

    type XgiGenerationOptions = {
        /// <!-- --> 구문의 xml comment 삽입 여부.  순수 xml 생성 과정 debugging 용도
        EnableXmlComment:bool
        IsAppendExpressionTextToRungComment:bool
    }

    let mutable xgiGenerationOptions = {
        EnableXmlComment = false
        IsAppendExpressionTextToRungComment = true
    }

    /// Xml Symbol tag 가 가지는 속성
    type SymbolInfo = {
        Name:string
        /// "BOOL"
        Type:string
        InitValue:obj
        Comment:string
        /// "M"
        Device:string
        /// "%MX1"
        Address:string
        DevicePos:int //XGK 일경우 DevicePos 정보 필요
        Kind:int
        State:int
        AddressIEC : string //XGK 일경우 IEC 주소로 변환해서 가지고 있음
    }


    /// name -> comment -> plcType -> SymbolInfo
    let mutable fwdCreateSymbolInfo =
        let dummy (name:string) (comment:string) (plcType:string) (initValue:BoxedObjectHolder) : SymbolInfo =
            failwith "Should be reimplemented."
        dummy

