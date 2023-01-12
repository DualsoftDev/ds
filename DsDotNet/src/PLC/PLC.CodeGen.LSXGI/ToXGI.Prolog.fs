namespace PLC.CodeGen.LSXGI

open System.Security

[<AutoOpen>]
module XgiPrologModule =
    /// name -> comment -> plcType -> SymbolInfo
    let mutable fwdCreateSymbolInfo =
        let dummy (name:string) (comment:string) (plcType:string) : SymbolInfo =
            failwith "Should be reimplemented."
        dummy

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