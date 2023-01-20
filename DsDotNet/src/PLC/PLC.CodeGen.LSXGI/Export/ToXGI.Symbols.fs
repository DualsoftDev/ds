namespace PLC.CodeGen.LSXGI

open System.Linq
open System.Collections.Generic

open Engine.Common.FS
open Engine.Core
open PLC.CodeGen.LSXGI


[<AutoOpen>]
module internal XgiSymbolsModule =
    type XgiSymbol =
        | DuTag         of ITagWithAddress
        | DuXgiLocalVar of IXgiLocalVar
        | DuTimer       of TimerStruct
        | DuCounter     of CounterBaseStruct


    let storagesToXgiSymbol(storages:IStorage seq) : (IStorage*XgiSymbol) list =
        let timerOrCountersNames =
            storages.Filter(fun s -> s :? TimerCounterBaseStruct)
                .Select(fun struc -> struc.Name)
                |> HashSet
                ;

        [
            for s in storages do
                match s with
                | :? ITagWithAddress as t ->
                    let name = (t :> INamed).Name
                    if timerOrCountersNames.Contains(name.Split(".")[0]) then
                        // skip timer/counter structure member : timer 나 counter 명 + "." + field name
                        None
                    else
                        Some (s, XgiSymbol.DuTag t)
                | :? IXgiLocalVar as xgi ->
                    Some (s, XgiSymbol.DuXgiLocalVar xgi)
                | :? TimerStruct as ts ->
                    Some (s, XgiSymbol.DuTimer ts)
                | :? CounterBaseStruct as cs ->
                    Some (s, XgiSymbol.DuCounter cs)
                | _ -> failwithlog "ERROR"
        ] |> List.choose id

    let xgiSymbolToSymbolInfo (kindVar:int) (xgiSymbol:XgiSymbol) : SymbolInfo =
        match xgiSymbol with
        | DuTag t ->
            let name, addr = t.Name, t.Address

            let device, memSize =
                match t.Address with
                | RegexPattern @"%([IQM])([XBWL]).*$" [iqm; mem] -> iqm, mem
                | _ -> "M", "X" //test ahn 주소없는 오토변수 방법 타입 ??

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

        | DuXgiLocalVar xgi ->
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

    let xgiSymbolsToSymbolInfos (isLocal:bool) (xgiSymbols:XgiSymbol seq) : SymbolInfo list =
        let kindVar = int (if isLocal then Variable.Kind.VAR else Variable.Kind.VAR_GLOBAL)
        xgiSymbols |> map (xgiSymbolToSymbolInfo kindVar) |> List.ofSeq

    /// 내부 변환: Storages => [XgiSymbol] => [SymbolInfo] => Xml string
    ///
    /// <GlobalVariable .../> or <LocalVariable .../> 문자열 반환
    let private storagesToXml (isLocal:bool) =
        let toXml = if isLocal then XGITag.generateLocalSymbolsXml else XGITag.generateGlobalSymbolsXml
        storagesToXgiSymbol
        >> map snd
        >> xgiSymbolsToSymbolInfos isLocal
        >> toXml

    /// <LocalVariable .../> 문자열 반환
    let storagesToLocalXml  (storages:IStorage seq) = storagesToXml true storages
    /// <GlobalVariable .../> 문자열 반환
    let storagesToGlobalXml (storages:IStorage seq) = storagesToXml false storages

