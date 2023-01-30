namespace Old.Dual.Core

open System.Collections.Generic
open FSharpPlus
open Old.Dual.Core.Types
open Old.Dual.Core
open Old.Dual.Common
open Old.Dual.Core.QGraph
open Old.Dual.Core.ModelPostProcessor
open FSharpPlus
open Old.Dual.Common
open Old.Dual.Core.QGraph
open Old.Dual.Core.Types
open System.Collections.Generic
open Old.Dual.Core.QGraph
open Old.Dual.Core
open System.Runtime.CompilerServices
open Old.Dual.Core.Types.Command

/// (Unit) test 모듈에서만 사용됨
module TestFramework =
    let processModelFullblown model replaces = processModelWithOption (Some <| createFullBlownCodeGenerationOption()) replaces model

    let processModelWithDefault model replaces = processModelWithOption None replaces model

module SequenceValidator =
    /// Boolean value 를 저장하기 위한 tag
    type BitTag(tag:string) =
        member val Value = false with get, set
        member x.ToText() = tag
        /// 몇번째 scan 에서 tag 값이 변경되었는지 기록
        member val ScanCounter = -1 with get, set
        interface IExpressionTerminal with
            member x.ToText() = x.ToText()
            member x.Equals t = t :? BitTag && t.ToText() = x.ToText()

    /// Oneshot rising/falling 을 detect 하기 위한 tag
    type PulseTag(tag:BitTag) =
        inherit BitTag(tag.ToText())

    let toBitTag tagName = Terminal(BitTag(tagName))

    /// Expression tree 를 BitTag tree 로 변환
    let toBitTagExpression (dic:Dictionary<string, BitTag>) (exp:Expression) =
        let transformer (terminal:IExpressionTerminal) =
            let bitTag = dic.[terminal.ToText()]
            match terminal with
            | :? PulseTerminal ->
                PulseTag(bitTag) :> IExpressionTerminal
            | _ ->
                bitTag :> IExpressionTerminal

        Expression.transformTerminal transformer exp

    /// tag 와 boolean 값
    type BitValue = string * bool

    /// 모든 bit tag 를 한번 scan 했을 때의 값들
    type SingleScanHistory = BitValue list

    /// 여러번 scan 한 history
    type FullScanHistory = (int * SingleScanHistory) list

    /// history 로부터 주어진 state 의 sequence 를 추출.  e.g "_G" 인 state 만 추출한다던지..
    let extractMatchingSequence (stateNamefilter:string -> bool) state (history:FullScanHistory) =
        history |> bind snd |> filter (fun (name:string, _) -> stateNamefilter name)

    // TODO
    // XGI PLC 생성이 dll 로 분리되어, 현재의 project 내에서 참조가 불가능함.
    //open TestFramework
    //// XG5000 XML statements 생성
    //let generateRungStatementsHelper .... =
    //let getTagValueChangedSequence (rootVertices:seq<IVertex>) (edges:seq<IEdge>) (segments:ISegment seq)  =

