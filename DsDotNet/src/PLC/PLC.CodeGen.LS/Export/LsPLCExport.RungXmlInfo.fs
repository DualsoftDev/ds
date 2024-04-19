namespace PLC.CodeGen.LS

open Dual.Common.Core.FS
open PLC.CodeGen.LS.Config.POU.Program.LDRoutine
open FB
open Dual.Common.Core.FS.StateBuilderModule.State
open System.Runtime.CompilerServices

[<AutoOpen>]
module internal RungXmlInfoModule =
    /// XmlOutput = string
    type XmlOutput = string
    type EncodedXYCoordinate = int

    /// Rung 단위 생성을 위한 정보
    type RungXmlInfo =
        {
            /// Xgi 출력시 순서 결정하기 위한 coordinate.
            Coordinate: EncodedXYCoordinate // int
            /// Xml element 문자열
            Xml: XmlOutput // string
            SpanX: int
            SpanY: int
        }

    /// Rung 구성 요소의 일부 block 에 관한 정보
    type BlockXmlInfo =
        {
            /// Block 시작 좌상단 x 좌표
            X: int
            /// Block 시작 좌상단 y 좌표
            Y: int
            /// Block 이 사용하는 가로 span
            TotalSpanX: int
            /// Block 이 사용하는 세로 span
            TotalSpanY: int
            /// Block 을 구성하는 element 들의 xml 정보
            XmlElements: RungXmlInfo list
        }

    type BlockXmlInfo with
        member x.GetXml():string =
            x.XmlElements |> List.map (fun e -> e.Xml) |> String.concat "\r\n"

    /// Rung 을 생성하기 위한 정보
    ///
    /// - Xmls: 생성된 xml string 의 list
    type RungGenerationInfo =
        { Xmls: XmlOutput list // Rung 별 누적 xml.  역순으로 추가.  꺼낼 때 뒤집어야..
          NextRungY: int }

        member me.AddSingleLineXml(xml) = { Xmls = xml :: me.Xmls; NextRungY = me.NextRungY + 1 }

    type XmlSnippet =
    | DuRungXmlInfo of RungXmlInfo
    | DuBlockXmlInfo of BlockXmlInfo
    | DuRungGenerationInfo of RungGenerationInfo



[<Extension>]
type internal RungXmlMerger =
    [<Extension>]
    static member MergeXmls(xmls:RungXmlInfo seq) = 
        xmls
        |> Seq.sortBy (fun ri -> ri.Coordinate) // fst
        |> Seq.map (fun ri -> ri.Xml) //snd
        |> String.concat "\r\n"
