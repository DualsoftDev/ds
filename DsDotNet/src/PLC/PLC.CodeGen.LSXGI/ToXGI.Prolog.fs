namespace PLC.CodeGen.LSXGI

open System.Security
open Engine.Core
open Engine.Common.FS

[<AutoOpen>]
module XgiPrologModule =
    /// XML 특수 문자 escape.  '&' 등
    let escapeXml xml = SecurityElement.Escape xml

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


    /// name -> comment -> plcType -> kind -> SymbolInfo
    let mutable fwdCreateSymbolInfo =
        let dummy (_name:string) (_comment:string) (_plcType:string) (_kind:int) (_initValue:BoxedObjectHolder) : SymbolInfo =
            failwithlog "Should be reimplemented."
        dummy

