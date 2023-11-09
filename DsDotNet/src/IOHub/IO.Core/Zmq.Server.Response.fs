(* IO.Core using Zero MQ *)

namespace IO.Core
open Dual.Common.Core.FS



[<AutoOpen>]
module internal ZmqServerResponseImplModule =
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

    type WriteResponseOK(cri:ClientRequestInfo, memoryChangeInfo:IMemoryChangeInfo) =
        inherit Response(cri)
        interface IResponseOK
        member x.MemoryChangeInfo = memoryChangeInfo

    type WriteHeterogeniousResponseOK(cri:ClientRequestInfo, spotChanges:IMemoryChangeInfo seq) =
        inherit Response(cri)
        interface IResponseOK
        member val SpotChanges = spotChanges |> toArray


    type ReadResponseOK(cri:ClientRequestInfo, memoryType:MemoryType, values:obj) =
        inherit Response(cri)
        interface IResponseOK
        member x.Values = values
        member x.MemoryType = memoryType



