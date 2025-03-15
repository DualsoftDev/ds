namespace Dsu.PLCConverter.FS

open ActivePattern
open Microsoft.FSharp.Collections
open Dsu.PLCConverter.FS.XgiSpecs
open System.Diagnostics
open System
open System.Collections.Generic

[<AutoOpen>]
/// XML에서 사용되는 심볼 태그의 속성 및 관련 함수들을 포함하는 모듈
module XgiSymbolManager =
    let DicMelsecToXgiSym = new Dictionary<string,(string*string)>()  // melsec, (xgiAddress,xgiName)
    let DicXgiSym = new Dictionary<string, string>()  // melsec, (xgiAddress,xgiName)
    
    let getNameField (item:string) usingDrirect =
        // `usingDrirect`가 true이고 토큰의 첫 글자가 `AutoType`에 없는 경우
        if usingDrirect 
        then
            if MelSecAutoType.Contains(item.Substring(0,1)) then
                Tuple.tuple2nd DicMelsecToXgiSym.[item]
            else
                // `f`에 대한 딕셔너리 `dicSym`에서 주소를 추출
                let address = Tuple.tuple1st DicMelsecToXgiSym.[item]
                // 주소가 비어 있는 경우 `Name` 필드를, 그렇지 않으면 `Address` 필드를 사용
                if address = "" then Tuple.tuple2nd DicMelsecToXgiSym.[item] else address
        else
            if MelSecSysType.Contains(item.Substring(0,2)) then
                Tuple.tuple1st DicMelsecToXgiSym.[item]
            else 
                Tuple.tuple2nd DicMelsecToXgiSym.[item]

    let getXGIArgs (arg:string) usingDrirect =
        arg.SplitBy(';') // 세미콜론으로 문자열을 분할하여 각각을 개별 토큰으로 만듦
        |> Seq.map (fun f -> 
            // `dicSym`에 토큰이 존재하는지 확인
            if DicMelsecToXgiSym.ContainsKey(f) then 
                match f with
                | ActivePattern.RegexPattern ZIndexText [baseAddr;indexDev] 
                    -> 
                        let name, address = DicMelsecToXgiSym[f]
                        let addressAuto, varType, bitOffset = XGI.MakeXgiAddressWithOffset(XGI.Parsing baseAddr)
                        if bitOffset%8 = 0 
                        then 
                            $"{name}[{indexDev}]"
                        else 
                            $"{name}[{indexDev}]_Err8비트단위필요"
                            
                | _ -> getNameField f usingDrirect
            else 
                // `dicSym`에 `f`가 없을 경우 `f` 자체를 반환
                f
        )
        |> Seq.toList 

    let getArgAddress (arg:string) =
        if DicXgiSym.ContainsKey(arg) then 
            DicXgiSym.[arg]
        else 
            arg // name ket에 없으면 arg 자신이 Address