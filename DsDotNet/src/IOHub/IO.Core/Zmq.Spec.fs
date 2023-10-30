namespace IO.Core
open System
open System.IO
open Dual.Common.Core.FS
open IO.Spec
open Newtonsoft.Json

[<AutoOpen>]
module rec ZmqSpec =
    [<AllowNullLiteral>]
    type IIOResult = interface end
    type IIOResultOK = inherit IIOResult
    type IIOResultNG =
        inherit IIOResult
        abstract member Error:string

    [<AbstractClass>]
    type IOResult(error:string) =
        member x.Error = error
        member x.IsOK = String.IsNullOrEmpty(error)
        interface IIOResult

    [<AbstractClass>]
    type ReadResult(error:string) =
        inherit IOResult(error)

    type ReadResultArray<'T>(results:'T[]) =
        inherit ReadResult(null)
        interface IIOResultOK
        member val Results = results

    type ReadResultSingle<'T>(result:'T) =
        inherit ReadResult(null)
        interface IIOResultOK
        member val Result = result


    type ReadResultString(result:string) =
        inherit ReadResult(null)
        interface IIOResultOK
        member val Result = result

    type ReadResultError(error:string) =
        inherit ReadResult(error)
        interface IIOResultNG with
            member x.Error = error

    type WriteResultOK() =
        inherit IOResult(null)
        interface IIOResultOK

    type WriteResultError(error:string) =
        inherit IOResult(error)
        interface IIOResultNG with
            member x.Error = error


    type PLCMemoryBitSize =
        | Bit = 1
        | Byte = 8
        | Word = 16
        | DWord = 32
        | LWord = 64



    /// MW100 : name='M', type='W', offset=100.  (MX30, MD1234, ML1234, ..)
    type AddressSpec(fileSpec:IOFileSpec, dataType:PLCMemoryBitSize, offsetByte:int, offsetBit:int) =
        member val IOFileSpec = fileSpec
        member val DataType = dataType
        member val OffsetByte = offsetByte
        member val OffsetBit = offsetBit


    (*
  "Vendors": [
    {
      "Name": "LsXGI",
      "Location": "/tmp/iomaps",
      "Dll": "F:\\Git\\ds\\DsDotNet\\src\\IOHub\\ThirdParty.AddressInfo.Provider\\bin\\Debug\\net7.0\\ThirdParty.AddressInfo.Provider.dll",
      "ClassName": "ThirdParty.AddressInfo.Provider.AddressInfoProviderLsXGI",
      "Accepts": "%[IQM]*",
      "Files": [
        {
          "Name": "I",
          "Length": 65535
        },
        ...
    
    *)

    [<AllowNullLiteral>] 
    type IBufferManager = interface end
    and IOFileSpec() =
        member val Name = ""  with get, set
        member val Length = 0 with get, set

        // reference to parent
        member val Vendor:VendorSpec = null with get, set
        member val FileStream:FileStream = null with get, set
        member val BufferManager:IBufferManager = null with get, set
    
    // Activator.CreateInstanceFrom(v.Dll, v.ClassName) 를 이용
    [<AllowNullLiteral>] 
    type  VendorSpec() =
        member val Name = "" with get, set
        member val Location = "" with get, set
        member val Dll = "" with get, set
        member val ClassName = "" with get, set
        member val Accepts = "" with get, set
        member val Files:IOFileSpec[] = [||] with get, set
        member val AddressResolver:IAddressInfoProvider = null with get, set

    type IOSpec() =
        member val ServicePort = 0 with get, set
        member val TopLevelLocation = "" with get, set
        member val Vendors:VendorSpec[] = [||] with get, set
        static member FromJsonFile(jsonPath:string) =
            jsonPath
            |> File.ReadAllText
            |> JsonConvert.DeserializeObject<IOSpec>
            |> tee regulate


    let regulate (x:IOSpec) =
        let sep = Path.DirectorySeparatorChar
        let regulatePath (dir:string) = dir.Replace('\\', sep).ToLower()
        let regulateDir (dir:string) = (regulatePath dir).TrimEnd(sep)
        x.TopLevelLocation <- regulateDir x.TopLevelLocation
        for v in x.Vendors do
            v.Location <- regulateDir v.Location
            v.Dll <- regulateDir v.Dll
            v.Accepts <- v.Accepts.ToLower()
            for f in v.Files do
                f.Vendor <- v
                f.Name <- f.Name.ToLower()
                if f.Length <= 0 then
                    failwithlogf $"Invalid file length {f.Length} on file {f.Name}"


    let bitSizeToEnum(bitSize:int) =
        match bitSize with
        | 1 -> PLCMemoryBitSize.Bit
        | 8 -> PLCMemoryBitSize.Byte
        | 16 -> PLCMemoryBitSize.Word
        | 32 -> PLCMemoryBitSize.DWord
        | 64 -> PLCMemoryBitSize.LWord
        | _ -> failwithf($"Invalid bit size: {bitSize}")

    type IOFileSpec with
        member x.GetPath() =
            match x.Vendor.Location with
            | "" -> x.Name
            | _ as l -> $"{l}/{x.Name}"
