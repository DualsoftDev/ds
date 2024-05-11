namespace PLC.CodeGen.LS

open System.Linq
open System.Collections.Generic
open System.Security
open Dual.Common.Core.FS
open Engine.Core
open PLC.CodeGen.Common
open PLC.CodeGen.LS
open System


[<AutoOpen>]
module internal XgiSymbolsModule =
    type XgxSymbol =
        | DuXgiVar of IXgxVar
        | DuTimer of TimerStruct
        | DuCounter of CounterBaseStruct
        | DuStorage of IStorage


    let storagesToXgxSymbol (storages: IStorage seq) : (IStorage * XgxSymbol) list =
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

    let autoAllocatorAdress (t:IStorage) (prjParam: XgxProjectParams) = 
        // address 가 "_" 인 symbol 에 한해서 자동으로 address 를 할당.
        // null 또는 다른 값이 지정되어 있으면, 그대로 사용한다.
        if t.Address.IsNullOrEmpty() then
            failwithlog $"ERROR. {t.Name} address empty."

        else if t.Address = TextAddrEmpty then
            let allocatorFunctions =
                match prjParam.MemoryAllocatorSpec with
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

            if t.Address = TextAddrEmpty || t.Address = TextSkip then
                let addr:string = allocator()

                if "M0575E" = addr
                then    
                    ()
                t.Address <- addr

    let getXGXTagInfo (targetType:PlatformTarget) (address:string) (name:string) =  
        match targetType with
        | XGI ->
            match tryParseXGITag address with
            |Some tag -> 
                address, tag.Device.ToString()
                , if tag.DataType = PLCHwModel.DataType.Bit then tag.BitOffset else tag.ByteOffset    
            |None ->
                failwithlog $"tryParse{targetType} {name} {address} error"

        | XGK ->    
            match tryParseXGKTag address with
            |Some tag -> 
                match tag.DataType with
                | PLCHwModel.DataType.Bit -> address, tag.Device.ToString(), tag.BitOffset
                | PLCHwModel.DataType.Word -> address, tag.Device.ToString(), tag.ByteOffset / 2
                | _-> failwithlog $"XGK Not supported plc {tag.DataType} type"
                         
            |None ->
                failwithlog $"tryParse{targetType} {name} {address} error"

        | _ -> failwithlog $"Not supported plc {targetType} type"
        

    let xgxSymbolToSymbolInfo (prjParam: XgxProjectParams) (kindVar: int) (xgxSymbol: XgxSymbol) : SymbolInfo =

        match xgxSymbol with
        | DuStorage(:? ITag as t) ->
            let name = t.Name
            //전처리  XGI % 생략시 자동 붙히기
            match prjParam.TargetType with
                | XGI -> if t.Address <> TextAddrEmpty && not(t.Address.StartsWith("%"))
                         then
                            t.Address <- $"%%{t.Address}"
                | _ ->   ()

            autoAllocatorAdress t prjParam
            let address, device, devPos = getXGXTagInfo prjParam.TargetType t.Address t.Name
            let plcType = systemTypeToXgxTypeName prjParam.TargetType t.DataType
            let comment = ""
            let initValue = null // PLCTag 는 값을 초기화 할 수 없다.

            { defaultSymbolInfo with
                Name = name
                Comment = comment
                Type = plcType
                Address = address
                DevicePos = devPos
                Device = device.ToString()
                InitValue = initValue
                Kind = kindVar }

        // address 가 지정되지 않은 tag : e.g Timer, Counter 의 내부 멤버 변수들 EN, DN, CU, CD, ...
        | DuStorage t ->
            let symbolInfo =

                let plcType = systemTypeToXgxTypeName prjParam.TargetType t.DataType
                let comment = SecurityElement.Escape t.Comment
               
                let address, device, devPos = 
                    match prjParam.TargetType with
                    | XGI ->
                        let _ = t.BoxedValue.GetType().GetMemorySizePrefix()
                        if t.Address = TextAddrEmpty then  //XGI 는 TextAddrEmpty 선언된 부분만 자동생성
                            autoAllocatorAdress t prjParam
                        elif t.Address = TextSkip then
                            t.Address <- ""

                        t.Address, "", -1
                    | XGK ->
                        if t.Address = "" then  //XGK 는 무조건 address 가 있어야 한다.
                            t.Address <- TextAddrEmpty
                        
                        autoAllocatorAdress t prjParam
                        getXGXTagInfo prjParam.TargetType t.Address t.Name

                    | _ ->
                        failwithf "Invalid target type: %A" prjParam.TargetType


                { defaultSymbolInfo with
                    Name = t.Name
                    Comment = comment
                    Type = plcType
                    Device = device
                    Address = address // xgk address 는 xml에 저장 안됨 devPos 처리
                    DevicePos = devPos
                    InitValue = t.BoxedValue
                    Kind = kindVar }

            symbolInfo
        //DuXgxVar ?
        | DuXgiVar xgx ->
            match prjParam.TargetType with
            | XGI ->
                if kindVar = int Variable.Kind.VAR_GLOBAL then
                // Global 변수도 일단, XgiLocalVar type 으로 생성되므로, PLC 생성 시에만 global 로 override 해서 생성한다.
                    { xgx.SymbolInfo with
                        Kind = kindVar
                        Address = xgx.Address }
                else
                     xgx.SymbolInfo
            | XGK ->
                if xgx.Address = "" then  //XGK 는 무조건 address 가 있어야 한다.
                    xgx.Address <- TextAddrEmpty

                autoAllocatorAdress xgx prjParam
                let address, device, devPos = getXGXTagInfo prjParam.TargetType xgx.Address xgx.Name
                { xgx.SymbolInfo with
                    Kind = kindVar
                    Address = address
                    Device = device
                    DevicePos = devPos }
            | _ ->
                    failwithf "Invalid target type: %A" prjParam.TargetType




        | DuTimer timer ->
            let device, addr, devicePos  =
                match prjParam.TargetType with
                | XGK ->
                    let offset = prjParam.TimerCounterGenerator()
                    timer.XgkStructVariableDevicePos <- offset
                    "T", timer.XgkStructVariableName, offset
                | _ -> "", "", -1   
      
            let plcType = timer.Type.ToString() 
            let name, comment = timer.Name, $"TIMER {timer.Name}"

            { defaultSymbolInfo with
                Name = name
                Comment = comment
                Type = plcType
                Address = addr
                Device = device
                DevicePos = devicePos
                Kind = kindVar }

        | DuCounter counter ->
            let device, addr, devicePos  =
                match prjParam.TargetType with
                | XGK ->
                    let offset = prjParam.CounterCounterGenerator()
                    counter.XgkStructVariableDevicePos <- offset
                    "C", counter.XgkStructVariableName, offset
                | _ -> "", "", -1 


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
                DevicePos = devicePos
                Kind = kindVar }

    let private xgxSymbolsToSymbolInfos
        (prjParam: XgxProjectParams)
        (kindVar: int)
        (xgxSymbols: XgxSymbol seq)
      : SymbolInfo list =
        xgxSymbols |> map (xgxSymbolToSymbolInfo prjParam kindVar) |> List.ofSeq


    let private storagesToSymbolInfos (prjParam: XgxProjectParams) (kindVar: int) : (IStorage seq -> SymbolInfo list) =
        storagesToXgxSymbol >> map snd >> xgxSymbolsToSymbolInfos prjParam kindVar

    /// <LocalVariable .../> 문자열 반환
    /// 내부 변환: Storages => [XgiSymbol] => [SymbolInfo] => Xml string
    let storagesToLocalXml
        (prjParam: XgxProjectParams)
        (localStorages: IStorage seq)
        (globalStoragesRefereces: IStorage seq)
      : string =
        let symbolInfos =
            [ yield! storagesToSymbolInfos prjParam (int Variable.Kind.VAR) localStorages
              yield! storagesToSymbolInfos prjParam (int Variable.Kind.VAR_EXTERNAL) globalStoragesRefereces ]

        XGITag.generateLocalSymbolsXml prjParam symbolInfos

    /// <GlobalVariable .../> 문자열 반환
    /// 내부 변환: Storages => [XgiSymbol] => [SymbolInfo] => Xml string
    let storagesToGlobalXml (prjParam: XgxProjectParams) (globalStorages: IStorage seq) =
        //storagesToXml false globalStorages
        let symbolInfos =
            storagesToSymbolInfos prjParam (int Variable.Kind.VAR_GLOBAL) globalStorages

        (* check any error *)
        do
            let optError =
                symbolInfos
                |> map (fun si -> si.Validate(prjParam.TargetType))
                |> filter Result.isError
                |> Seq.tryHead

            match optError with
            | Some(Error err) -> failwith err
            | _ -> ()


            let usedAddresses =
                symbolInfos
                |> Seq.filter (fun f -> not(f.Address.IsNullOrEmpty()))
                |> Array.ofSeq

            //check if there is any duplicated address
            let duplicatedAddresses =
                usedAddresses
                |> Array.groupBy (fun f -> f.Address)
                |> Array.filter (fun (_, vs) -> vs.Length > 1)

            // prints duplications
            if duplicatedAddresses.Length > 0 then
                let dupItems =
                    duplicatedAddresses
                    |> map (fun (address, vs) ->
                        let names = vs |> map (fun var -> var.Name) |> String.concat ", "
                        $"  {address}: {names}")
                    |> String.concat Environment.NewLine

                failwithlog
                    $"Total {duplicatedAddresses.Length} 중복주소 items:{Environment.NewLine}{dupItems}"

        XGITag.generateGlobalSymbolsXml prjParam symbolInfos
