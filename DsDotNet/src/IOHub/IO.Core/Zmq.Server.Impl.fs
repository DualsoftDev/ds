(* IO.Core using Zero MQ *)

namespace IO.Core

open System
open NetMQ
open NetMQ.Sockets
open Dual.Common.Core.FS
open System.Collections.Generic
open System.Reactive.Linq



[<AutoOpen>]
module internal ZmqServerImplModule =
    let mutable ioSpec:IOSpec = null
    /// e.g {"p/o", <Paix Output Buffer manager>}
    let streamManagers = new Dictionary<string, StreamManager>()

    /// tag 별 address 정보를 저장하는 dictionary
    let tagDic = new Dictionary<string, AddressSpec>()
    let mutable clients: ClientIdentifier list = []
    let mutable serverSocket: RouterSocket = null

    let getVendor (addr: string) : (VendorSpec * string) =
        match addr with
        | RegexPattern "^([^/]+)/([^/]+)$" [ vendor; address ] ->
            let v = ioSpec.Vendors |> Seq.find (fun v -> v.Location = vendor)
            v, address
        | _ ->
            let v = ioSpec.Vendors |> Seq.find (fun v -> v.Location = "")
            v, addr

    // e.g "p/ob1"
    let (|AddressPattern|_|) (str: string) =
        if tagDic.ContainsKey(str) then
            Some(tagDic.[str])
        else
            option {
                let (vendor, address) = getVendor str

                match vendor.AddressResolver.GetAddressInfo(address) with
                | true, memType, offset, contentBitLength ->
                    let! f = vendor.Files |> Seq.tryFind (fun f -> f.Name = memType)
                    let addressSpec = AddressSpec(f, bitSizeToEnum (contentBitLength), offset)
                    tagDic.Add(str, addressSpec)
                    return addressSpec
                | _ -> return! None
            }

    let (|AddressAssignPattern|_|) (str: string) =
        match str with
        | RegexPattern "([^=]+)=(\w+)" [ AddressPattern addr; value ] -> Some(addr, value)
        | _ -> None

    let readAddress (clientRequestInfo: ClientRequestInfo, address: string) : obj =
        match address with
        | AddressPattern ap ->
            let fs = ap.IOFileSpec

            if ap.MemoryType = MemoryType.String then
                // address: e.g "p/shello" ==> 'p' vendor 의 's' file 의 'hello' 문자열
                let keyPrefix = fs.GetPath() // e.g "p/s
                let key = address.Substring(keyPrefix.Length) // e.g "hello"
                fs.ReadStrings [| key |]
            else
                let offset = ap.Offset
                let bufferManager = fs.StreamManager :?> StreamManager
                bufferManager.VerifyOffsets(clientRequestInfo, ap.MemoryType, [| offset |])

                match ap.MemoryType with
                | MemoryType.Bit   -> bufferManager.readBit(offset) :> obj
                | MemoryType.Byte  -> bufferManager.readU8(offset)
                | MemoryType.Word  -> bufferManager.readU16(offset)
                | MemoryType.DWord -> bufferManager.readU32(offset)
                | MemoryType.LWord -> bufferManager.readU64(offset)
                | _ -> failwithf ($"Unknown data type : {ap.MemoryType}")
        | _ -> failwithf ($"Unknown address pattern : {address}")

    /// "write p/ob1=1 p/ix2=0" : 비효율성 인정한 version.  buffer manager 및 dataType 의 다양성 공존
    let writeAddressWithValue
        (
            clientRequestInfo: ClientRequestInfo,
            addressWithAssignValue: string
        ) : IMemoryChangeInfo =
        let cri = clientRequestInfo

        let parseBool (s: string) =
            match s.ToLower() with
            | "1" | "true" -> true
            | "0" | "false" -> false
            | _ -> failwithf ($"Invalid boolean value: {s}")

        match addressWithAssignValue with
        | AddressAssignPattern(addressPattern, value) ->
            let ap = addressPattern
            let offset = ap.Offset
            let fs = ap.IOFileSpec

            if ap.MemoryType = MemoryType.String then
                // addressPattern: e.g "p/shello=world" ==> 'p' vendor 의 's' file 의 'hello' 문자열 key에 'world' 문자열을 쓰기
                let keyPrefix = fs.GetPath() // e.g "p/s

                let key =
                    match addressWithAssignValue with
                    | RegexPattern "([^=]+)=(\w+)" [ keyPart; value ] -> keyPart.Substring(keyPrefix.Length) // e.g "hello"
                    | _ -> failwith $"Invalid format: {addressWithAssignValue}"

                fs.WriteString(key, value)
                SingleStringChangeInfo(cri, fs, key, value)
            else
                let bufferManager = fs.StreamManager :?> StreamManager
                bufferManager.VerifyOffsets(cri, ap.MemoryType, [| offset |])

                let mutable objValue: obj = null

                match ap.MemoryType with
                | MemoryType.Bit   -> objValue <- [|parseBool(value)|];    bufferManager.writeBit (cri, offset, parseBool(value))
                | MemoryType.Byte  -> objValue <- [|Byte.Parse(value)|];   bufferManager.writeU8s cri ([offset, Byte.Parse(value)])
                | MemoryType.Word  -> objValue <- [|UInt16.Parse(value)|]; bufferManager.writeU16 cri (offset, UInt16.Parse(value))
                | MemoryType.DWord -> objValue <- [|UInt32.Parse(value)|]; bufferManager.writeU32 cri (offset, UInt32.Parse(value))
                | MemoryType.LWord -> objValue <- [|UInt64.Parse(value)|]; bufferManager.writeU64 cri (offset, UInt64.Parse(value))
                | _ -> failwithf ($"Unknown data type : {ap.MemoryType}")

                let fs = bufferManager.FileSpec
                IOChangeInfo(cri, fs, ap.MemoryType, [| offset |], objValue)

        | _ -> failwithf ($"Unknown address with assignment pattern : {addressWithAssignValue}")

    /// Client 로부터 받은 multi-message format
    [<AutoOpen>]
    module internal MultiMessageFromClient =
        let ClientId  = 0
        let Command   = 1
        let RequestId = 2
        let ArgGroup1 = 3
        let ArgGroup2 = 4
        let ArgGroup3 = 5

        let TagKindName = ArgGroup1
        let Offsets     = ArgGroup2
        let Values      = ArgGroup3


    let fetchBufferManagerAndOffsets (isBitIndex: bool) (multiMessages: NetMQFrame[]) =
        let bufferManager =
            let name = multiMessages[TagKindName].ConvertToString().ToLower()
            streamManagers[name]

        let offsets = ByteConverter.BytesToTypeArray<int>(multiMessages[Offsets].Buffer)

        for byteIndex in offsets |> map (fun n -> if isBitIndex then n / 8 else n) do
            if bufferManager.FileStream.Length < byteIndex then
                failwithf ($"Invalid address: {byteIndex}")

        bufferManager, offsets

    let fetchForRead = fetchBufferManagerAndOffsets false
    let fetchForReadBit = fetchBufferManagerAndOffsets true

    let fetchForWrite (multiMessages: NetMQFrame[]) =
        let bm, offsets = fetchForRead multiMessages
        let values = multiMessages[Values].Buffer
        bm, offsets, values

    /// NetMQ 의 ConvertToString() bug 대응 용 코드.  문자열의 맨 마지막에 '\0' 이 붙는 경우 강제 제거.
    let removeTrailingNullChar (str: string) =
        if str.NonNullAny() && str[str.Length - 1] = '\000' then
            str.Substring(0, str.Length - 1)
        else
            str

    let periodicPingClients () =
        Observable
            .Interval(TimeSpan.FromSeconds(3))
            .Subscribe(fun n ->
                for client in clients do
                    serverSocket
                        .SendMoreFrame(client)
                        .SendMoreFrame(-1 |> ByteConverter.ToBytes)
                        .SendFrame("PING")

                ())


//let showSamples (vendorSpec:VendorSpec) (addressExtractor:IAddressInfoProvider) =
//    let v = vendorSpec
//    match v.Name with
//    | "Paix" ->
//        match addressExtractor.GetAddressInfo("ox12.1") with
//        | true, memoryType, byteOffset, bitOffset, contentBitLength ->
//            assert (memoryType = "o")
//            assert (bitOffset = 1)
//            assert (byteOffset = 12)
//            assert (contentBitLength = 1)
//        | _ ->
//            failwithf($"Invalid address format: {v.Name}, {v.Dll}, {v.ClassName}")
//        match addressExtractor.GetAddressInfo("ob12") with
//        | true, memoryType, byteOffset, bitOffset, contentBitLength ->
//            assert (memoryType = "o")
//            assert (bitOffset = 0)
//            assert (byteOffset = 12)
//            assert (contentBitLength = 8)
//        | _ ->
//            failwithf($"Invalid address format: {v.Name}, {v.Dll}, {v.ClassName}")
//    | "LsXGI" ->
//        match addressExtractor.GetAddressInfo("%IX30.3") with
//        | true, memoryType, byteOffset, bitOffset, contentBitLength ->
//            assert (memoryType = "i")
//            assert (bitOffset = 3)
//            assert (byteOffset = 30)
//            assert (contentBitLength = 1)
//        | _ ->
//            failwithf($"Invalid address format: {v.Name}, {v.Dll}, {v.ClassName}")
//    | _ ->
//        ()
