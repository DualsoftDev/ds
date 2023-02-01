namespace PLC.CodeGen.LSXGI

open System.Security
open Engine.Core
open Engine.Common.FS

[<AutoOpen>]
module XgiPrologModule =
    /// XML 특수 문자 escape.  '&' 등
    let escapeXml xml = SecurityElement.Escape xml

    //let validateVariableName name =

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
                match x.Name.ToUpper() with
                | RegexPattern "^([NMR][XBWDL]?)(\d+)$" [reserved; _num] ->
                    return! Error $"'{x.Name}' is not valid symbol name.  (Can't use direct variable name)"
                | RegexPattern "([\s]+)" [ws] ->
                    return! Error $"'{x.Name}' contains white space char"
                | _ ->
                    ()

                match x.Address with
                | IsItNullOrEmpty _ -> ()
                (* matches %I3, %I3.2, %I3.2.1, %IX3, %IX3.2, %IX3.2.1, ... *)
                | RegexPattern "^%([IQMR][XBWDL]?)(\d+)([.\d+]{0, 2})*$"  _ -> ()      // IQMLKFWUR
                | _ -> return! Error $"Invalid address: '{x.Address}'"

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

