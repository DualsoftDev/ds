namespace Dual.Core.Types

open Dual.Common
open ActivePattern
open Microsoft.FSharp.Collections
open Dual.Core.Prelude
open Dual.Core.Prelude.IEC61131
open System.Diagnostics
open System

(*
 * Type hierarchy
    type PUnit =
        | Reference of Condition
		    type Expression = 
			    | Terminal  of identifier
			    | Binary of Expression * Op * Condition
			    | Unary of Op * Expression
			    type Op =
				    | And | Or | Neg | OpUnit
			
        | Action of PAction
		    type PAction =
			    | OnOffAction of OnOff
				    type OnOff =
					    | TurnOn of Tag
					    | TurnOff of Tag
			    | ParallelActions of PAction list
			    | PLCAction of PLCCommand * PLCParams
 *)
[<AutoOpen>]
module internal TypePrelude =
    let empty = Seq.empty

type Tag = identifier
type TagType =
    /// 출력 coil 에 해당하는 tag - Q
    | Action = 0
    /// 입력 (sensor state)에 해당하는 tag - I
    | State = 1
    /// 내부 (Memory state)에 해당하는 tag - M
    | Dummy = 2
    /// 내부 Functoin Block 상태 값을 가지는 임시 메모리 객체에 해당하는 tag
    /// 제어기기 환경에 따라 다름(XGI는 타이머 현재값 카운터 현재값 관리 내부 메모리
    | Instance = 3

type DeviceType =
    | Actuator = 0
    | Sensor = 1

type FullNameRule =
    | AREA_TYPE_DEVICE_TAG = 0 
    | NO_RULE = 1

[<RequireQualifiedAccess>]
type DeviceActionType = 
    | ADV = 0
    | RET = 1
    | ON  = 2
    | OFF = 3
    | UP = 4
    | DOWN = 5
    | CLAMP = 6
    | UNCLAMP = 7
    | None = 99

[<AbstractClass; Sealed>]
type AddressHelper() =
    static let storageManager = PLCStorageManager.CreateFullBlown()
    static member StorageManager with get() = storageManager


type Address =
    | I of string
    | Q of string
    | M of string
    with
        /// 문자열 주소로부터 Address type 생성
        static member FromString(address:string) =
            match address with
            | StartsWith "%IX" () -> I(address)
            | StartsWith "%QX" () -> Q(address)
            | _ -> M(address)
        /// PLC device address type 를 반환
        member x.GetAddrType() =
            match x with
            | I(_) -> "I"
            | Q(_) -> "Q"
            | M(_) -> "M"
        /// PLC device 의 address type + memory type 를 반환
        member x.GetAddrMemType() =
            let a = x.GetAddress()
            match x.GetAddress() with
            | ActivePattern.RegexPattern @"%([IQM])([XWBD]).*" [iqm; xw;] -> iqm + xw
            | _ -> failwithlog "Invalid address"


        member x.GetAddress() =
            match x with
            | I(addr) | Q(addr) | M(addr) -> addr

        /// 주소를 shift 한 결과를 반환
        member x.Shift offset =
            /// "IX", "QX", "IW", ... 등
            let prefix = x.GetAddrMemType()
            let sm = AddressHelper.StorageManager.[prefix] :?> PLCSubStorage3

            match x.GetAddress() with
            | ActivePattern.RegexPattern @"%([IQM])([XBWD])(\d+)$" [iom; xw; d1] ->
                let n2, n1, n0 = sm.GetComponentIndices( (int d1) + offset )
                sprintf "%%%s%d.%d.%d" prefix n2 n1 n0 |> Address.FromString

            | ActivePattern.RegexPattern @"%([IQM])([XBWD])(\d+)\.(\d+)$" [iom; xw; d1; d2] ->
                let flat = sm.GetFlatIndex(0, (int d1), (int d2))
                let n2, n1, n0 = sm.GetComponentIndices( flat + offset )
                sprintf "%%%s%d.%d.%d" prefix n2 n1 n0 |> Address.FromString

            | ActivePattern.RegexPattern @"%([IQM])([XBWD])(\d+)\.(\d+)?\.(\d+)?$" [iom; xw; d1; d2; d3] ->
                let flat = sm.GetFlatIndex((int d1), (int d2), (int d3))
                let n2, n1, n0 = sm.GetComponentIndices( flat + offset )
                sprintf "%%%s%d.%d.%d" prefix n2 n1 n0 |> Address.FromString
            | _ ->
                failwithlog "Invalid shift"

/// 신호 켜고 끄기
[<DebuggerDisplay("{ToText()}")>]
type OnOff =
    /// '+' output
    | TurnOn of IExpressionTerminal
    /// '-' output
    | TurnOff of IExpressionTerminal
    with
        member x.ToText() =
            match x with
            | TurnOn(n) -> n.ToText()
            | TurnOff(f) -> "-" + f.ToText()

        [<Obsolete("Use ToText() instead")>]
        override x.ToString() =
            logWarn "Use PUnit.ToText() instead:\r\n%s" Environment.StackTrace
            Debugger.Break()
            x.ToText()


/// PLC 명령어 : SET, RESET 등
type PLCCommand = string

type PLCParam =
    | PLCParamTag of ITag
    | PLCParamString of string
    with
        member x.ToText() =
            match x with
            | PLCParamTag(tag) -> tag.ToString()
            | PLCParamString(str) -> str
        member x.TryGetTag() =
            match x with
            | PLCParamTag(tag) -> Some tag
            | _ -> None

/// PLC 명령어에 종속된 복수개의 parameters
type PLCParams(parameters:PLCParam seq) =
    inherit ResizeArray<PLCParam>(parameters)
    new() = PLCParams(sempty)
    member x.ToText() =
        x |> Seq.map(fun p -> p.ToText()) |> String.concat(", ")
    member x.CollectTags() =
        x |> Seq.map(fun p -> p.TryGetTag()) |> Seq.choose id


/// Process (P) 상에서의 수행 action.  참조는 제외된 형태
[<DebuggerDisplay("{ToText()}")>]
type PAction =
    /// 단일 on/off action
    | OnOffAction of OnOff
    /// [] block 내에 포함된 action lists
    | ParallelActions of ResizeArray<PAction>
    /// "@RESET[I_S300_2ND_CLAMP1_RET]" --> PLCCommand = "RESET", PLCParams = "I_S300_2ND_CLAMP1_RET"
    | PLCAction of PLCCommand * PLCParams
    with
        member x.ToText() =
            match x with
            | OnOffAction(nf) -> sprintf "%s" (nf.ToText())
            | ParallelActions(pa) ->
                let inner =
                    pa |> Array.ofSeq
                       |> Array.map (fun a -> a.ToText())
                       |> String.concat "&"
                sprintf "[%s]" inner
            | PLCAction(cmd, param) ->
                sprintf "@%s[%s]" cmd (param.ToText())

        [<Obsolete("Use ToText() instead")>]
        override x.ToString() =
            logWarn "Use PUnit.ToText() instead:\r\n%s" Environment.StackTrace
            Debugger.Break()
            x.ToText()

// TODO : Gamma
#if false
/// P (process) 에 포함되는 요소 개별에 관한 type.  참조와 action 으로 구분
[<Obsolete("Use PUnit instead")>]
type PUnitObsolete =
    /// Process 에 포함되는 참조.  추후 소멸 예정
    | Reference of Expression
    | Action of PAction
    with
        member x.ToText() =
            match x with
            | Reference(ref) ->
                let refstr = ref.ToText()
                if refstr.StartsWith("(") && refstr.EndsWith(")") then
                    refstr
                else
                    sprintf "(%s)" refstr
            | Action(act) -> sprintf "%s" (act.ToText())

        [<Obsolete("Use ToText() instead")>]
        override x.ToString() =
            logWarn "Use PUnit.ToText() instead:\r\n%s" Environment.StackTrace
            Debugger.Break()
            x.ToText()
#endif


/// P (process) 에 포함되는 요소 개별에 관한 type.  참조와 action 으로 구분
[<DebuggerDisplay("{Condition.ToText()} => {Action.ToText()}")>]
type PUnit(action, condition, reference) =
    inherit NodeLeaf(None, [||], null)

    new(action, condition) = PUnit(action, condition, Expression.Zero)

    /// 순서 조건. this PUnit 이 발생하기 이전의 step 조건
    member val Condition:Expression = condition with get, set
    /// 참조.
    member val Reference:Expression = reference with get, set
    member val Action:PAction = action with get, set
    static member Create(action) = PUnit(action, Expression.Zero, Expression.Zero)

//type PUnitsContainer(punits:PUnit seq) =
//    inherit SetBasedCollectionT<PUnit>(punits)

