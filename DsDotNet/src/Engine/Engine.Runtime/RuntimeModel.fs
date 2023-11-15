namespace Engine.Runtime
open System
open IO.Core

type FilePath = string

type RuntimeModel(zipDsPath:FilePath) =
    do
        // todo: 컴파일 모델 정보
        ()
    member x.SourceDsZipPath = zipDsPath


type IoHub(zmqSettingsJson:FilePath) =
    let zmqInfo = Zmq.InitializeServer zmqSettingsJson

    interface IDisposable with
        member x.Dispose() = x.Dispose()

    member x.Server = zmqInfo.Server
    member x.Spec   = zmqInfo.IOSpec
    //member x.IoHubInfo = zmqInfo

    member x.Dispose() = zmqInfo.Dispose()
