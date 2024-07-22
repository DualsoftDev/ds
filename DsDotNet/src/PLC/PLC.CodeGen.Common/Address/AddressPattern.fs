namespace PLC.CodeGen.Common

open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Globalization
open System.Text.RegularExpressions
open System


[<AutoOpen>]
module LSEAddressPattern =


    let subBitPattern (size: int) (str: string) =
        match System.Int32.TryParse(str) with
        | true, v when v < size -> Some(v)
        | _ -> None

    let (|ByteSubBitPattern|_|) = subBitPattern 8
    let (|WordSubBitPattern|_|) = subBitPattern 16
    let (|DWordSubBitPattern|_|) = subBitPattern 32
    let (|LWordSubBitPattern|_|) = subBitPattern 64
    let (|DevicePattern|_|) (str: string) = DU.fromString<DeviceType> str
    let (|DataTypePattern|_|) (str:string)=
        try
            Some <|  str.FromDeviceMnemonic()
        with exn ->
            None

    let (|HexPattern|_|) (str: string) =
            match System.Int32.TryParse(str, NumberStyles.HexNumber, CultureInfo.CurrentCulture) with
            | true, v -> Some(v)
            | _ -> None


    let list5Digit = [ "L"; "N"; "D"; "R" ]
    let list4Digit = [ "P"; "M"; "K"; "F"; "T"; "C" ]

    let getXgkBitText (device:string, offset: int) : string =
        let word = offset / 16
        let bit = offset % 16
        match device.ToUpper() with
        | d when list5Digit |> List.contains d ->
            device + sprintf "%05i.%X" word bit
        | d when list4Digit |> List.contains d ->
            device + sprintf "%04i%X" word bit
        | _ -> failwithf $"XGK device({device})는 지원하지 않습니다."

    let getXgkWordText (device:string, offsetByte: int) : string =
        if offsetByte % 2 = 1 then
            failwithf $"XGK 주소는 Word 타입 지원하려면 offsetByte({offsetByte})가 2의 배수야 합니다."

        let wordIndex = offsetByte / 2
        match device.ToUpper() with
        | d when list5Digit |> List.contains d ->
            sprintf "%s%05i" d wordIndex
        | d when list4Digit |> List.contains d ->
            sprintf "%s%04i" d wordIndex
        | _ -> failwithf $"XGK device({device})는 지원하지 않습니다."

    let getXgkTextByType (device:string, offset: int, isBool:bool) : string =
        if isBool
        then getXgkBitText(device, offset)
        else 
           getXgkWordText(device, offset)  

            
    let getXgKTextByTag (device:LsTagInfo) : string =
        getXgkTextByType (device.Device.ToString(), device.BitOffset, device.DataType = DataType.Bit)

    let getXgiIOTextBySize (device:string, offset: int, bitSize:int, iSlot:int, sumBit:int) : string =
        if bitSize = 1
        then $"%%{device}X0.{iSlot}.{(offset-sumBit) % 64}"  //test ahn 아날로그 base 1로 일단 고정
        else 
            match bitSize with  //test ahn  xgi 규격확인
            | 8 -> $"%%{device}B{offset+1024}"
            | 16 -> $"%%{device}W{offset+1024}"
            | 32 -> $"%%{device}D{offset+1024}"
            | 64 -> $"%%{device}L{offset+1024}"
            | _ -> failwithf $"Invalid size :{bitSize}"
            

    let getXgiMemoryTextBySize (device:string, offset: int, bitSize:int) : string =
        if bitSize = 1
        then $"%%{device}X{offset}" 
        else 
            if offset%8 = 0 then getXgkWordText(device, offset/8)
            else failwithf $"Word Address는 8의 배수여야 합니다. {offset}"


    let createTagInfo = LsTagInfo.Create >> Some
    let (|LsTagXGIPattern|_|) ((modelId: int option), (tag: string)) =
        let regexXGI1 = @"^%([IQUMLKFNRAW])X([\d]+)$"
        let regexXGI2 = @"^%([IQU])X(\d+)\.(\d+)\.(\d+)$"
        let regexXGI3 = @"^%([IQUMLKFNRAW])([BWDL])(\d+)$"
        let regexXGI4 = @"^%([IQUMLKFNRAW])([BWDL])(\d+)\.(\d+)$"


        let tag = tag.ToUpper()       
        match tag with
        //%IX13412, %MX123423414
        | RegexPattern regexXGI1 [ DevicePattern device; Int32Pattern bit ] -> 
            createTagInfo (tag, device, DataType.Bit, bit, modelId)  

        //%IX0.0.1, %QX0.0.1, %UX0.0.1
        | RegexPattern regexXGI2 [ DevicePattern device; Int32Pattern file; WordSubBitPattern element; LWordSubBitPattern bit ] ->
            let baseStep, slotStep = if device.Equals(DeviceType.U) then 512 * 16, 512 else 64 * 16, 64
            let totalBitOffset = file * baseStep + element * slotStep + bit
            createTagInfo (tag, device, DataType.Bit, totalBitOffset, modelId)

        //%IW0, %IL120, %MD120
        | RegexPattern regexXGI3 [ DevicePattern device; DataTypePattern dataType; Int32Pattern offset ] ->
            let byteOffset = offset * dataType.GetByteLength()
            let totalBitOffset = byteOffset * 8
            createTagInfo (tag, device, dataType, totalBitOffset, modelId)
   
        //%IW0.0, %IL120.45, %MD120.31
        | RegexPattern regexXGI4 [ DevicePattern device; DataTypePattern dataType;Int32Pattern element; Int32Pattern bit ] ->
            let validBit =
                match dataType with
                |Bit    ->  failwithf $"Pattern Error: {tag}"
                |Byte  ->   (|ByteSubBitPattern|_|)  $"{bit}"
                |Word  ->   (|WordSubBitPattern|_|)  $"{bit}"
                |DWord  ->  (|DWordSubBitPattern|_|) $"{bit}"
                |LWord  ->  (|LWordSubBitPattern|_|) $"{bit}"
                |Continuous -> failwithf $"Pattern Error : {tag}"

            if validBit.IsSome then
                let uMemStep = if device.Equals(DeviceType.U) then 8 else 1
                let bitStandard = 8 * uMemStep / dataType.GetByteLength()

                let bitSet = (bit % bitStandard) * dataType.GetByteLength() * 8
                let elementSet = (element % 16 + bit / bitStandard) * 8 * 8 * uMemStep

                let offset = bitSet + elementSet 
                createTagInfo (tag, device, DataType.Bit, offset, modelId)
            else None

        | _ -> 
            logWarn $"Failed to parse XGI tag : {tag}"
            None



    let (|LsTagXGKPattern|_|) ((modelId: int option), (tag: string), (isBool: bool option)) =
        let regexXGK1   =  @"^([PMKFTC])(\d{4})$"                 
        let regexXGK1_1 =  @"^([PMKFTC])(\d+)$"                 
        let regexXGK2   =  @"^([PMKF])(\d{4})([\da-fA-F])$"       
        let regexXGK2_1 =  @"^([PMKF])([\da-fA-F])$"       
        let regexXGK2_2 =  @"^([PMKF])(\d+)([\da-fA-F])$"       
        let regexXGK3   =  @"^([LNDRZR])(\d+)$"                 
        let regexXGK4   =  @"^([LNDRZR])(\d+)\.([\da-fA-F])$"   
        let regexXGK5   =  @"^([U])(\d+)\.(\d+)$"                 
        let regexXGK6   =  @"^([U])(\d+)\.(\d+)\.([\da-fA-F])$"   
        let regexXGK7   =  @"^([S])(\d+)\.(\d+)$"                 
        let regexXGK8   =  @"^([Z])(\d+)\$"                       

        let bitCheck  = isBool.IsSome && isBool.Value
        let wordCheck = isBool.IsSome && not(isBool.Value)

        let tag = tag.ToUpper()       
        match tag with
        //P1341, M2341
        | RegexPattern regexXGK1 [ DevicePattern device; Int32Pattern word; ] when isBool.IsNone ->
            let totalBitOffset = word * 16
            createTagInfo (tag, device, DataType.Word, totalBitOffset, modelId)

        //P12, M3
        | RegexPattern regexXGK1_1 [ DevicePattern device; Int32Pattern word; ] when wordCheck ->
            let totalBitOffset = word * 16
            createTagInfo (tag, device, DataType.Word, totalBitOffset, modelId)

        //P13412, M2341F
        | RegexPattern regexXGK2 [ DevicePattern device; Int32Pattern word; HexPattern hexaBit ] when isBool.IsNone -> 
            let totalBitOffset = word * 16 + hexaBit
            createTagInfo (tag, device, DataType.Bit, totalBitOffset, modelId)  

        //PF, M0
        | RegexPattern regexXGK2_1 [ DevicePattern device; HexPattern hexaBit ] when bitCheck -> 
            let totalBitOffset = hexaBit
            createTagInfo (tag, device, DataType.Bit, totalBitOffset, modelId)  

        //P0F, M00
        | RegexPattern regexXGK2_2 [ DevicePattern device; Int32Pattern word; HexPattern hexaBit ] when bitCheck -> 
            let totalBitOffset = word * 16 + hexaBit
            createTagInfo (tag, device, DataType.Bit, totalBitOffset, modelId)  

            
          //D13411, R13411
        | RegexPattern regexXGK3 [ DevicePattern device; Int32Pattern word; ] when wordCheck->
            let totalBitOffset = word * 16
            createTagInfo (tag, device, DataType.Word, totalBitOffset, modelId)

          //D13411.9, R13411.F
        | RegexPattern regexXGK4 [ DevicePattern device; Int32Pattern word; HexPattern hexaBit ] when bitCheck-> 
            let totalBitOffset = word * 16 + hexaBit
            createTagInfo (tag, device, DataType.Bit, totalBitOffset, modelId)
  
        //U23.31  //채널 31max   
        | RegexPattern regexXGK5 [ DevicePattern device; Int32Pattern file; DWordSubBitPattern sub ] when wordCheck ->
            let totalBitOffset = (file * 32 * 16) + (sub * 16) 
            createTagInfo (tag, device, DataType.Word, totalBitOffset, modelId)

        //U23.31.F  //채널 31max   
        | RegexPattern regexXGK6 [ DevicePattern device; Int32Pattern file; DWordSubBitPattern sub; HexPattern hexaBit ] when bitCheck-> 
            let totalBitOffset = (file * 32 * 16) + (sub * 16) + hexaBit
            createTagInfo (tag, device, DataType.Bit, totalBitOffset, modelId)
    
        //S23.31  //bit만 가능  
        | RegexPattern regexXGK7 [ DevicePattern device; Int32Pattern word; WordSubBitPattern bit] when bitCheck-> 
            let totalBitOffset = word * 16 + bit
            createTagInfo (tag, device, DataType.Bit, totalBitOffset, modelId)
        //Z23  //word만 가능  
        | RegexPattern regexXGK8 [ DevicePattern device; Int32Pattern word] when wordCheck ->
            let totalBitOffset = word * 16 
            createTagInfo (tag, device, DataType.Word , totalBitOffset, modelId)
    
        | _ -> 
            logWarn $"Failed to parse XGK tag : {tag}"
            None


    let tryParseXGITag tag = (|LsTagXGIPattern|_|) (None, tag)
    ///XGK 검증필요
    let tryParseXGKTag tag = (|LsTagXGKPattern|_|) (None, tag, None)
    let tryParseXGKTagByBitType tag isBit = (|LsTagXGKPattern|_|) (None, tag, Some isBit)
    let tryParseXGITagByCpu (tag: string) (modelId: int) = (|LsTagXGIPattern|_|) (modelId |> Some, tag)
    ///XGK 검증필요
    let tryParseXGKTagByCpu (tag: string) (modelId: int) = (|LsTagXGKPattern|_|) (modelId |> Some, tag, None)

    let isXgiTag tag = tryParseXGITag tag |> Option.isSome
    ///XGK 검증필요
    let isXgkTag tag = tryParseXGKTag tag |> Option.isSome


type XgkAddress private () =
    static let parseXgkAddress (addr:string) =

        assert(isInUnitTest())      // 일단 unit test 사용 only 로..  완전하게 구현 불가능한 함수.

        match addr with
        // P/M 에 대한 Bit 지정은 5자리를 full 로 채워야 한다.
        | RegexPattern @"^([PM])(\d{4})([\da-fA-F]+)$" [DevicePattern device; Int32Pattern word; HexPattern hexaBit] -> 
            device, word, Some(hexaBit)
        // P/M 에 대한 Word 지정은 4자리를 full 로 채워야 한다.
        | RegexPattern @"^([PM])(\d{4})$" [DevicePattern device; Int32Pattern word] -> 
            device, word, None
        // 나머지는 모두 bit 로 간주
        | RegexPattern @"^([PM])(\d{0,4})([\da-fA-F]+)$" [DevicePattern device; Int32Pattern word; HexPattern hexaBit] -> 
            device, word, Some(hexaBit)

        // W123456, W12345F : 꽉 채워지는 경우. no ambiguity
        | RegexPattern @"^([W])(\d{5})([\da-fA-F])$" [DevicePattern device; Int32Pattern word; HexPattern hexaBit] -> 
            device, word, Some(hexaBit)
        // W1234F : 덜 채워졌지만, hex 로 끝나는 경우. no ambiguity
        | RegexPattern @"^([W])(\d{1,4})([a-fA-F])$" [DevicePattern device; Int32Pattern word; HexPattern hexaBit] -> 
            device, word, Some(hexaBit)
        // W12345 : 덜 채워졌지만, hex 로 끝나지 않는 경우. ambiguous.  마지막은 강제로 hex 로 취급한다.
        | RegexPattern @"^([W])(\d{4})(\d)$" [DevicePattern device; Int32Pattern word; HexPattern hexaBit] -> 
            device, word, Some(hexaBit)

        | _ -> failwithf $"Invalid XGK address : {addr}"

    member val Device:string = null with get, set
    member val WordOffset = 0 with get, set
    member val BitOffset:int option = None with get, set
    member x.TotalBitOffset =
        let w = x.WordOffset
        match x.BitOffset with
        | Some bit -> w * 16 + bit
        | None -> w * 16

    static member FromAddress (address:string) =
        let device, word, bit = parseXgkAddress address
        let xgkAddress = new XgkAddress(Device = device.ToString(), WordOffset = word, BitOffset = bit)
        xgkAddress


[<Extension>]
type LSEAddressPatternExt =
    [<Extension>] static member IsXGIAddress (x:string) = isXgiTag x 
///XGK 검증필요
    [<Extension>] static member IsXGKAddress (x:string) = isXgkTag x 
