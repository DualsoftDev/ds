namespace Dsu.PLCConverter.FS

open Dsu.PLCConverter.FS.XgiSpecs.Config.POU.Program.LDRoutine
open System.Collections.Generic
open System.Text.RegularExpressions

[<AutoOpen>]
/// 프로그램을 Ladder 형태로 그리는 기본 구조를 정의
module XgiDraw =

    /// 프로그램의 끝을 나타내는 요소를 그리기
    let drawEnd(y: int) =
        let results = ResizeArray<string>()
        let yy = y * 1024 + 1
        // END를 나타내는 수평 라인 요소 추가
        sprintf "\t\t<Element ElementType=\"%d\" Coordinate=\"%d\" Param=\"90\"></Element>" 
            (int ElementType.MultiHorzLineMode) yy |> results.Add
        // END 태그 추가
        sprintf "\t\t<Element ElementType=\"%d\" Coordinate=\"%d\" Param=\"END\">END</Element>" 
            (int ElementType.FBMode) (yy + 93) |> results.Add
        results

    /// Rung에 주석 추가
    let drawRungComment(rungComment: string, y: int) =
        let results = ResizeArray<string>()
        let rungComment =
            if rungComment.StartsWith("[Title]")
            then 
                rungComment.Replace("[Title]", "[#Title]")      
                else 
                rungComment
      
        let yy = y * 1024 + 1
        // 주석 내용을 포함한 RungCommentMode 요소 추가
        sprintf "\t\t<Element ElementType=\"%d\" Coordinate=\"%d\">%s</Element>" 
            (int ElementType.RungCommentMode) yy (rungComment |> System.Security.SecurityElement.Escape) |> results.Add
        results

    /// 레이블 요소 추가
    let drawLabel(label: string, y: int) =
        let results = ResizeArray<string>()
        let yy = y * 1024 + 1
        // LabelMode 요소 추가
        sprintf "\t\t<Element ElementType=\"%d\" Coordinate=\"%d\">%s</Element>" 
            (int ElementType.LabelMode) yy label  |> results.Add
        results

    /// Coil을 그리는 함수
    let drawCoil (arg: string, x, y, elementType: ElementType) =
        let results = ResizeArray<string>()
        if x < coilCellX then
            // Coil 좌표가 제한을 넘지 않는다면 Coil을 그리고 끝 라인 추가
            mutiEndLine x coilCellX y |> results.Add
            let c = coord coilCellX y
            results.Add(elementFull (int elementType) c "" arg)
        else
            // 좌표가 제한을 넘으면 오류로 처리
            errorHistory.Add(sprintf "Too many contacts [%d]" x)
            convertOk <- false
        y + 1, results

    /// 일반적인 Contact를 그리는 함수
    let drawNormalC (op, opOpt: OpOpt, (args: seq<string>), basePoint: LoadPoint, runPoint: LoadPoint) =
        let results = ResizeArray<string>()
        let initX, initY, runX, runY, addX, addY = drawPoint basePoint runPoint opOpt    
       
        // 산전 타이머 출력을 조건으로 사용할 때, Contact 주소 형식 설정
        let contactXml (c: int) =
            let contactSym = getNewArgs args "" |> Seq.head
            elementFull (int (getContactMode opOpt)) c "" contactSym

        // 연산자에 따라 좌표 설정
        let x, y =
            match op with
            | Load -> initX, initY
            | And -> runX, initY
            | Or -> 
                let leftP = runPoint
                let rightP = LoadPoint(initX, runY, initX + addX, runY + addY)
                addBlankLine (leftP, rightP) |> results.AddRange
                initX, runY

        // Contact 요소 추가
        let coordLocal = coord x y
        results.Add(contactXml coordLocal)
        let newRunPt = getNewPoint(runPoint, op, OpOpt.Normal)
        newRunPt, results

    /// 비교 Contact를 그리는 함수
    let drawCompare (op, opComp: OpComp, opCompType: OpCompType, (args: seq<string>), basePoint: LoadPoint, runPoint: LoadPoint) =
        let results = ResizeArray<string>()
        let initX, initY, runX, runY, addX, addY = drawPoint basePoint runPoint OpOpt.Compare    

        let nibbles = args.where(fun f-> Regex.IsMatch(f, NibbleText)) |> Seq.toList
        let addresses = args.map(fun f-> getArgAddress f).where(fun f-> IsXGIAddress f) |> Seq.toList
        let opCompText = 
            if nibbles.any() then
                match nibbles[0] with
                | ActivePattern.RegexPattern NibbleText [k; nibbleSize; addr] ->
                    match nibbleSize with
                    |  "1" | "2"  -> "BYTE"
                    |  "3" | "4"  -> "INT"
                    |  "5" | "6" | "7"| "8" -> "DINT"
                    | _ -> failwithf $"error nibbleSize {nibbleSize}"
                | _ -> failwithf $"error not nibble Type{nibbles[0]}"
            
            elif addresses.any()
            then
                match addresses[0].Substring(2, 1) with  //%MD234
                | "B" -> "BYTE"
                | "W" -> "INT"
                | "D" -> "DINT"
                | "L" -> "LONG"
                | _ -> failwithf $"error nibbleSize {addresses[0]}"
            else 
                opCompType.ToText
                
        // 비교 연산자에 따라 XGI FBMode와 매개변수 설정
        let func = opComp.ToText
        let funcFind = 
            if opComp = OpComp.NE 
            then sprintf "%s_%s" opComp.ToText opCompText
            else sprintf "%s2_%s" opComp.ToText opCompText

        let mutable paraX = initX
        let mutable paraY = initY
        let x, y =
            match op with
            | Load -> (initX + 1), initY
            | And -> paraX <- runX; paraY <- initY; (runX + 1), initY
            | Or -> 
                paraX <- initX; paraY <- runY
                let leftP = runPoint
                let rightP = LoadPoint(initX, runY, initX + addX, runY + addY)
                addBlankLine (leftP, rightP) |> results.AddRange
                (initX + 1), runY

        // FBMode 요소 추가
        let coordLocal = coord x y
        let FB_Param = sprintf "Param=\"%s\"" ((getFBXML(funcFind, func, ",", getFBIndex opComp.ToText)))
        results.Add(elementFull (int ElementType.VertFBMode) coordLocal FB_Param "")

        // 인자 설정
        let newArgs = newDoubleArgs(parseCheckType (opCompText)) args ""
        let in1 = 1
        let in2 = 2
        let out1X = 2
        let out1Y = 1
        results.Add(elementFull (int ElementType.HorzLineMode) (coord paraX paraY) "" "")
        results.Add(elementFull (int ElementType.VariableMode) (coord paraX (paraY + in1)) "" (newArgs |> Seq.head))
        results.Add(elementFull (int ElementType.VariableMode) (coord paraX (paraY + in2)) "" (newArgs |> Seq.last))
        results.Add(elementFull (int ElementType.HorzLineMode) (coord (paraX + out1X) (paraY + out1Y)) "" "")
        vlineDownTo (paraX + 2) paraY 1 |> results.AddRange // 우측 수직 라인 추가
        let newRunPt = getNewPoint(runPoint, op, OpOpt.Compare)
        newRunPt, results

    /// 상승 에지 요소 추가
    let drawRising (x, y) =
        let results = ResizeArray<string>()
        let cellX = fbCellX x
        let c = coord cellX y
        results.Add(risingline c)
        mutiEndLine x (cellX - 1) y |> results.Add
        results

    /// Contact를 그리는 함수
    let drawContact (contact: contact, basePoint: LoadPoint, runPoint: LoadPoint) =
        let args, op, opOpt, opComp, opCompType = contact
        match opOpt with
        | Compare -> drawCompare(op, opComp, opCompType, args, basePoint, runPoint)
        | _ -> drawNormalC(op, opOpt, args, basePoint, runPoint)

    /// 여러 Loader 요소를 그리는 함수
    let drawLoaders (loaders: List<LoadBlock>) =
        let results = ResizeArray<string>()
        let mutable continueX = 0
        let mutable continueY = 0

        // 개별 Loader를 재귀적으로 그리는 내부 함수
        let rec drawLoader(load: LoadBlock) =
            match load with
            | LoadBase(contacts, pt) ->
                if contacts.Count <> 0 then
                    let runPt = pt.Copy()
                    runPt.EX <- runPt.SX
                    runPt.EY <- runPt.SY
                    let basePt = runPt.Copy()

                    continueX <- max continueX pt.EX
                    continueY <- pt.SY
                    contacts
                    |> Seq.map (fun con -> 
                        let newPt, ret = drawContact(con, basePt, runPt)
                        runPt.Update(newPt); results.AddRange(ret))
                    |> Array.ofSeq 
                    |> ignore
                else
                    continueX <- max continueX pt.EX

            | Extend(contacts, basePt, exPt) ->
                if contacts.Count <> 0 then
                    let runPt = basePt.Copy()
                    let basePt = basePt.Copy()
                    continueX <- max continueX exPt.EX
                    continueY <- exPt.SY
                    contacts
                    |> Seq.map (fun con -> 
                        let newPt, ret = drawContact(con, basePt, runPt)
                        runPt.Update(newPt); results.AddRange(ret))
                    |> Array.ofSeq 
                    |> ignore
                else
                    continueX <- max continueX exPt.EX

            | Mix(left, op, right, extend) ->
                drawLoader left
                drawLoader right
                drawLoader extend
                let leftP = getPoint left
                let rightP = getPoint right

                match op with
                | OrLoad -> addBlankLine(leftP, rightP) |> results.AddRange
                | _ -> ()

            | Empty -> ()

        loaders
        |> Seq.map drawLoader
        |> Array.ofSeq 
        |> ignore
        continueX, continueY, results
