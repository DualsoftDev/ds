namespace PLC.CodeGen.LS

open System

open System.Linq
open System.Collections.Generic
open System.Security
open Dual.Common.Core.FS
open Engine.Core
open PLC.CodeGen.Common
open PLC.CodeGen.LS


[<AutoOpen>]
module internal XgiSymbolsModule =
    type XgxSymbol =
        | DuXgiVar of IXgxVar
        | DuTimer of TimerStruct
        | DuCounter of CounterBaseStruct
        | DuStorage of IStorage


    let storagesToXgiSymbol (storages: IStorage seq) : (IStorage * XgxSymbol) list =
        let timerOrCountersNames =
            storages
                .Filter(fun s -> s :? TimerCounterBaseStruct)
                .Select(fun struc -> struc.Name)
            |> HashSet

        [ for s in storages do
              match s with
              | :? IXgxVar as xgi -> Some(s, XgxSymbol.DuXgiVar xgi)
              | :? TimerStruct as ts -> Some(s, XgxSymbol.DuTimer ts)
              | :? CounterBaseStruct as cs -> Some(s, XgxSymbol.DuCounter cs)
              | _ ->
                  let name = (s :> INamed).Name

                  if timerOrCountersNames.Contains(name.Split(".")[0]) then
                      // skip timer/counter structure member : timer 나 counter 명 + "." + field name
                      None
                  else
                      Some(s, XgxSymbol.DuStorage s) ]
        |> List.choose id

    let autoAllocatorAdress (t:IStorage) (prjParams: XgxProjectParams) = 
        // address 가 "_" 인 symbol 에 한해서 자동으로 address 를 할당.
        // null 또는 다른 값이 지정되어 있으면, 그대로 사용한다.
        if t.Address = "" then  failwithlog $"ERROR. {t.Name} address empty."

        if RuntimeDS.Target = XGI 
            && t.Address.IsNonNull() 
            && t.Address <> TextAddrEmpty 
            && not(t.Address.StartsWith("%"))
        then t.Address <- $"%%{t.Address}"

        if t.Address = TextAddrEmpty then
            let allocatorFunctions =
                match prjParams.MemoryAllocatorSpec with
                | RangeSpec _ -> failwithlog "ERROR.  Should have already been converted to allocator functions."
                | AllocatorFunctions functions -> functions

            let {   BitAllocator = x
                    ByteAllocator = b
                    WordAllocator = w
                    DWordAllocator = d
                    LWordAllocator = l } =
                allocatorFunctions

            let allocator =
                match t.BoxedValue.GetType().GetMemorySizePrefix() with
                | "X" -> x
                | "B" -> b
                | "W" -> w
                | "D" -> d
                | "L" -> l
                | _ -> failwithlog "ERROR"

            if t.Name.StartsWith("_") then
                logWarn $"Something fish: trying to generate auto M address for {t.Name}"

            if t.Address <> TextAddrEmpty 
            then
                t.Address <- allocator ()

    let getXGITagInfo (address:string) (name:string) =
        match tryParseXGITag address with
        | Some tag -> address, tag.Device.ToString()
        | _ -> 
            if address = TextAddrEmpty 
            then  "", ""
            else  failwith $"Invalid tag address {address} for {name}"
        

    let xgiSymbolToSymbolInfo (prjParams: XgxProjectParams) (kindVar: int) (xgiSymbol: XgxSymbol) : SymbolInfo =
        match xgiSymbol with
        | DuStorage(:? ITag as t) ->
            let name = t.Name

            autoAllocatorAdress t prjParams
            let address, device = getXGITagInfo t.Address t.Name 
            let plcType = systemTypeToXgiTypeName t.DataType
            let comment = ""
            let initValue = null // PLCTag 는 값을 초기화 할 수 없다.

            { defaultSymbolInfo with
                Name = name
                Comment = comment
                Type = plcType
                Address = address
                Device = device.ToString()
                InitValue = initValue
                Kind = kindVar }

        // address 가 지정되지 않은 tag : e.g Timer, Counter 의 내부 멤버 변수들 EN, DN, CU, CD, ...
        | DuStorage t ->
            let symbolInfo =
             
                let plcType = systemTypeToXgiTypeName t.DataType
                let comment = SecurityElement.Escape t.Comment
               
                autoAllocatorAdress t prjParams
                let address, device = getXGITagInfo t.Address t.Name 

                { defaultSymbolInfo with
                    Name = t.Name
                    Comment = comment
                    Type = plcType
                    Device = device
                    Address = address
                    InitValue = t.BoxedValue
                    Kind = kindVar }

            symbolInfo

        | DuXgiVar xgi ->
            if kindVar = int Variable.Kind.VAR_GLOBAL then
                // Global 변수도 일단, XgiLocalVar type 으로 생성되므로, PLC 생성 시에만 global 로 override 해서 생성한다.
                { xgi.SymbolInfo with
                    Kind = kindVar
                    Address = xgi.Address }
            else
                xgi.SymbolInfo
        | DuTimer timer ->
            let device, addr = "", ""

            let plcType =
                match timer.Type with
                | TON
                | TOF
                | TMR -> timer.Type.ToString()

            let name, comment = timer.Name, $"TIMER {timer.Name}"

            { defaultSymbolInfo with
                Name = name
                Comment = comment
                Type = plcType
                Address = addr
                Device = device
                Kind = kindVar }
        | DuCounter counter ->
            let device, addr = "", ""

            let plcType =
                match counter.Type with
                | CTU
                | CTD
                | CTUD -> $"{counter.Type}_INT" // todo: CTU_{INT, UINT, .... } 등의 종류가 있음...
                | CTR -> $"{counter.Type}"

            let name, comment = counter.Name, $"COUNTER {counter.Name}"

            { defaultSymbolInfo with
                Name = name
                Comment = comment
                Type = plcType
                Address = addr
                Device = device
                Kind = kindVar }

    let private xgiSymbolsToSymbolInfos
        (prjParams: XgxProjectParams)
        (kindVar: int)
        (xgiSymbols: XgxSymbol seq)
        : SymbolInfo list =
        xgiSymbols |> map (xgiSymbolToSymbolInfo prjParams kindVar) |> List.ofSeq


    let private storagesToSymbolInfos (prjParams: XgxProjectParams) (kindVar: int) : (IStorage seq -> SymbolInfo list) =
        storagesToXgiSymbol >> map snd >> xgiSymbolsToSymbolInfos prjParams kindVar

    /// <LocalVariable .../> 문자열 반환
    /// 내부 변환: Storages => [XgiSymbol] => [SymbolInfo] => Xml string
    let storagesToLocalXml
        (prjParams: XgxProjectParams)
        (localStorages: IStorage seq)
        (globalStoragesRefereces: IStorage seq)
        =
        let symbolInfos =
            [ yield! storagesToSymbolInfos prjParams (int Variable.Kind.VAR) localStorages
              yield! storagesToSymbolInfos prjParams (int Variable.Kind.VAR_EXTERNAL) globalStoragesRefereces ]

        XGITag.generateLocalSymbolsXml symbolInfos

    /// <GlobalVariable .../> 문자열 반환
    /// 내부 변환: Storages => [XgiSymbol] => [SymbolInfo] => Xml string
    let storagesToGlobalXml (prjParams: XgxProjectParams) (globalStorages: IStorage seq) =
        //storagesToXml false globalStorages
        let symbolInfos =
            storagesToSymbolInfos prjParams (int Variable.Kind.VAR_GLOBAL) globalStorages

        (* check any error *)
        do
            let optError =
                symbolInfos
                |> map (fun si -> si.Validate())
                |> filter Result.isError
                |> Seq.tryHead

            match optError with
            | Some(Error err) -> failwith err
            | _ -> ()

        XGITag.generateGlobalSymbolsXml symbolInfos
