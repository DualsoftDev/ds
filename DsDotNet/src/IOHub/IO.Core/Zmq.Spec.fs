namespace IO.Core
open System
open System.Diagnostics.CodeAnalysis
open System.IO
open Dual.Common.Core.FS

[<AutoOpen>]
module ZmqSpec =
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




    /// MW100 : name='M', type='W', offset=100.  (MX30, MD1234, ML1234, ..)
    type AddressSpec(name:string, typ:string, offset:int) =
        member val Name = name.ToLower() with get, set
        member val Offset = offset with get, set
        member val Type = typ.ToLower() with get, set


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


    type IOFileSpec() =
        member val Name = ""  with get, set
        member val Length = 0 with get, set
        member val Vendor:VendorSpec = null with get, set
    
    and [<AllowNullLiteral>] VendorSpec() =
        member val Name = "" with get, set
        member val Location = "" with get, set
        member val Dll = "" with get, set
        member val ClassName = "" with get, set
        member val Accepts = "" with get, set
        member val Files:IOFileSpec[] = [||] with get, set
    type IOSpec() =
        member val ServicePort = 0 with get, set
        member val TopLevelLocation = "" with get, set
        member val Vendors:VendorSpec[] = [||] with get, set

    let private sep = Path.DirectorySeparatorChar
    let regulatePath (dir:string) = dir.Replace('\\', sep).ToLower()
    let regulateDir (dir:string) = (regulatePath dir).TrimEnd(sep)

    type IOSpec with
        member x.Regulate() =
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

