namespace Dsu.PLCConverter.FS

open ActivePattern
open Microsoft.FSharp.Collections
open Dsu.PLCConverter.FS.XgiSpecs
open System.Diagnostics
open System
open System.Collections.Generic

[<AutoOpen>]
/// XML에서 사용되는 심볼 태그의 속성 및 관련 함수들을 포함하는 모듈
module XgiSymbolBatch =

    let determineVarType (nibbleSize:int) =
        match nibbleSize with
        | 1 | 2 -> VarType.BYTE
        | 3 | 4 -> VarType.WORD
        | 5 | 6 | 7 | 8 -> VarType.DWORD
        | 9 | 10 | 11 | 12 | 13 | 14 | 15 | 16 -> VarType.LWORD
        | _ -> failwithf $"Invalid nibbleSize: {nibbleSize}. Max supported value is 16."

    let calculateAddress (addressAuto:string) varType bitOffset isIQ =
        let headPrefix = addressAuto.Substring(0, 2)
        if isIQ  
        then 
            $"{headPrefix}X{bitOffset}" 
        else
            match varType with
            | VarType.BYTE   ->  $"{headPrefix}B{bitOffset/8}" 
            | VarType.WORD   ->  $"{headPrefix}W{bitOffset/16}" 
            | VarType.DWORD  ->  $"{headPrefix}D{bitOffset/32}" 
            | VarType.LWORD  ->  $"{headPrefix}L{bitOffset/64}" 
            | _ -> addressAuto

    let createNibbleFitSymbol addressString (nibbleSize:int) comment=
        let parse = XGI.Parsing addressString
        let addressAuto, _, bitOffset = XGI.MakeXgiAddressWithOffset(parse)

        let varType = determineVarType nibbleSize
        let isIQ = addressAuto.StartsWith("%I") || addressAuto.StartsWith("%Q")
        let address = calculateAddress addressAuto varType bitOffset isIQ
        let symbolName = $"K{nibbleSize}{address.TrimStart('%')}_{bitOffset}"

        createSymbolInfo parse address comment varType symbolName (int Variable.Kind.VAR) true None

        
    let makeBatchSymbol (address: string) =
        match address with
        | ActivePattern.RegexPattern NibbleText [k; nibbleSize; addr] ->
            let symbol =  createNibbleFitSymbol addr (Convert.ToInt32(nibbleSize)) ""
            symbol.GxAddress <- address
            symbol, createNibbleTempLWord symbol 
        | _ -> failwith (sprintf "Invalid address format: [%s]" address)
