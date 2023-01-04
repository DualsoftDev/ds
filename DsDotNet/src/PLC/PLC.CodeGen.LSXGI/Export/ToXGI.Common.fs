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
    let coord x y : EncodedXYCoordinate = x*3 + y*1024 + 1
    let rungXy coord =
        let y = (coord - 1) / 1024
        let xx = ((coord - 1) % 1024)
        let x = xx / 3
        let r = xx % 3
        x, y, r

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
    let mutiEndLine startX endX y =
        if endX > startX then
            let lengthParam = sprintf "Param=\"%d\"" (3 * (endX-startX))
            let c = coord startX y
            elementFull (int ElementType.MultiHorzLineMode) c lengthParam ""
        else
            failwithlogf "endX startX [%d > %d]" endX startX

    /// 함수 그리기
    let createFB funcFind func (inst:string) tag x y : CoordinatedRungXml =
        let instFB = if(inst <> "") then (inst + ",VAR") else ","
        let c = coord x y
        let fbBody = sprintf "Param=\"%s\"" (FB.getFBXML( funcFind, func, instFB, FB.getFBIndex tag))
        let xml = elementFull (int ElementType.VertFBMode) c fbBody inst
        { Coordinate = c; Xml = xml }

    /// 함수 파라메터 그리기
    let createFBParameterXml tag x y=
        let c = coord x y
        let xml = elementFull (int ElementType.VariableMode) c "" tag
        { Coordinate = c; Xml = xml }

    let drawRising (x, y) =
        let cellX = getFBCellX x
        let c = coord (cellX) y
        [   { Coordinate = c; Xml = risingline c}
            { Coordinate = c; Xml = mutiEndLine x (cellX-1) y}
        ]

    /// x y 위치에서 수직으로 n 개의 line 을 긋는다
    let vlineDownTo x y n =
        [
            if x >= 0 then
                let c = coord x y
                yield { Coordinate = c; Xml = $"<!-- vlineDownTo {x} {y} {n} -->" }
                for n in [0.. n-1] do
                    let c = 2 + coord x (y + n)
                    yield { Coordinate = c; Xml = vline c }
        ]

    let drawPulseCoil (x, y, tagCoil:IExpressionTerminal, funSize:int) =
        let newX = getFBCellX (x-1)
        let newY = y + funSize
        [
            { Coordinate = coord x y; Xml = risingline (coord x y)}
            { Coordinate = coord newX newY; Xml = mutiEndLine (x) (newX - 1) newY}
            { Coordinate = coord coilCellX newY; Xml = elementBody (int ElementType.CoilMode) (coord coilCellX newY) (tagCoil.PLCTagName)}
            yield! vlineDownTo (x-1) y funSize
        ]


