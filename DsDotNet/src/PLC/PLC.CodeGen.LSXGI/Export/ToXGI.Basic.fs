namespace Dual.ConvertPLC.FS.LsXGI

open Engine.Common.FS
open Dual.Core.Types
open Dual.Core.Types.Command
open Dual.Core.QGraph.Prelude
open Dual.ConvertPLC.FS.LsXGI.Config.POU.Program.LDRoutine

[<AutoOpen>]
module internal Basic =
    let rec getDepthFirstLogical (expr: FlatExpression) =
        match expr with
        | FlatNary(op, FlatTerminal(_)::_) -> Some(op)
        | FlatNary(op, h::_) -> getDepthFirstLogical h
        | _ -> None

    /// Flat expression 을 논리 Cell 좌표계 x y 에서 시작하는 rung 를 작성한다.
    /// xml 및 다음 y 좌표 반환
    let rung x y expr (cmdExp:XgiCommand) : string * int =
     

        /// x y 위치에서 expression 표현하기 위한 정보 반환
        /// {| Xml=[|c, str|]; NextX=sx; NextY=maxY; VLineUpRightMaxY=maxY |}
        /// - Xml : 좌표 * 결과 xml 문자열
        /// - NextX : 다음 element 의 시작 x 위치
        /// - VLineUpRightMaxY : 수직 라인을 그을 때, 우측 최상단 종점의 y 좌표
        let mutable addNextY = 0
        let rec rng x y (expr:FlatExpression) =
            
            let c = coord x y
            /// 좌표 * 결과 xml 문자열 보관 장소
            let xml = ResizeArray<int * string>()
            (c, sprintf "<!-- %d %d %s -->" x y (expr.ToText())) |> xml.Add

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
                {| Xml=[|c, str|]; NextX=x; NextY=y; VLineUpRightMaxY=y |}

            | FlatNary(And, exprs) ->
                let mutable sx = x
                let mutable maxY = y
                for exp in exprs do
                    let sub = rng sx y exp
                    sx <- sub.NextX + 1
                    maxY <- max maxY sub.NextY
                    xml.AddRange(sub.Xml)
                sx <- sx - 1    // for loop 에서 마지막 +1 된 것 revert
                {| Xml=xml.ToArray(); NextX=sx; NextY=maxY; VLineUpRightMaxY=maxY |}

            | FlatNary(Or, exprs) ->
                let mutable sy = y
                let mutable vLineUpMaxY = y
                let mutable maxX = x
                /// OR 로 묶인 block 들의 종료 위치 정보 x * y
                let endInfo = ResizeArray<int*int>()
                for (i, exp) in (exprs |> Seq.indexed) do
                    let sub = rng x sy exp
                    endInfo.Add((sub.NextX, sy))
                    sy <- sub.NextY + 1
                    vLineUpMaxY <- max vLineUpMaxY sub.VLineUpRightMaxY
                    maxX <- max maxX sub.NextX
                    xml.AddRange(sub.Xml)

                sy <- sy - 1    // for loop 에서 마지막 +1 된 것 revert

                // short end 우측 확장 연결 정보를 xml 에 저장
                endInfo
                    |> Seq.filter (fun (x, y) -> x < maxX)
                    |> Seq.map (fun (x, y) ->
                        let x = x + 1
                        let param = sprintf "Param=\"%d\"" ((maxX - x)*3)
                        let mode = int ElementType.MultiHorzLineMode
                        let c = coord x y
                        c, elementFull mode c param "")
                    |> xml.AddRange

                // 좌측 vertical lines
                vlineDownTo (x-1) y (sy-y) |> xml.AddRange

                // 우측 vertical lines
                vlineDownTo maxX y (vLineUpMaxY-y) |> xml.AddRange

                {| Xml=xml.ToArray(); NextX=maxX; NextY=sy; VLineUpRightMaxY=y |}
            | FlatZero -> 
                let str = hlineEmpty c
                {| Xml=[|c, str|]; NextX=x; NextY=y; VLineUpRightMaxY=y |}
            | _ ->   failwithlog "Unknown FlatExpression case"


        /// 최초 시작이 OR 로 시작하면 우측으로 1 column 들여쓰기 한다.
        let indent = 0  // if getDepthFirstLogical expr = Some(Op.Or) then 1 else 0

        let result = rng (x+indent) y expr
        /// 좌표 * xml 결과 문자열
        let cXml =
            seq {
                yield! result.Xml
                if indent = 1 then
                    assert(false)   // indent 가 필요하면, 사용할 코드.  현재는 indent 0 으로 fix
                    let c = coord x y
                    yield c, elementFull (int ElementType.MultiHorzLineMode) c "Param=\"0\"" ""

                let drawCoil(x, y) = 
                    let results = ResizeArray<int * string>()
                    let lengthParam = sprintf "Param=\"%d\"" (3 * (coilCellX-x-2))
                    let c = coord (x+1) y
                    results.Add((c, elementFull (int ElementType.MultiHorzLineMode) c lengthParam ""))
                    let c = coord coilCellX y 
                    results.Add((c, elementBody (int cmdExp.LDEnum) c (cmdExp.CoilTerminalTag |> toText)))
                    0, results      

                let nx = result.NextX
                let newY, result = 
                    match cmdExp.CommandType with
                    | CoilCmd (cc) -> drawCoil(nx, y)
                    | FunctionCmd (fc) -> drawCommand(cmdExp, nx, y)
                    | FunctionBlockCmd (fbc) -> drawCommand(cmdExp, nx, y)

                addNextY <- newY; yield! result
            }

        let xml =
            cXml
                |> Seq.sortBy fst
                |> Seq.map snd
                |> String.concat "\r\n"

        xml, result.NextY + addNextY

