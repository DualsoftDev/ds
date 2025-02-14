namespace IO.Core

open System
open NetMQ
open System.IO
open Dual.Common.Core.FS
open IO.Spec
open Newtonsoft.Json
open System.Runtime.CompilerServices

[<AutoOpen>]
module rec ZmqSpec =
    type ErrorMessage = string
    /// e.g "i", "o", "m", "p/o", "p/i", ..
    type MemoryName = string
    /// string type 의 key, value pair : string * string
    type StringKeyValue = string * string

    type NoMoreInputOK() = class end

    type IOResult = Result<obj, ErrorMessage>
    type TypedIOResult<'T> = Result<'T, ErrorMessage>


    /// (PLC 등의) 메모리 타입.  int 환산 enumeration 값은 bit length 기준
    type MemoryType =
        | Undefined = 0
        | Bit = 1
        | Byte = 8
        | Word = 16
        | DWord = 32
        | LWord = 64
        | String = 1000

    /// MW100 : name='M', type='W', offset=100.  (MX30, MD1234, ML1234, ..)
    type AddressSpec(fileSpec: IOFileSpec, memoryType: MemoryType, offset: int) =
        member val IOFileSpec = fileSpec
        member val MemoryType = memoryType
        member val Offset = offset


    (*
  "Vendors": [
    {
      "Name": "LsXGI",
      "Location": "/tmp/iomaps",
      "Dll": "Z:\\ds\\DsDotNet\\src\\IOHub\\ThirdParty.AddressInfo.Provider\\bin\\Debug\\net8.0\\ThirdParty.AddressInfo.Provider.dll",
      "ClassName": "ThirdParty.AddressInfo.Provider.AddressInfoProviderLsXGI",
      "Files": [
        {
          "Name": "I",
          "Length": 65535
        },
        ...

    *)

    [<AllowNullLiteral>]
    type IStreamManager = interface end

    type IOFileSpec() =
        member val Name: MemoryName = "" with get, set
        member val Length = 0 with get, set

        member val IsStringStorage = false with get, set
        member val ConnectionString = "" with get, set

        // reference to parent
        [<JsonIgnore>]
        member val Vendor: VendorSpec = null with get, set

        [<JsonIgnore>]
        member val FileStream: FileStream = null with get, set

        [<JsonIgnore>]
        member val StreamManager: IStreamManager = null with get, set

    // Activator.CreateInstanceFrom(v.Dll, v.ClassName) 를 이용
    [<AllowNullLiteral>]
    type VendorSpec() =
        member val Name      = "" with get, set
        member val Location  = "" with get, set
        member val Dll       = "" with get, set
        member val ClassName = "" with get, set
        member val Files:IOFileSpec[] = [||] with get, set

        [<JsonIgnore>]
        member val AddressResolver: IAddressInfoProvider = null with get, set

    [<AllowNullLiteral>]
    type IOSpec() =
        member val ServicePort = 0 with get, set
        member val TopLevelLocation = "" with get, set
        member val Vendors: VendorSpec[] = [||] with get, set

        static member FromJsonFile(jsonPath: string) =
            jsonPath
            |> File.ReadAllText
            |> JsonConvert.DeserializeObject<IOSpec>
            |> tee regulate

    [<AllowNullLiteral>]
    type IOSpecHW() =
        member val ServerIP = "" with get, set
        member val ServerPort = 0 with get, set
        member val HwIP = "" with get, set
        member val HwPort = 0 with get, set

        static member FromJsonFile(jsonPath: string) =
            jsonPath
            |> File.ReadAllText
            |> JsonConvert.DeserializeObject<IOSpecHW>

    let regulate (x: IOSpec) =
        let sep = Path.DirectorySeparatorChar
        let regulatePath (dir: string) = dir.Replace('\\', sep).ToLower()
        let regulateDir (dir: string) = (regulatePath dir).TrimEnd(sep)
        x.TopLevelLocation <- regulateDir x.TopLevelLocation


        for v in x.Vendors do
            v.Location <- regulateDir v.Location
            v.Dll <- regulateDir v.Dll
            //v.AddressResolver <-
            //    let dllPath = if File.Exists(v.Dll) then v.Dll else Path.Combine(AppContext.BaseDirectory, v.Dll)
            //    let oh: ObjectHandle = Activator.CreateInstanceFrom(dllPath, v.ClassName)
            //    let obj: obj = oh.Unwrap()
            //    obj :?> IAddressInfoProvider

            for f in v.Files do
                f.Vendor <- v
                f.Name <- f.Name.ToLower()

                if f.Length <= 0 && not f.IsStringStorage then
                    failwithlogf $"Invalid file length {f.Length} on file {f.Name}"


    let bitSizeToEnum (bitSize: int) =
        match bitSize with
        | 1 -> MemoryType.Bit
        | 8 -> MemoryType.Byte
        | 16 -> MemoryType.Word
        | 32 -> MemoryType.DWord
        | 64 -> MemoryType.LWord
        | 1000 -> MemoryType.String
        | _ -> failwithf ($"Invalid bit size: {bitSize}")

    type IOFileSpec with

        member x.GetPath() =
            match x.Vendor.Location with
            | "" -> x.Name
            | _ as l -> $"{l}/{x.Name}"

    type IOutgoingSocket with

        member x.SendFrameWithRequestId(id: int) =
            id |> ByteConverter.ToBytes |> x.SendFrame

        member x.SendMoreFrameWithRequestId(id: int) =
            id |> ByteConverter.ToBytes |> x.SendMoreFrame

        member x.SendFrameWithRequestIdAndEndian(id: int, isDifferentEndian: bool) =
            id
            |> ByteConverter.ToBytes
            |> reverseBytesOnDemand isDifferentEndian
            |> x.SendFrame

        member x.SendMoreFrameWithRequestIdAndEndian(id: int, isDifferentEndian: bool) =
            id
            |> ByteConverter.ToBytes
            |> reverseBytesOnDemand isDifferentEndian
            |> x.SendMoreFrame

    type NetMQFrame with

        /// NetMQFrame 의 Buffer 로부터 int 값을 읽어서 반환
        member x.GetInt32(reverse: bool) =
            (x.Buffer |> reverseBytesOnDemand reverse, 0) |> BitConverter.ToInt32

        /// NetMQFrame 의 Buffer 로부터 type 'T 의 값들을 읽어서 array 로 반환
        member x.GetArray<'T>(reverse: bool) =
            ByteConverter.BytesToTypeArray<'T>(x.Buffer, reverse)


type IClientRequestInfo = interface end

/// string, bit, byte, word, dword, lword type 의 I/O 값 변경에 관한 base 정보
type IMemoryChangeInfo =
    abstract member ClientRequestInfo: IClientRequestInfo
    abstract member IOFileSpec: IOFileSpec
    abstract member Value: obj


type IIOChangeInfo = interface end

/// string 을 제외한 bit, byte, word, dword, lword type 의 I/O 값 변경 정보
type INumericIOChangeInfo =
    inherit IMemoryChangeInfo
    inherit IIOChangeInfo
    abstract member Offsets: int[]
    abstract member MemoryType: MemoryType

type IStringIOChangeInfo =
    inherit IMemoryChangeInfo
    inherit IIOChangeInfo
    abstract member Keys: string[]
