namespace PLC.CodeGen.LSXGI

open System.Linq
open System.Collections.Generic
open System.Security
open Engine.Common.FS
open Engine.Core
open PLC.CodeGen.Common
open PLC.CodeGen.LSXGI


[<AutoOpen>]
module internal XgiSymbolsModule =
    type XgiSymbol =
        | DuXgiVar  of IXgiVar
        | DuTimer   of TimerStruct
        | DuCounter of CounterBaseStruct
        | DuStorage of IStorage


    let storagesToXgiSymbol(storages:IStorage seq) : (IStorage*XgiSymbol) list =
        let timerOrCountersNames =
            storages.Filter(fun s -> s :? TimerCounterBaseStruct)
                .Select(fun struc -> struc.Name)
                |> HashSet
                ;

        [
            for s in storages do
                match s with
                | :? IXgiVar as xgi ->
                    Some (s, XgiSymbol.DuXgiVar xgi)
                | :? TimerStruct as ts ->
                    Some (s, XgiSymbol.DuTimer ts)
                | :? CounterBaseStruct as cs ->
                    Some (s, XgiSymbol.DuCounter cs)
                | _ ->
                    let name = (s :> INamed).Name
                    if timerOrCountersNames.Contains(name.Split(".")[0]) then
                        // skip timer/counter structure member : timer 나 counter 명 + "." + field name
                        None
                    else
                        Some (s, XgiSymbol.DuStorage s)
        ] |> List.choose id

    let xgiSymbolToSymbolInfo (prjParams:XgiProjectParams) (kindVar:int) (xgiSymbol:XgiSymbol) : SymbolInfo =
        match xgiSymbol with
        | DuStorage (:? ITag as t) ->
            let name, addr = t.Name, t.Address

            let device, memSize =
                match addr with
                | RegexPattern @"%([IQM])([XBWL]).*$" [iqm; mem] -> iqm, mem
                | RegexPattern @"%([IQM]).*$" [iqm; ] -> iqm, "X"   // `%I1` 이런거 허용하나?
                | _ -> failwith $"Invalid tag address {addr} for {name}"

            let plcType =
                match memSize with
                | "X" -> "BOOL"
                | "B" -> "BYTE"
                | "W" -> "WORD"
                | "L" -> "DWORD"
                | _ -> failwithlog "ERROR"
            let comment = "FAKECOMMENT"

            let initValue = null // PLCTag 는 값을 초기화 할 수 없다.
            { defaultSymbolCreateParam with Name=name; Comment=comment; PLCType=plcType; Address=addr; InitValue=initValue; Device=device; Kind=kindVar; }
            |> XGITag.createSymbolInfoWithDetail

        // address 가 지정되지 않은 tag : e.g Timer, Counter 의 내부 멤버 변수들 EN, DN, CU, CD, ...
        | DuStorage t ->
            let symbolInfo =
                let plcType = systemTypeToXgiTypeName t.DataType
                let comment = SecurityElement.Escape t.Comment
                if t.Address = "" then
                    let {BitAllocator = bitAllocator} = prjParams.MemoryAllocator
                    if t.Name.StartsWith("_") then
                        logWarn $"Something fish: trying to generate auto M address for {t.Name}"
                    t.Address <- bitAllocator()
                { defaultSymbolCreateParam with Name=t.Name; Comment=comment; PLCType=plcType; Address=t.Address; InitValue=t.BoxedValue; Kind=kindVar; }
                |> XGITag.createSymbolInfoWithDetail

            symbolInfo

        | DuXgiVar xgi ->
            if kindVar = int Variable.Kind.VAR_GLOBAL then
                // Global 변수도 일단, XgiLocalVar type 으로 생성되므로, PLC 생성 시에만 global 로 override 해서 생성한다.
                { xgi.SymbolInfo with Kind = kindVar; Address=xgi.Address }
            else
                xgi.SymbolInfo
        | DuTimer timer ->
            let device, addr = "", ""
            let plcType =
                match timer.Type with
                | TON | TOF | TMR -> timer.Type.ToString()

            let param:XgiSymbolCreateParams =
                let name, comment = timer.Name, $"TIMER {timer.Name}"
                { defaultSymbolCreateParam with Name=name; Comment=comment; PLCType=plcType; Address=addr; InitValue=null; Device=device; Kind=kindVar; }
            XGITag.createSymbolInfoWithDetail param
        | DuCounter counter ->
            let device, addr = "", ""
            let plcType =
                match counter.Type with
                | CTU | CTD | CTUD -> $"{counter.Type}_INT"       // todo: CTU_{INT, UINT, .... } 등의 종류가 있음...
                | CTR -> $"{counter.Type}"

            let param:XgiSymbolCreateParams =
                let name, comment = counter.Name, $"COUNTER {counter.Name}"
                { defaultSymbolCreateParam with Name=name; Comment=comment; PLCType=plcType; Address=addr; InitValue=null; Device=device; Kind=kindVar; }
            XGITag.createSymbolInfoWithDetail param

    let private xgiSymbolsToSymbolInfos (prjParams:XgiProjectParams) (kindVar:int) (xgiSymbols:XgiSymbol seq) : SymbolInfo list =
        xgiSymbols |> map (xgiSymbolToSymbolInfo prjParams kindVar) |> List.ofSeq


    let private storagesToSymbolInfos (prjParams:XgiProjectParams) (kindVar:int) : (IStorage seq -> SymbolInfo list) =
        storagesToXgiSymbol
        >> map snd
        >> xgiSymbolsToSymbolInfos prjParams kindVar

    /// <LocalVariable .../> 문자열 반환
    /// 내부 변환: Storages => [XgiSymbol] => [SymbolInfo] => Xml string
    let storagesToLocalXml (prjParams:XgiProjectParams) (localStorages:IStorage seq) (globalStoragesRefereces:IStorage seq) =
        let symbolInfos = [
            yield! storagesToSymbolInfos prjParams (int Variable.Kind.VAR) localStorages
            yield! storagesToSymbolInfos prjParams (int Variable.Kind.VAR_EXTERNAL) globalStoragesRefereces
        ]
        XGITag.generateLocalSymbolsXml symbolInfos

    /// <GlobalVariable .../> 문자열 반환
    /// 내부 변환: Storages => [XgiSymbol] => [SymbolInfo] => Xml string
    let storagesToGlobalXml (prjParams:XgiProjectParams) (globalStorages:IStorage seq) =
        //storagesToXml false globalStorages
        let symbolInfos = storagesToSymbolInfos prjParams (int Variable.Kind.VAR_GLOBAL) globalStorages
        XGITag.generateLocalSymbolsXml symbolInfos

