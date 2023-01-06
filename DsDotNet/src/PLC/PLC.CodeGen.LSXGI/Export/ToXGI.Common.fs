namespace PLC.CodeGen.LSXGI

open Engine.Common.FS
open Engine.Core
open PLC.CodeGen.LSXGI.Config.POU.Program.LDRoutine

[<AutoOpen>]
module internal Common =
    /// XmlOutput = string
    type XmlOutput = string
    type EncodedXYCoordinate = int
    type CoordinatedRungXml = {
        Coordinate: EncodedXYCoordinate   // int
        Xml: XmlOutput                  // string
    }

    type CoordinatedRungXmlsWithNewY = {
        SpanY: int
        PositionedRungXmls: CoordinatedRungXml list
    }

    type RungInfosWithSpan = {
        RungInfos:CoordinatedRungXml list
        X: int
        Y: int
        SpanX: int
        SpanY: int
    }

    type RungGenerationInfo = {
        Xmls: XmlOutput list   // Rung 별 누적 xml.  역순으로 추가.  꺼낼 때 뒤집어야..
        Y: int }
    with
        member me.Add(xml) = { Xmls = xml::me.Xmls; Y = me.Y + 1 }

    /// <!-- --> 구문의 xml comment 삽입 여부.  순수 debugging 용도
    let enableXmlComment = false
    let dq = "\""

    /// rung 을 구성하는 element (접점)의 XML 표현 문자열 반환
    let elementFull (elementType:int) coordi (param:string) (tag:string) : XmlOutput =
        $"\t\t<Element ElementType={dq}{elementType}{dq} Coordinate={dq}{coordi}{dq} {param}>{tag}</Element>"

    /// rung 을 구성하는 element (접점)의 XML 표현 문자열 반환
    let elementBody elementType coordi tag = elementFull elementType coordi "" tag
    /// rung 을 구성하는 element (접점)의 XML 표현 문자열 반환
    let element elementType coordi = elementBody elementType coordi ""
    let hlineEmpty c = element (int ElementType.HorzLineMode) c
    let hline c = element (int ElementType.MultiHorzLineMode) c
    /// 좌표 c 에서 시작하는 수직 line
    let vline c = element (int ElementType.VertLineMode) c
    /// 좌표 반환 : 1, 4, 7, 11, ...
    /// 논리 좌표 x y 를 LS 산전 XGI 수치 좌표계로 반환
    let coord (x, y) : EncodedXYCoordinate = x*3 + y*1024 + 1
    let rungXy coord =
        let y = (coord - 1) / 1024
        let xx = ((coord - 1) % 1024)
        let x = xx / 3
        let r = xx % 3
        (x, y), r

    /// 산전 limit : contact 기준 가로로 최대 31개[0..30] + coil 1개[31]
    let coilCellX = 31
    let maxNumHorizontalContact = 31
    /// 최소기본 FB 위치 : 가로로  9 포인트
    let minFBCellX = 9
    /// 조건이 9 이상이면 뒤로 증가
    let getFBCellX x:int = if minFBCellX <= x+3 then (x+4) else minFBCellX
    /// 좌표 c 에서 시작하는 양 방향 검출 line
    let risingline c = elementFull (int ElementType.RisingContact) (c) "" ""
    /// 좌표 c 에서 시작하는 음 방향 검출 line
    let fallingline c = elementFull (int ElementType.FallingContact) (c) "" ""



    /// 마지막 수평으로 연결 정보
    let hLineTo (x, y) endX =
        if endX <= x then
            failwithlog $"endX startX [{endX} > {x}]"

        let lengthParam = $"Param={dq}{3 * (endX-x)}{dq}"
        let c = coord(x, y)
        elementFull (int ElementType.MultiHorzLineMode) c lengthParam ""



    /// x y 위치에서 수직선 한개를 긋는다
    let vLineAt (x, y) =
        verify(x >= 0)
        let c = coord(x, y) + 2
        { Coordinate = c; Xml = vline c }

    /// x y 위치에서 수직으로 n 개의 line 을 긋는다
    let vlineDownTo (x, y) n =
        [
            if enableXmlComment then
                let c = coord(x, y)
                yield { Coordinate = c; Xml = $"<!-- vlineDownTo {x} {y} {n} -->" }

            for i in [0.. n-1] do
                yield vLineAt (x, y+i)
        ]


    /// 함수 그리기 (detailedFunctionName = 'ADD2_INT', briefFunctionName = 'ADD')
    let createFunctionXmlAt (detailedFunctionName, briefFunctionName) (inst:string) (x, y) : CoordinatedRungXml =
        let tag = briefFunctionName
        let instFB = if inst = "" then "," else (inst + ",VAR")
        let c = coord(x, y)
        let param = FB.getFBXmlParam (detailedFunctionName, briefFunctionName) instFB (FB.getFBIndex tag)
        let fbBody = $"Param={dq}{param}{dq}"
        let xml = elementFull (int ElementType.VertFBMode) c fbBody inst
        { Coordinate = c; Xml = xml }

    /// 함수 파라메터 그리기
    let createFBParameterXml (x, y) tag =
        let c = coord(x, y)
        let xml = elementFull (int ElementType.VariableMode) c "" tag
        { Coordinate = c; Xml = xml }

    let drawRising (x, y) =
        let cellX = getFBCellX x
        let c = coord (cellX, y)
        [   { Coordinate = c; Xml = risingline c}
            { Coordinate = c; Xml = hLineTo (x, y) (cellX-1)}
        ]

    let drawPulseCoil (x, y) (tagCoil:INamedExpressionizableTerminal) (funSize:int) =
        let newX = getFBCellX (x-1)
        let newY = y + funSize
        [
            { Coordinate = coord(x, y); Xml = risingline (coord(x, y))}
            { Coordinate = coord(newX, newY); Xml = hLineTo (x, newY) (newX - 1) }
            { Coordinate = coord(coilCellX, newY); Xml = elementBody (int ElementType.CoilMode) (coord(coilCellX, newY)) (tagCoil.StorageName)}
            yield! vlineDownTo (x-1, y) funSize
        ]


