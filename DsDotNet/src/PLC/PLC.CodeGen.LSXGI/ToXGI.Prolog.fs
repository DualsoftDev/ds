namespace PLC.CodeGen.LSXGI

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
        if address.IsNullOrEmpty() then
            Ok true
        else
            match address.ToUpper() with
            (* matches %I3, %I3.2, %I3.2.1, %IX3, %IX3.2, %IX3.2.1, ... *)
            | RegexPattern @"^%([IQMR][XBWDL]?)(\d+)$"  _
            |   RegexPattern @"^%([IQ][XBWDL]?)(\d+)\.(\d+)$"  _
            |   RegexPattern @"^%([IQ][XBWDL]?)(\d+)\.(\d+)\.(\d+)$" _ -> Ok true
            |   RegexPattern @"^%M([BWDL])(\d+)\.(\d+)$" [size; Int32Pattern n1; Int32Pattern n2; ] when
                    (size="B" && 0 <= n2 && n2 < 8 )
                    || (size="W" && 0 <= n2 && n2 < 16 )
                    || (size="D" && 0 <= n2 && n2 < 32 )
                    || (size="L" && 0 <= n2 && n2 < 64 ) ->
                  Ok true
            | _ -> Error $"Invalid address: '{address}'"

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
            (* Symbol name validataion : 규칙을 찾을 수가 없네요~~
                - OK: n, m, p, nn0, p1, mb, mw, rx, ix0, ib0
                - Fail: n0, m0, mb0, mx0, mw0, r0, rx0, N0, M0,
             *)
            result {
                let! _ = validateVariableName x.Name
                let! _ = validateAddress x.Address
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

