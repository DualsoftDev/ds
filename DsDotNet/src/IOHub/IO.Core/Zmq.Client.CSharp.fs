namespace IO.Core
open System
open IO.Core


// { C# 에서 소화하기 쉬운 형태로 변환.  C# Result<'T, string> 형태로..

[<AutoOpen>]
module internal ZmqCsClient =
    type CsResult<'T> = Dual.Common.Core.Result<'T, ErrorMessage>
    let toResultCs (fsResult:TypedIOResult<'T>) =
        match fsResult with
        | Ok r -> Dual.Common.Core.Result.Ok r
        | Error e -> Dual.Common.Core.Result.Err e

type CSharpClient(serverAddress:string) =
    inherit Client(serverAddress)
    member x.SendRequest(request:string) : CsResult<string>                   = toResultCs <| base.SendRequest(request)
    member x.ReadBits   (name:MemoryName, offsets:int[]) : CsResult<bool[]>   = toResultCs <| base.ReadBits(name, offsets)
    member x.ReadBytes  (name:MemoryName, offsets:int[]) : CsResult<byte[]>   = toResultCs <| base.ReadBytes(name, offsets)
    member x.ReadUInt16s(name:MemoryName, offsets:int[]) : CsResult<uint16[]> = toResultCs <| base.ReadUInt16s(name, offsets)
    member x.ReadUInt32s(name:MemoryName, offsets:int[]) : CsResult<uint32[]> = toResultCs <| base.ReadUInt32s(name, offsets)
    member x.ReadUInt64s(name:MemoryName, offsets:int[]) : CsResult<uint64[]> = toResultCs <| base.ReadUInt64s(name, offsets)
    member x.ReadStrings(name:MemoryName, keys:string[]) : CsResult<StringKeyValue[]> = toResultCs <| base.ReadStrings(name, keys)
    member x.ReadAllStrings(name:MemoryName)             : CsResult<StringKeyValue[]> = toResultCs <| base.ReadAllStrings(name)


open System.Runtime.CompilerServices
// C# 에서 소화하기 쉬운 형태로 변환
[<Extension>]
type ZmqClientExt =
    [<Extension>] static member CsGetTagNameAndValues(change:IIOChangeInfo) = change.GetTagNameAndValues()

