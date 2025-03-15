namespace Dsu.PLCConverter.FS

open Dsu.PLCConverter.FS.XgiSpecs.Config.POU.Program.LDRoutine
open System.Linq
open XgiBaseXML
open Dsu.PLCConverter.FS.XgiSpecs
open System

[<AutoOpen>]
/// XGI로 FB 변환을 위한 기본 구조
module XgiFBDraw =

    let drawFBConvert (results:ResizeArray<string>, fbInfo:FBInfo, x, y) =
        let fbX = fbCellX x
        let c = coord (fbX + 1) y
        let mutable fX = x
        let mutable fY = y
        let fbName = fbInfo.FbName
        let findName = 
            if  existFBXML fbInfo.FindName  
            then
                fbInfo.FindName  
            else
                fbInfo.FindName.Replace("_DINT", "_DWORD").Replace("_INT", "_WORD").Replace("_LINT", "_LWORD")   
                        
        let args  = fbInfo.Args
        let oriArgs = fbInfo.MelsecArgs

        if not (existFBXML findName) then
            let err = String.Join(", ", fbInfo.MelsecArgs)
            errorHistory.Add(sprintf "Invalid step %d (%s) [pou : %s]" pouStep  err pouName)
            convertOk <- false
            fY, results
        else

            let findName, fbName = 
                if DicMelsecToXgiSym.ContainsKey(oriArgs.First())
                then 
                    match fbName with
                    | "GROUP_MOVE" when  oriArgs.any() && isArrayDevice(oriArgs.First())  //SRC가 Z 레지스터 dev 일경우
                        -> "ANY_MOVE2", "ANY_MOVE2"
                    |_ -> findName, fbName
                else 
                    findName, fbName

            let instance = getInstance fbName
            let FB_Param = sprintf "Param=\"%s\"" (getFBXML(findName, fbName, instance, getFBIndex findName))
            results.Add(elementFull (int ElementType.VertFBMode) c FB_Param "")

            let inCount, allCount = getFBInCount(findName)
            let mutable retY = fY + allCount + 2

            let newArgs = args |> Seq.toList

            match fbName with
            | "TIME_DE" ->
                retY <- retY + 4

            | "SWAP" | "INC" | "DEC" ->
                InArgs(newArgs, fbX, y) |> results.AddRange
                OutArgs(newArgs, fbX, y) |> results.AddRange

            | "FF" ->
                OutArgs(newArgs, fbX, y - 1) |> results.AddRange
                retY <- fY + 2

            | "MCS" ->
                InArgs([newArgs[0].TrimStart('N')], fbX, y) |> results.AddRange
                OutArgs(newArgs[1..], fbX, y - 1) |> results.AddRange

            | "MCSCLR" ->
                InArgs(newArgs |> List.map (fun f -> f.TrimStart 'N'), fbX, y) |> results.AddRange

            | "GROUP_FILL" ->
                if fbInfo.DataType = CheckType.BOOL then
                    InArgs(["false"], fbX, y) |> results.AddRange
                    InArgs([newArgs[0]], fbX, y + 1) |> results.AddRange
                    InArgs(["0"], fbX, y + 2) |> results.AddRange
                    InArgs([newArgs[1]], fbX, y + 3) |> results.AddRange
                else
                    InArgs(newArgs[0..1], fbX, y) |> results.AddRange
                    InArgs(["0"], fbX, y + 2) |> results.AddRange
                    InArgs(newArgs[2..], fbX, y + 3) |> results.AddRange

            | "GROUP_MOVE" ->
                InArgs([newArgs[0]], fbX, y) |> results.AddRange
                InArgs(["0"], fbX, y + 1) |> results.AddRange
                InArgs(["0"], fbX, y + 2) |> results.AddRange
                InArgs(newArgs[2..], fbX, y + 3) |> results.AddRange
                OutArgs([newArgs[1]], fbX + 1, y) |> results.AddRange


            | "ANY_MOVE2" -> //z 인덱스 디바이스 전용 %MW123[Z11]
                let src = newArgs[0].Split('[')[0]
                let index = (newArgs[0].Split('[')[1]).TrimEnd(']') 
                InArgs([src;"3";index;"0";newArgs[2]], fbX, y) |> results.AddRange //1bit. 2byte. 3word. 4:Dword 5:LWord
                OutArgs([newArgs[1]], fbX+1, y) |> results.AddRange

            | "ANY_MOVE" ->
                InArgs([newArgs[0]], fbX, y) |> results.AddRange
                OutArgs([newArgs[1]], fbX+1, y) |> results.AddRange
  
            | "ARY_MOVE" ->
                InArgs(["1"], fbX, y) |> results.AddRange
                InArgs([newArgs[0]], fbX, y+1) |> results.AddRange
                InArgs(["0"], fbX, y+2) |> results.AddRange
                InArgs(["0"], fbX, y+3) |> results.AddRange
                OutArgs([newArgs[1]], fbX, y) |> results.AddRange

            | _ ->
                let inItems = newArgs[0..(inCount - 1)]
                InArgs(inItems, fbX, y) |> results.AddRange
                let outItems = newArgs[inCount..]
                OutArgs(outItems, fbX, y) |> results.AddRange

            retY, results


    let drawFB (func:string, cmd:string, (args:string list), oriArg:string, x, y) =

        let results = ResizeArray<string>()

        let usePulse = func.Contains(";P")
        if usePulse then
            drawRising (x, y) |> results.AddRange
        else
            mutiEndLine x (fbCellX x) y |> results.Add


        let func = func.Split(';')[0]
        //if func = XgiFBUtils.MIXFB then // 함수를 그대로 변환아닌 특수 처리 DATARD 등등
        //    drawFBConvertMix(results, cmd, oriArg, args, x, y) 
        //else 
        let fbInfo = getFBInfo (func, args, oriArg)
        
        drawFBConvert(results, fbInfo, x, y) 
                
            
