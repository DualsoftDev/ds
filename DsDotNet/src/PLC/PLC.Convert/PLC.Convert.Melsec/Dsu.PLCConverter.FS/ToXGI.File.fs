namespace Dsu.PLCConverter.FS

open Dsu.PLCConverter.FS.XgiSpecs.Config
open System.Collections.Generic
open Dsu.PLCConverter.FS.XgiSpecs.Config.POU.Program.LDRoutine
open System.Text.RegularExpressions
open System.Linq
open XgiSpecial
open ActivePattern

[<AutoOpen>]
/// 프로그램 XML 파일로 저장하기 위한 기본 구조
module XgiFile =


    let getXmlAbleSymbols (usingDirect: bool) (symbolSrcs: SymbolInfo seq) =
        if usingDirect then 
            symbolSrcs 
            |> Seq.filter(fun f -> f.Type() = "STRING" || f.Type().StartsWith("ARRAY")
                                || f.DeviceType() = "A" 
                                || f.Address.isNullOrEmpty() //주소없는 GlobalLabel 변수
                                || f.IsNibble
                                || AutoType.Contains(f.Type()))
        else 
            symbolSrcs

    let appendInstSymbols(symbolSrcs:SymbolInfo seq)   = 
        symbolSrcs
        |> Seq.append(
            insts|> Seq.map (fun (f,n) -> SymbolInfo(0,n,"","A","",(int Variable.Kind.VAR),f.ToText ,0, "", false))
            )


    let generateNibbleRung (symbols:SymbolInfo seq) y =
        let symbols = symbols.Where(fun f->  f.IsNibble)
        let xml, y' = handle_NiibleSymbol symbols y 
    
        seq {
            yield "\t<Rung BlockMask=\"0\">"
            yield! xml
            yield "\t</Rung>"
        }
        |> String.concat "\r\n", y'


    /// variable 정의 구역 xml 의 string 을 생성
    let generateSymbols (symbols:SymbolInfo seq) (bGlobal:bool) (usingDrirect:bool)=
        let symbols = symbols.Where(fun f->  f.DeviceType() <> "S") //fx fw 특수 주소 제외
        let symbols = if bGlobal then symbols
                      else appendInstSymbols symbols   
                      
        let xmlSymbols = symbols |> getXmlAbleSymbols usingDrirect 

        seq {
            yield sprintf "<%s Version=\"Ver 1.0\" Count=\"%d\">" (if bGlobal then "GlobalVariable" else "LocalVar") (xmlSymbols.length())
            yield "<Symbols>"
            yield! xmlSymbols |> Seq.map (fun s -> s.GenerateXml())
            yield "</Symbols>"
            yield "<TempVar Count=\"0\"></TempVar>"
            if bGlobal then
                yield sprintf "<DirectVarComment Count=\"%d\">" (symbols.length())
                yield! symbols |> Seq.map (fun s -> s.GenerateDirectVarXml())
                yield sprintf "</DirectVarComment>"
            yield sprintf "</%s>" (if bGlobal then "GlobalVariable" else "LocalVar")
        }
        |> String.concat "\r\n"

    /// (조건=coil) seq 로부터 rung xml 들의 string 을 생성
    let generateNewSymbol (pous:POUParseResult seq) (commentDic:CommentDictionary)=
        let newSymbol = Dictionary<string, SymbolInfo>()
        let usingAddress =
                        seq {
                            for pou in pous do
                                for rung in pou.Rungs do
                                    for line in rung do
                                        yield printArgumentNCmdOfLine line
                        }


        //let skipAddressHead = ["U"; "N"; "K"; "J"; "P"]     //N -> MCS  영역 MCS  //P -> Lable영역 JMP
        //let skipAddressBody = ["TRUE"; "FALSE"; "IN";]     //Works3 LOAD TRUE, FALSE 대응
        usingAddress
        |> Seq.iter (fun (args, cmd) -> 
            args
            |> Seq.filter (IsMelsecAddress)
            |> Seq.filter (fun f -> not (newSymbol.ContainsKey(f)) && not (commentDic.ContainsKey(f)))
            |> Seq.iter (fun f -> 
            
                        //(\D)숫자아닌 모든문자
                if(isNibbleDevice(f) && not (isArrayDevice(f))) 
                    then 
                        let sym, symTempLWord =  makeBatchSymbol f
                        newSymbol.Add (f,sym)// K4Y03F  K배치 사용
                        newSymbol.[symTempLWord.Name] <- symTempLWord// K4Y03F  symTempLWord 사용
                   
                elif(not (isNibbleDevice f) && isArrayDevice(f))
                    then 
                        let sym =  makeIndexArraySymbol f
                        newSymbol.Add (f,sym)// K4Y03F  K배치 사용
                else   
                    match f with
                    | _ when Regex.IsMatch(f, @"(^[K|H|E])([-.0-9A-Fa-f]+)") -> 
                        // K 10진수, H 16진수, E 실수 타입 등 처리
                        printfn "Matched numeric or specific type: %s" f
                    | _ when Regex.IsMatch(f, @"""") -> 
                        // 문자열 포함 처리
                        printfn "Matched string: %s" f
                    | _ -> 
                        let sym = makeSymbol f "" Variable.Kind.VAR_GLOBAL
                        newSymbol.Add(f, sym)
                    )
                )

        newSymbol

    let getUsingSymbols pou  (symbols:SymbolInfo seq)=
        let newSymbols = List<SymbolInfo>()
        let usingAddress =
            seq {
                for rung in pou.Rungs do
                    for line in rung do
                        yield! ArgumentOfLine line
            }   |> HashSet
        symbols
        |> Seq.filter (fun f -> usingAddress.Contains(f.GxAddress.Replace("_Nibble", "")))//nibble address K8M102_Nibble
        |> Seq.map createLocalSymbol
        |> newSymbols.AddRange

        newSymbols

    /// (조건=coil) seq 로부터 rung xml 들의 string 을 생성
    let generateRungs (pous:POUParseResult seq, symbols:SymbolInfo seq, usingDrirect:bool)=
        let errorLog =  ResizeArray<string>()
        let warningLog =  ResizeArray<string>()
        seq {
            for pou in pous do
                pouName <- pou.Name
                pouStep <- 0
                insts.Clear() //로컬 인스턴스 초기화
                
                let mutable y = 0
                let mutable convertPass = true
                yield "<Program Task=\"스캔 프로그램\" LocalVariable=\"1\" Kind=\"0\" InstanceName=\"\" Comment=\"\" FindProgram=\"1\" Encrytption=\"\">"
                yield sprintf "%s" pou.Name
                yield "<Body>"
                yield "<LDRoutine>"
                let newSymbols = getUsingSymbols pou symbols
                DicMelsecToXgiSym.Clear()
                DicXgiSym.Clear()
                newSymbols |> Seq.where(fun f -> not(DicMelsecToXgiSym.ContainsKey f.Name)) |> Seq.iter(fun f->DicMelsecToXgiSym.Add(f.GxAddress, (f.Address, f.Name)))
                newSymbols |> Seq.where(fun f -> not(DicXgiSym.ContainsKey f.Name))         |> Seq.iter(fun f->DicXgiSym.Add(f.Name, f.Address))

                convertPass <- true
                let xml, y' = generateNibbleRung newSymbols y
                y <- y'
                yield xml

                for rung in pou.Rungs do
                    //rung 조건 작성
                    let xml, y' =  rungXml y rung  usingDrirect
                    if(not convertOk)
                    then
                        convertPass <- false
                        //error rung 주석 작성
                        errorLog.Add(xml)
                        let yy = y * 1024 + 1
                        yield sprintf "\t<Rung BlockMask=\"0\"><Element ElementType=\"%d\" Coordinate=\"%d\">%s</Element></Rung>" (int ElementType.RungCommentMode) yy xml
                        y <- y + 1
                        convertOk <- true
                        errorHistory.Clear()

                    else
                        y <- y'
                        yield "\t<Rung BlockMask=\"0\">"
                        yield xml
                        yield "\t</Rung>"

                    if(warningHistory.Count <> 0) 
                    then
                        let yy = y * 1024 + 1
                        yield sprintf "\t<Rung BlockMask=\"0\"><Element ElementType=\"%d\" Coordinate=\"%d\">[DEBUG]↑↑↑↑↑↑변환경고↑↑↑↑↑↑\r\n%s</Element></Rung>" 
                                (int ElementType.RungCommentMode) yy ("[DEBUG]" +  (warningHistory|> String.concat "\r\n[DEBUG]"))
                        y <- y + 1
                        warningHistory.Clear()

                yield "</LDRoutine></Body>"
                yield generateSymbols newSymbols false  usingDrirect
                yield "<RungTable></RungTable>"
                yield "</Program>"
        }
        |> String.concat "\r\n", errorLog, warningLog

    let wrapWithXml rungsXml symbolsGlobalXml baseXml=
            let allLines =
                seq {
                    for line in StringExt.splitByLines(baseXml) do
                        match line with
                        | ActivePattern.RegexPattern "\s*<InsertPoint Content=\"(\w+)\" />" [insertType] ->
                            match insertType with
                            | "Programs" -> yield rungsXml
                            | "GlobalVariable" -> yield symbolsGlobalXml
                            | _ -> failwithf "Unknown"
                        | _ ->
                            yield line
                }
            allLines

    let programsXml(rungsXml:string) = 
        seq {
               yield "<Programs>"
               yield rungsXml
               yield "</Programs>"
           }|> String.concat "\r\n"

    /// POU를  XML 심볼 구조로 변환
    let getAllSymbols (pous, commentDic:CommentDictionary, globalLabelDic:GlobalLabelDictionary) =
        let symbols = getGlobalSymbols commentDic globalLabelDic
        /// 2-1. comment 에 없는 new Address 추가
        let newSymbols = generateNewSymbol pous commentDic
        let total = newSymbols.Values |> Seq.append symbols

        total, newSymbols

    /// POU를  XML 문자열 로 변환
    let getXgiPou(pous, symbols, usingDrirect) =
        let rungsXml, errLog, warnLog = generateRungs (pous, symbols, usingDrirect)
        rungsXml, errLog, warnLog

    /// POU를  XML 로 저장
    let getXgiXMLPou(rungsXml:string, symbols, usingDrirect) =
        let baseXml = createXgiXml()
        let symbolsGlobalXml = generateSymbols symbols true usingDrirect

        let result = wrapWithXml (programsXml rungsXml) symbolsGlobalXml baseXml
        result |> String.concat "\r\n"

    /// 파일을 XML 문자열 로 변환
    let getXgiXML(files, usingDrirect) =
        let pous, commentDic, globalLabelDic = parseCSVs files
        let symbols, _ = getAllSymbols (pous, commentDic, globalLabelDic)
        let baseXml = createXgiXml()

        /// 1. Rungs 의 XML 문자열(with local var)
        let rungsXml, errLog, warnLog = generateRungs (pous, symbols, usingDrirect)
        /// 2. Symbol table 정의 XML 문자열
        let symbolsGlobalXml = generateSymbols symbols true usingDrirect

        /// 1+2 XGI XML 만들기
        let result = wrapWithXml (programsXml rungsXml) symbolsGlobalXml baseXml
        result, errLog, warnLog
