namespace Dsu.PLCConverter.FS

open Dsu.PLCConverter.FS.XgiSpecs.Config.POU.Program.LDRoutine
open Dsu.PLCConverter.FS.XgiSpecs
open System

[<AutoOpen>]
module XgiFBMixDraw =

    let results: ResizeArray<string> = ResizeArray<string>()

    /// FB Mode XML 요소 추가
    let private addFBModeElement fbName findName x y =
        let instance = getInstance fbName
        let FB_Param = sprintf "Param=\"%s\"" (getFBXML(findName, fbName, instance, getFBIndex findName))
        let c = coord (x + 1) y
        results.Add(elementFull (int ElementType.VertFBMode) c FB_Param "")

    let private handle_DATERD  orgArg  x y =
        let fbFindName = "BYTE_TO_WORD"
        let fbCallName = "BYTE_TO_***"
        let mutable fbY = y
        let fbX = x + 1
        let xgiAddress, _, bitOffset = XGI.MakeXgiAddressWithOffset(XGI.Parsing orgArg)
        let countFB = 6
        for i in 0..countFB do

            let _inCount, allCount = getFBInCount(fbFindName)
            let addY = allCount + 2 //기본 FB y = 2 + input 인자  
            hlineRightTo fbX fbY 1 |> results.AddRange // 우측 수평 라인 추가
            
            if i <> countFB //마지막 FB가 아니면 (마지막은 기본 생성됨)
            then 
                vlineDownTo (fbX-1) fbY addY |> results.AddRange // 좌측 수직 라인 추가

            let address = $"{xgiAddress[..2]}{bitOffset/16+i}"

            addFBModeElement  fbCallName fbFindName fbX fbY       
            InArgs([$"_RTC_TIME[{i}]"],fbX, fbY) |> results.AddRange
            OutArgs([address],fbX, fbY) |> results.AddRange

            fbY <- fbY + addY 

        fbY

    //let handle_NiibleMove (fbInfo:FBInfo) x y =
    //    results.Clear()
    //    let fb = "BMOV"
    //    let fbX = x
    //    let mutable fbY = y
    //    let srcName = fbInfo.Args |> Seq.head
    //    let dstName = fbInfo.Args |> Seq.last

    //    (*BMOV*)
    //    hlineRightTo fbX fbY 1 |> results.AddRange // 우측 수평 라인 추가
    //    addFBModeElement  fb fb fbX fbY       
    //    let addressType, addressIndex, bitSize = 
    //        match dstName with
    //        | ActivePattern.RegexPattern @"(K)(\d+)(\D+\S+)" [k; nibbleSize; addr] 
    //            -> addr.[..0],    (Convert.ToInt32(addr.[2..])),    (Convert.ToInt32(nibbleSize)*4) //K6MX23231
    //        | _ ->  failwithf $"handle_NiibleSymbol ({dstName}) err"

    //    let address = $"%%{addressType}L{addressIndex/64}"
    //    let offset  = addressIndex%64

    //    InArgs([srcName;"0";$"{offset}";"0";$"{bitSize}"],fbX, fbY) |> results.AddRange
    //    OutArgs([address],fbX, fbY) |> results.AddRange

    //    let _inCount, allCount = getFBInCount(fb)
    //    let addY = allCount+2 //기본 FB y = 2 + input 인자  
    //    fbY <- fbY + addY 

    //    fbY, results

    let handle_NiibleSymbol (symbols:SymbolInfo seq) y =
        results.Clear()
        let fb1 = "BMOV"
        let fb2 = "LWORD_TO_***"
        let fbX = 0
        let mutable fbY = y
        let symbols = symbols 
                        |> Seq.filter(fun symbol -> symbol.Type() <> "LWORD")  //k8최대 32 bit  temp 필터링

        if symbols.any()
        then
            results.Add
                (sprintf "<Element ElementType=\"%d\" Coordinate=\"%d\">자동생성 코드: 비교문 사용된 K1 ~ K8 Bit 처리</Element>" (int ElementType.RungCommentMode) fbY)
            fbY <- fbY+1

            symbols 
            |> Seq.filter(fun symbol -> symbol.Type() <> "LWORD")  //k8최대 32 bit  temp 필터링
            |> Seq.iter(fun symbol ->
                let tempLword = $"{symbol.Name}_LWORD"

                (*BMOV*)
                hlineRightTo fbX fbY 1 |> results.AddRange // 우측 수평 라인 추가
                addFBModeElement  fb1 fb1 fbX fbY       
                let addressType, addressIndex, bitSize = 
                   match symbol.Name.Split('_')[0] with
                   | ActivePattern.RegexPattern NibbleText [k; nibbleSize; addr] 
                        -> addr.[..0], (Convert.ToInt32(addr.[2..])), (Convert.ToInt32 (nibbleSize)*4) 
                   | _ -> failwithf $"handle_NiibleSymbol ({symbol.Name}) err"

                let address = $"%%{addressType}L{addressIndex/64}"
                let offset = addressIndex%64

                InArgs([address;"0";$"{offset}";"0";$"{bitSize}"],fbX, fbY) |> results.AddRange
                OutArgs([tempLword],fbX, fbY) |> results.AddRange

                (*LWORD_TO_****)
                let fbX2 = fbX+3
                let fb2Find = fb2.Replace("***", symbol.Type())

                hlineRightTo (fbX2-1) fbY 2 |> results.AddRange // 우측 수평 라인 추가
                addFBModeElement  fb2 fb2Find fbX2 fbY       
                InArgs([tempLword],fbX2, fbY) |> results.AddRange
                OutArgs([symbol.Name],fbX2, fbY) |> results.AddRange
            
                let _inCount, allCount1 = getFBInCount(fb1)
                let _inCount, allCount2 = getFBInCount(fb2Find)
                let addY = Math.Max(allCount1, allCount2)+2 //기본 FB y = 2 + input 인자  
                fbY <- fbY + addY 
                )

        results, fbY
  
    /// FB 변환 작업
    let drawFBConvertMix (midResults: ResizeArray<string>, orgCmd: string, orgArg: string, (args:string list), x, y) =
        results.Clear()
        results.AddRange(midResults)
        
        let fbX, fbY = (fbCellX x), y
        let mutable retY = 0 

        // FB 유형에 따라 처리
        match orgCmd with
        | "DATERD" ->
            retY <- handle_DATERD orgArg fbX fbY 
             

        | _ ->
            failwithf $"Not supported FB: {orgCmd}"    

        retY+y, results
