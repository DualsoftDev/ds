namespace Dual.PLC.Common.FS

open System
open System.Collections.Generic

type DeviceInfo() =
    member val Device = "" with get, set
    member val LWordOffset = 0 with get, set
    member val LWordTag = "" with get, set

/// 범용 PLC 배치 구조 - 모든 PLC 전용 배치가 이 클래스를 상속받음
[<AbstractClass>]
type PlcBatchBase<'T when 'T :> PlcTagBase>(buffer: byte[], deviceInfos: DeviceInfo[], tags: 'T[]) =

    // 내부 상태
    let mutable tags = tags

    /// 버퍼: 실제 메모리 데이터 저장소
    member val Buffer = buffer with get, set

    /// 디바이스 정보들: 각 디바이스에 대한 메타데이터
    member val DeviceInfos = deviceInfos with get

    /// 현재 태그 배열
    member this.Tags = tags

    /// 태그 재지정
    member this.SetTags(newTags: 'T[]) =
        tags <- newTags

    /// 기본 LWordAddress 구현 (첫 번째 태그 기준)
    abstract member LWordAddress: string
    /// 기본 BatchToText 구현 (디바이스 및 최대 BitOffset 기준)
    abstract member BatchToText: unit -> string
