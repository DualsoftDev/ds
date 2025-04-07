namespace Dual.PLC.Common.FS

open System

/// 범용 PLC 배치 구조 - 모든 PLC 전용 배치가 이 클래스를 상속받음
[<AbstractClass>]
type PlcBatchBase<'T when 'T :> PlcTagBase>(buffer: byte[], initialTags: 'T[]) =

    // 내부 상태 (mutable, copy-on-write)
    let mutable tags = Array.copy initialTags

    /// 버퍼: 실제 메모리 데이터 저장소
    member val Buffer = buffer with get, set

    /// 현재 태그 배열 (copy하여 반환)
    member this.Tags 
        with get() = Array.copy tags

    /// 태그 재지정 (copy-from)
    member this.SetTags(newTags: 'T[]) =
        tags <- Array.copy newTags

    /// Batch 주소 - 일반적으로 첫 태그 기준으로 구함
    abstract member BatchAddress: string

    /// 디버깅용 텍스트 - 디바이스별 최대 BitOffset 정보 요약
    abstract member BatchToText: unit -> string
