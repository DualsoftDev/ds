namespace PLC.CodeGen.LS

open Dual.Common.Core.FS
open PLC.CodeGen.LS.Config.POU.Program.LDRoutine
open FB
open Dual.Common.Core.FS.StateBuilderModule.State

[<AutoOpen>]
module internal Common =
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

    /// 좌표 반환 : 1, 4, 7, 11, ...
    /// 논리 좌표 x y 를 LS 산전 XGI 수치 좌표계로 반환
    let coord (x, y) : EncodedXYCoordinate = x * 3 + y * 1024 + 1

    /// coord(x, y) 에서 x, y 좌표 반환
    let xyOfCoord coord =
        let y = (coord - 1) / 1024
        let xx = ((coord - 1) % 1024)
        let x = xx / 3
        let r = xx % 3
        (x, y), r

    /// coord(x, y) 에서 x 좌표 반환
    let xOfCoord : (EncodedXYCoordinate -> int) = xyOfCoord >> fst >> fst
    /// coord(x, y) 에서 y 좌표 반환
    let yOfCoord : (EncodedXYCoordinate -> int) = xyOfCoord >> fst >> snd

    let rungXmlInfosToBlockXmlInfo (rungXmlInfos: RungXmlInfo list) : BlockXmlInfo =
        let xs = rungXmlInfos
        let xys = xs |> List.map (fun e -> xyOfCoord e.Coordinate |> fst)
        let minX = xys |> List.map fst |> List.min
        let minY = xys |> List.map snd |> List.min
        let maxX = xs |> List.map (fun e -> xOfCoord e.Coordinate + e.SpanX) |> List.max
        let maxY = xs |> List.map (fun e -> yOfCoord e.Coordinate + e.SpanY) |> List.max

        let totalSpanX = maxX - minX
        let totalSpanY = maxY - minY

        { X = minX
          Y = minY
          TotalSpanX = totalSpanX
          TotalSpanY = totalSpanY
          XmlElements = xs }

    /// Rung 을 생성하기 위한 정보
    ///
    /// - Xmls: 생성된 xml string 의 list
    type RungGenerationInfo =
        { Xmls: XmlOutput list // Rung 별 누적 xml.  역순으로 추가.  꺼낼 때 뒤집어야..
          Y: int }

        member me.Add(xml) = { Xmls = xml :: me.Xmls; Y = me.Y + 1 }

    let dq = "\""



    /// 산전 limit : contact 기준 가로로 최대 31개[0..30] + coil 1개[31]
    let coilCellX = 31
    let maxNumHorizontalContact = 31
    /// 최소기본 FB 위치 : 가로로  9 포인트
    let minFBCellX = 9

    /// rung 을 구성하는 element (접점)의 XML 표현 문자열 반환
    let elementFull (elementType: int) (coordi: int) (param: string) (tag: string) : XmlOutput =
        $"\t\t<Element ElementType={dq}{elementType}{dq} Coordinate={dq}{coordi}{dq} {param}>{tag}</Element>"

    /// rung 을 구성하는 element (접점)의 XML 표현 문자열 반환
    let elementBody elementType coordi tag = elementFull elementType coordi "" tag
    /// rung 을 구성하는 element (접점)의 XML 표현 문자열 반환
    let element elementType coordi = elementBody elementType coordi ""

    /// 좌표 c 에서 시작하는 수직 line
    let vline c =
        element (int ElementType.VertLineMode) c

    let pointAt (elementType: ElementType) (tag: string) (x: int) (y: int) : XmlOutput =
        let xx = x * 3 + 1
        let yy = y * 1024
        let coordi = xx + yy
        assert (coordi = coord (x, y))

        let nElementType = int elementType

        (* see elementFull
            /// rung 을 구성하는 element (접점)의 XML 표현 문자열 반환
            let elementFull elementType coordi param tag : XmlOutput =
                $"\t\t<Element ElementType={dq}{elementType}{dq} Coordinate={dq}{coordi}{dq} {param}>{tag}</Element>"
        *)

        elementFull nElementType coordi "" tag

    /// x, y 위치에 contact 생성하기 위한 xml 문자열 반환
    let contactAt (tag: string) (x: int) (y: int) = pointAt ElementType.ContactMode tag x y

    /// y line 에 coil 생성하기 위한 xml 문자열 반환
    let coilAt (tag: string) (y: int) =
        pointAt ElementType.CoilMode tag coilCellX y // coilCellX = 31

    module Unused =
        /// 조건이 9 이상이면 뒤로 증가
        let getFBCellX x : int =
            if minFBCellX <= x + 3 then (x + 4) else minFBCellX

        /// 좌표 c 에서 시작하는 양 방향 검출 line
        let risingline c =
            elementFull (int ElementType.RisingContact) (c) "" ""

        /// 좌표 c 에서 시작하는 음 방향 검출 line
        let fallingline c =
            elementFull (int ElementType.FallingContact) (c) "" ""

        let hlineEmpty c =
            element (int ElementType.HorzLineMode) c

        let hline c =
            element (int ElementType.MultiHorzLineMode) c


    let hlineStartMarkAt (x, y) =
        elementFull (int ElementType.HorzLineMode) (coord (x, y)) "" ""

    /// debugging 용 xml comment 생성
    let xmlCommentAtCoordinate (c: EncodedXYCoordinate) (comment: string) =
        { Coordinate = c
          Xml = $"<!-- {comment} -->"
          SpanX = maxNumHorizontalContact
          SpanY = 1 }

    /// debugging 용 xml comment 생성
    let xmlCommentAt (x, y) comment =
        xmlCommentAtCoordinate (coord (x, y)) comment


    /// 마지막 수평으로 연결 정보: 그릴 수 없으면 [], 그릴 수 있으면 [singleton]
    let tryHlineTo (x, y) endX =
        if endX < x then
            []
        else
            let lengthParam = $"Param={dq}{3 * (endX - x)}{dq}"
            let c = coord (x, y)
            [ elementFull (int ElementType.MultiHorzLineMode) c lengthParam "" ]

    let hlineTo (x, y) endX =
        if endX < x then
            failwithlog $"endX startX [{endX} > {x}]"

        tryHlineTo (x, y) endX |> List.exactlyOne


    /// x y 위치에서 수직선 한개를 긋는다
    let vlineAt (x, y) : RungXmlInfo =
        verify (x >= 0)
        let c = coord (x, y) + 2

        { Coordinate = c
          Xml = vline c
          SpanX = 0
          SpanY = 1 }

    let mutable EnableXmlComment = false

    /// x y 위치에서 수직으로 n 개의 line 을 긋는다
    let vlineDownN (x, y) n : RungXmlInfo list =
        [ if EnableXmlComment then
              xmlCommentAt (x, y) $"vlineDownN ({x}, {y}) {n}"

          if n > 0 then
              for i in [ 0 .. n - 1 ] do
                  vlineAt (x, y + i) ]

    let vlineUpN (x, y) n = vlineDownN (x, y - n) n

    /// x y 위치에서 수직으로 endY 까지 line 을 긋는다
    let vlineDownTo (x, y) endY = vlineDownN (x, y) (endY - y)
    let vlineUpTo (x, y) endY = vlineUpN (x, y) (y - endY)

    /// xml 문자열을 <Rung> 으로 감싸기
    let wrapWithRung xml = $"\t<Rung BlockMask={dq}0{dq}>\r\n{xml}\t</Rung>"

    /// 함수 그리기 (detailedFunctionName = 'ADD2_INT', briefFunctionName = 'ADD')
    let createFunctionXmlAt (detailedFunctionName, briefFunctionName) (inst: string) (x, y) : RungXmlInfo =
        let tag = briefFunctionName
        let instFB = if inst = "" then "," else (inst + ",VAR")
        let c = coord (x, y)

        let param =
            FB.getFBXmlParam (detailedFunctionName, briefFunctionName) instFB (FB.getFBIndex tag)

        let fbBody = $"Param={dq}{param}{dq}"
        let xml = elementFull (int ElementType.VertFBMode) c fbBody inst

        { Coordinate = c
          Xml = xml
          SpanX = 3
          SpanY = getFunctionHeight detailedFunctionName }

    /// 함수 파라메터 그리기
    let createFBParameterXml (x, y) tag : RungXmlInfo =
        let c = coord (x, y)
        let xml = elementFull (int ElementType.VariableMode) c "" tag

        { Coordinate = c
          Xml = xml
          SpanX = 1
          SpanY = 1 }

//let drawRising (x, y) =
//    let cellX = getFBCellX x
//    let c = coord (cellX, y)
//    [   { Coordinate = c; Xml = risingline c; SpanX = 0; SpanY = 0 }
//        { Coordinate = c; Xml = hLineTo (x, y) (cellX-1); SpanX = (cellX-1); SpanY = 0 }
//    ]

//let drawPulseCoil (x, y) (tagCoil:INamedExpressionizableTerminal) (funSize:int) =
//    let newX = getFBCellX (x-1)
//    let newY = y + funSize
//    [
//        { Coordinate = coord(x, y); Xml = risingline (coord(x, y)); SpanX = 0; SpanY = 1 }
//        let xml = hLineTo (x, newY) (newX - 1)
//        { Coordinate = coord(newX, newY); Xml = xml; SpanX = (newX - 1); SpanY = 1 }

//        let xml = elementBody (int ElementType.CoilMode) (coord(coilCellX, newY)) (tagCoil.StorageName)
//        { Coordinate = coord(coilCellX, newY); Xml = xml; SpanX = 0; SpanY = 0 }
//        yield! vlineDownTo (x-1, y) funSize
//    ]
