namespace PLC.CodeGen.LSXGI

open System.Linq
open System.Reflection
open System.Collections.Generic

open Engine.Common.FS
open Engine.Core
open PLC.CodeGen.LSXGI
open PLC.CodeGen.Common
open PLC.CodeGen.LSXGI.Config.POU.Program.LDRoutine


[<AutoOpen>]
module internal XgiSymbolsModule =
    type internal XgiSymbol =
        | DuTag         of ITagWithAddress
        | DuXgiLocalVar of IXgiLocalVar
        | DuTimer       of TimerStruct
        | DuCounter     of CounterBaseStruct


    let internal storagesToXgiSymbol(storages:IStorage seq) : XgiSymbol list = [
        let timerOrCountersNames =
            storages.Filter(fun s -> s :? TimerCounterBaseStruct)
                .Select(fun struc -> struc.Name)
                |> HashSet
                ;

        for s in storages do
            match s with
            | :? ITagWithAddress as t ->
                let name = (t :> INamed).Name
                if timerOrCountersNames.Contains(name.Split(".")[0]) then
                    // skip timer/counter structure member : timer 나 counter 명 + "." + field name
                    ()
                else
                    XgiSymbol.DuTag t
            | :? IXgiLocalVar as xgi ->
                XgiSymbol.DuXgiLocalVar xgi
            | :? TimerStruct as ts ->
                XgiSymbol.DuTimer ts
            | :? CounterBaseStruct as cs ->
                XgiSymbol.DuCounter cs
            | _ -> failwithlog "ERROR"
    ]

    let internal xgiSymbolsToSymbolInfos (xgiSymbols:XgiSymbol seq) : SymbolInfo list =
        let kindVar = int Variable.Kind.VAR
        [
            for s in xgiSymbols do
                match s with
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
        ]



