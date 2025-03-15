namespace Dsu.PLCConverter.FS

open ActivePattern
open Microsoft.FSharp.Collections
open Dsu.PLCConverter.FS.XgiSpecs
open System.Diagnostics
open System

[<AutoOpen>]
/// XML에서 사용되는 심볼 태그의 속성 및 관련 함수들을 포함하는 모듈
module XgiSymbolIndexArray =

    let arrDefaultSize = 32766

    let createIndexArraySymbol addressString indexAddress  =
        let parse = XGI.Parsing addressString
        let addressAuto, varType, bitOffset = XGI.MakeXgiAddressWithOffset(parse)
        let symbolName = 
            if bitOffset%8 = 0
            then
                $"{addressAuto.TrimStart('%')}ARRAY{indexAddress}"
            else 
                $"{addressAuto.TrimStart('%')}ARRAY{indexAddress}_NOT_ARRAY_8비트단위시작필요"

        let arrayInfo = ArrayInfo(arrDefaultSize, varType)|>Some
        createSymbolInfo parse addressAuto "" VarType.ARRAY symbolName (int Variable.Kind.VAR) false arrayInfo

    let makeIndexArraySymbol (addressSrc: string) =
        match addressSrc with
        | ActivePattern.RegexPattern @"(\S+)(Z\d+)" [address; indexAddress ] ->
            let symbol =  createIndexArraySymbol address indexAddress
            symbol.GxAddress <- addressSrc
            symbol
        | _ -> failwith (sprintf "Invalid address format: [%s]" addressSrc)
