namespace PLC.CodeGen.LSXGI

open System.Linq
open Engine.Common.FS
open PLC.CodeGen.LSXGI.Config.POU.Program.LDRoutine
open PLC.CodeGen.Common

[<AutoOpen>]
module internal Basic =
    let rec getDepthFirstLogical (expr: FlatExpression) =
        match expr with
        | FlatNary(op, FlatTerminal(_)::_) -> Some(op)
        | FlatNary(op, h::_) -> getDepthFirstLogical h
        | _ -> None


    /// Flat expression 을 논리 Cell 좌표계 x y 에서 시작하는 rung 를 작성한다.
    /// xml 및 다음 y 좌표 반환
    let rung (x, y) (expr:FlatExpression option) (cmdExp:XgiCommand) : CoordinatedRungXml =

        /// x y 위치에서 expression 표현하기 위한 정보 반환
        /// {| Xml=[|c, str|]; NextX=sx; NextY=maxY; VLineUpRightMaxY=maxY |}
        /// - Xml : 좌표 * 결과 xml 문자열
        let rec rng (x, y) (expr:FlatExpression) : RungInfosWithSpan =
            let baseRIWNP = { RungInfos = []; X=x; Y=y; SpanX=1; SpanY=1; }
            let c = coord(x, y)
            /// 좌표 * 결과 xml 문자열 보관 장소
            let rungInfos = ResizeArray<CoordinatedRungXml>()
            if enableXmlComment then
                { Coordinate = c; Xml = $"<!-- {x} {y} {expr.ToText()} -->" } |> rungInfos.Add

            match expr with
            | FlatTerminal(id, pulse, neg) ->
                let mode =
                    match pulse, neg with
                    | true, true    -> ElementType.NPulseContactMode
                    | true, false   -> ElementType.PulseContactMode
                    | false, true   -> ElementType.ClosedContactMode
                    | false, false  -> ElementType.ContactMode
                    |> int
                let str = elementBody mode c (id.ToText())
                { baseRIWNP with RungInfos = [{ Coordinate = c; Xml = str}]; }

            | FlatNary(And, exprs) ->
                let mutable sx = x
                let subRungInfos:RungInfosWithSpan list =
                    [
                        for exp in exprs do
                            let sub = rng (sx, y) exp
                            sx <- sx + sub.SpanX
                            rungInfos.AddRange(sub.RungInfos)
                            yield sub
                    ]
                let spanX = subRungInfos.Sum(fun sri-> sri.SpanX)
                let spanY = subRungInfos.Max(fun sri-> sri.SpanY)
                { baseRIWNP with RungInfos=rungInfos.Distinct().ToFSharpList(); SpanX=spanX; SpanY=spanY; }

            | FlatNary(Or, exprs) ->
                let mutable sy = y
                let subRungInfos:RungInfosWithSpan list =
                    [
                        for exp in exprs do
                            let sub = rng (x, sy) exp
                            sy <- sy + sub.SpanY
                            rungInfos.AddRange(sub.RungInfos)
                            yield sub
                    ]
                let spanX = subRungInfos.Max(fun sri-> sri.SpanX)
                let spanY = subRungInfos.Sum(fun sri-> sri.SpanY)

                [
                    for ri in subRungInfos do
                        if ri.SpanX < spanX then
                            let param =
                                let span = (spanX - ri.SpanX - 1) * 3
                                $"Param={dq}{span}{dq}"
                            let mode = int ElementType.MultiHorzLineMode
                            let c = coord (ri.X+ri.SpanX, ri.Y)
                            { Coordinate = c; Xml = elementFull mode c param "" }
                ] |> rungInfos.AddRange

                // 좌측 vertical lines
                if x >= 1 then
                    vlineDownTo (x-1, y) (spanY-1) |> rungInfos.AddRange

                // ```OR variable length 역삼각형 test```
                let lowestY =
                    subRungInfos
                        .Where(fun sri -> sri.SpanX <= spanX)
                        .Max(fun sri -> sri.Y)
                // 우측 vertical lines
                vlineDownTo (x+spanX-1, y) (lowestY-y) |> rungInfos.AddRange


                { baseRIWNP with RungInfos=rungInfos.Distinct().ToFSharpList(); SpanX=spanX; SpanY=spanY; }


            | FlatNary((OpCompare _ | OpArithematic _), exprs) ->
                failwith "ERROR : Should have been processed in early stage."    // 사전에 미리 처리 되었어야 한다.  여기 들어오면 안된다. XgiStatement

            // terminal case
            | FlatNary(OpUnit, inner::[]) ->
                inner |> rng (x, y)

            // negation 없애기
            | FlatNary(Neg, inner::[]) ->
                let xxx = inner.Negate()
                FlatNary(OpUnit, [inner.Negate()]) |> rng (x, y)

            | FlatZero ->
                let str = hlineEmpty c
                { baseRIWNP with RungInfos=[{ Coordinate = c; Xml = str}]; SpanX=0; SpanY=0; }

            | _ ->
                failwithlog "Unknown FlatExpression case"

        /// 최초 시작이 OR 로 시작하면 우측으로 1 column 들여쓰기 한다.
        let indent = 0  // if getDepthFirstLogical expr = Some(Op.Or) then 1 else 0

        let result =
            match expr with
            | Some expr -> rng (x+indent, y) expr
            | _ ->
                let c = coord(x, y)
                let xml = elementFull ElementType.MultiHorzLineMode c $"Param={dq}3{dq}" ""
                { RungInfos = [{ Coordinate = c; Xml = xml}]; X=x; Y=y; SpanX=1; SpanY=1; }

        noop()

        let mutable commandHeight = 0
        /// 좌표 * xml 결과 문자열
        let positionedRungXmls =
            [
                yield! result.RungInfos

                //if indent = 1 then
                //    assert(false)   // indent 가 필요하면, 사용할 코드.  현재는 indent 0 으로 fix
                //    let c = coord(x, y)
                //    { Position = c; Xml = elementFull (int ElementType.MultiHorzLineMode) c "Param=\"0\"" "" }
                let nx = x + result.SpanX
                let commandSpanY, posiRungXmls =
                    match cmdExp.CommandType with
                    | CoilCmd (cc) ->
                        drawCoil (nx-1, y) cmdExp
                    | ( FunctionCmd _ | FunctionBlockCmd _ ) ->
                        drawCommand (nx, y) cmdExp

                commandHeight <- commandSpanY
                yield! posiRungXmls
            ]


        let xml =
            positionedRungXmls
                |> Seq.sortBy (fun ri -> ri.Coordinate)   // fst
                |> Seq.map (fun ri -> ri.Xml)  //snd
                |> String.concat "\r\n"

        let c =
            let spanY = max result.SpanY commandHeight
            coord(x, spanY + y)
        { Xml = xml; Coordinate = c }

