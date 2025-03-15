namespace Dsu.PLCConverter.FS

open System.Diagnostics
open System.Runtime.CompilerServices
open System.Collections.Generic
open System

/// Contact 관련 기본 연산자 정의
type Op =
    | Load | And | Or 
    with
        /// 연산자를 텍스트로 변환
        member x.ToText =
            match x with
            | Load -> "LOAD"
            | And -> "AND"
            | Or -> "OR"

        /// 문자열을 통해 연산자를 반환하는 함수
        static member getOp(op: string) =
            match op.ToUpper() with
            | "LOAD" -> Load
            | "AND" -> And
            | "OR" -> Or
            | _ -> failwith (sprintf "Invalid op [%s] (Op = {Load, And, Or})" op)

/// 비교 연산자를 정의
type OpComp =
    | NotComp | GT | GE | EQ | LE | LT | NE 
    with
        /// 비교 연산자를 텍스트로 변환
        member x.ToText =
            match x with
            | NotComp -> "NotComp"
            | GT -> "GT"
            | GE -> "GE"
            | EQ -> "EQ"
            | LE -> "LE"
            | LT -> "LT"
            | NE -> "NE"

/// 비교 연산자의 데이터 타입 정의
type OpCompType =
    | NoType | INT | DINT | REAL | STRING 
    with
        /// 데이터 타입을 텍스트로 변환
        member x.ToText =
            match x with
            | NoType -> "NoType"
            | INT -> "INT"
            | DINT -> "DINT"
            | REAL -> "REAL"
            | STRING -> "STRING"

        /// 문자열을 통해 데이터 타입을 반환하는 함수
        static member getOp(op: string) =
            match op.ToUpper() with
            | "NOTYPE" -> NoType
            | "INT" -> INT
            | "DINT" -> DINT
            | "REAL" -> REAL
            | "STRING" -> STRING
            | _ -> failwith (sprintf "Invalid op [%s] (OpCompType = {NoType, INT, DINT, REAL, STRING})" op)

/// Load 연산자를 정의
type OpLoad =
    | Base | AndLoad | OrLoad
    with
        /// Load 연산자를 텍스트로 변환
        member x.ToText =
            match x with
            | Base -> "Base"
            | AndLoad -> "AndLoad"
            | OrLoad -> "OrLoad"

/// 연산자의 옵션을 정의
type OpOpt =
    | Normal | Rising | Falling | NegRising | NegFalling | Neg | Inverter | Compare | RisingEdge | FallingEdge
    with
        /// 옵션을 텍스트로 변환
        member x.ToText =
            match x with
            | Normal -> "Normal"
            | Rising -> "Rising"
            | Falling -> "Falling"
            | NegRising -> "NegRising"
            | NegFalling -> "NegFalling"
            | Neg -> "Neg"
            | Inverter -> "Inverter"
            | Compare -> "Compare"
            | RisingEdge -> "RisingEdge"
            | FallingEdge -> "FallingEdge"

/// 인스턴스 옵션을 정의
type InstOpt =
    | R_TRIG | F_TRIG | FF 
    with
        /// 인스턴스 옵션을 텍스트로 변환
        member x.ToText =
            match x with
            | R_TRIG -> "R_TRIG"
            | F_TRIG -> "F_TRIG"
            | FF -> "FF"

/// 인스턴스 타입과 이름을 나타내는 튜플 타입
type Inst = InstOpt * string   // InstType * InstName
                     
/// 인스턴스 함수
type InstFun =
    /// 특정 인스턴스를 찾고 포맷된 문자열로 반환
    static member getInst(insts: seq<Inst>, inst: InstOpt) =
        sprintf "%s%d" inst.ToText (insts |> Seq.filter (fun (f, _) -> inst = f) |> Seq.length)

/// Contact의 정의: 문자열 리스트, 연산자, 옵션, 비교 연산자, 비교 타입을 포함
type contact = string list * Op * OpOpt * OpComp * OpCompType

/// `LoadPoint` 타입: 좌표 기반의 시작 및 종료점을 포함하여 프로그램의 특정 위치를 정의
[<DebuggerDisplay("Start({SX},{SY}) -> End({EX},{EY})  Size({SizeX},{SizeY})")>]
type LoadPoint(sx: int, sy: int, ex: int, ey: int) = 
    member val SX = sx with get, set
    member val SY = sy with get, set
    member val EX = ex with get, set
    member val EY = ey with get, set

    /// `Copy`: 현재 `LoadPoint`의 복사본 생성
    member x.Copy() = LoadPoint(x.SX, x.SY, x.EX, x.EY)

    /// `Update`: 새로운 `LoadPoint`로 위치 업데이트
    member x.Update(newPt: LoadPoint) =
        x.EX <- newPt.EX
        x.EY <- newPt.EY
        x.SX <- newPt.SX
        x.SY <- newPt.SY

    /// `IsSame`: 두 `LoadPoint`가 동일한 위치인지 비교
    member x.IsSame(newPt: LoadPoint) = 
        x.EX = newPt.EX && x.EY = newPt.EY && x.SX = newPt.SX && x.SY = newPt.SY

    /// `Shift`: 특정 값만큼 좌표를 이동
    member x.Shift(nx: int, ny: int) =
        x.EX <- x.EX + nx
        x.EY <- x.EY + ny
        x.SX <- x.SX + nx
        x.SY <- x.SY + ny

    /// `SizeX`: X축에서의 크기 계산
    member x.SizeX = x.EX - x.SX

    /// `SizeY`: Y축에서의 크기 계산
    member x.SizeY = x.EY - x.SY

    /// `SizeZero`: 사이즈가 0인지 확인
    member x.SizeZero = x.SizeX = 0 && x.SizeY = 0

/// 프로그램을 저장하는 기본 구조 타입
type LoadBlock = 
    /// `LoadBase`: 기본 Load 구조를 나타냄
    | LoadBase of List<contact> * LoadPoint

    /// `Extend`: 확장된 Load 표현을 나타냄 (기준점과 확장점을 포함)
    | Extend of List<contact> * LoadPoint * LoadPoint

    /// `Mix`: 두 LoadBlock을 특정 연산자로 조합하여 표현
    | Mix of LoadBlock * OpLoad * LoadBlock * LoadBlock

    /// `Empty`: 비어있는 상태를 나타냄
    | Empty
    with
        /// `ToText`: LoadBlock 타입을 텍스트로 변환
        member x.ToText =
            match x with
            | LoadBase (_, _) -> "LoadBase"
            | Extend (_, _, _) -> "Extend"
            | Mix (_, _, _, _) -> "Mix"
            | Empty -> "Empty"

        /// `Copy`: LoadBlock의 복사본 생성
        member x.Copy() =
            match x with
            | LoadBase (a, b) -> 
                let newListContact = List()
                newListContact.AddRange(a)
                LoadBase(newListContact, b.Copy())
            | Extend (a, b, c) ->
                let newListContact = List()
                newListContact.AddRange(a)
                Extend(newListContact, b.Copy(), c.Copy())
            | Mix (a, b, c, d) -> Mix(a.Copy(), b, c.Copy(), d.Copy())
            | Empty -> Empty
