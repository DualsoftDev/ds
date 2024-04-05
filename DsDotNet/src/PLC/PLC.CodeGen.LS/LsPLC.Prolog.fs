namespace PLC.CodeGen.LS

open System.Security
open Engine.Core
open Dual.Common.Core.FS

[<AutoOpen>]
module XgiPrologModule =
    /// XML 특수 문자 escape.  '&' 등
    let escapeXml xml = SecurityElement.Escape xml

    let validateVariableName (name:string) =
        match name.ToUpper() with
        | RegexPattern @"^([NMR][XBWDL]?)(\d+)$" [_reserved; _num] ->
            Error $"'{name}' is not valid symbol name.  (Can't use direct variable name)"
        | RegexPattern @"([\s]+)" [_ws] ->
            Error $"'{name}' contains white space char"
        | _ ->
            Ok true

    let validateAddress (address:string) = 
        if address.IsXGIAddress() then
            Ok true
        else
            Error $"Invalid address: '{address}'"

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
    } with
        member x.Validate() =
            result {
                let! _ = validateVariableName x.Name
                let! _ = if  x.Address.IsNullOrEmpty() && x.Device = ""  //빈주소 자동 변수로 허용
                         then Ok true 
                         else  validateAddress x.Address
                return! Ok()
            }

    let defaultSymbolInfo = {
        Name       = ""
        Type       = ""
        InitValue  = null
        Comment    = ""
        Device     = ""
        Address    = ""
        DevicePos  = -1
        Kind       = int Variable.Kind.VAR
        State      = 0
        AddressIEC = ""
    }


    /// name -> comment -> plcType -> kind -> SymbolInfo
    let mutable fwdCreateSymbolInfo =
        let dummy (_name:string) (_comment:string) (_plcType:string) (_kind:int) (_initValue:BoxedObjectHolder) : SymbolInfo =
            failwithlog "Should be reimplemented."
        dummy

