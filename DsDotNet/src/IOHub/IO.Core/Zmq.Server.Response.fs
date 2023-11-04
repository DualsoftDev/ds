(* IO.Core using Zero MQ *)

namespace IO.Core
open Dual.Common.Core.FS



[<AutoOpen>]
module internal ZmqServerResponseImplModule =
    /// bit, byte, word, dword, qword 의 offset.
    /// - bit offset 은 byteOffset * 8 + bitOffset 의 값
    /// - word offset 은 byteOffset / 2 의 값
    type Offset = int
    type ValuesChangeInfo = IOFileSpec * MemoryType * Offset array * obj   // dataType, offset, value
    type SingleValueChange = ValuesChangeInfo

    /// 서버에서 socket message 를 처리후, client 에게 socket 으로 보내기 전에 갖는 result type
    type IResponse = interface end
    type IResponseOK = inherit IResponse
    /// Request 문법 오류
    type IResponseNG = inherit IResponse
    type IResponseNoMoreInput = inherit IResponseOK
    type IResponseWithClientRquestInfo = 
        inherit IResponse
        abstract ClientRequestInfo : ClientRequestInfo
    [<AbstractClass>]
    type Response(cri:ClientRequestInfo) =
        interface IResponseWithClientRquestInfo with
            member x.ClientRequestInfo = cri
        member x.ClientRequestInfo = cri

    [<AbstractClass>]
    type StringResponse(cri:ClientRequestInfo, message:string) =
        inherit Response(cri)
        member x.Message = message

    type ResponseNoMoreInput() =
        interface IResponseNoMoreInput
    type ResponseOK() =
        interface IResponseOK

    type StringResponseOK(cri:ClientRequestInfo, message:string) =
        inherit StringResponse(cri, message)
        interface IResponseOK
    type StringResponseNG(cri:ClientRequestInfo, message:string) =
        inherit StringResponse(cri, message)
        interface IResponseNG

    type WriteResponseOK(cri:ClientRequestInfo, valuesChangeInfo:ValuesChangeInfo) =
        inherit Response(cri)
        let ioFIleSpec, contentBitSize, offsets, changedValues = valuesChangeInfo
        interface IResponseOK
        member x.ChangedValues = changedValues
        member x.ContentBitSize = contentBitSize
        member x.Offsets = offsets
        member x.FIleSpec = ioFIleSpec

    type WriteHeterogeniousResponseOK(cri:ClientRequestInfo, spotChanges:SingleValueChange seq) =
        inherit Response(cri)
        interface IResponseOK
        member val SpotChanges = spotChanges |> toArray


    type ReadResponseOK(cri:ClientRequestInfo, memoryType:MemoryType, values:obj) =
        inherit Response(cri)
        interface IResponseOK
        member x.Values = values
        member x.MemoryType = memoryType



