namespace Dual.Common.Base.FS

open System

// Basic interfaces

/// Name 속성을 갖는, 즉 이름을 갖는 interface
type INamed  =
    abstract Name: string with get, set

type IGuid  =
    abstract Guid: Guid with get, set

/// 문자열로 상호 변환 가능한 class 의 interface
type IParsable<'T> =
    abstract TryParse: string -> 'T option
    abstract Stringify: unit -> string