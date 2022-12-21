namespace Dsu.PLCConverter.FS

open Dual.Common
open Dsu.PLCConverter.FS.XgiSpecs.Config
open System.Collections.Generic
open Dsu.PLCConverter.FS.XgiSpecs.Config.POU.Program.LDRoutine
open System.Text.RegularExpressions

[<AutoOpen>]
/// 프로그램 XML 파일로 저장하기 위한 기본 구조
module XgiFile =
    ///  variable 정의 구역 xml 의 string 을 생성
    let generateSymbols (symbols:SymbolInfo seq) (bGlobal:bool) (usingDrirect:bool)=

        let symbols = symbols
                        |> Seq.filter (fun f-> not (f.DeviceType() = "S"))

        let symbols = if(not bGlobal)
                      then
                          symbols
                            |> Seq.append (
                                insts
                                |> Seq.map (fun (f,n) -> SymbolInfo(0,n,"","A","",(int Variable.Kind.VAR),f.ToText ,0, "",0))
                                        )
                      else symbols

        seq {
                yield sprintf "<%s Version=\"Ver 1.0\" Count=\"%d\">" (if bGlobal then "GlobalVariable" else "LocalVar") (symbols.length())
                yield "<Symbols>"
                yield!
                    symbols
                            |> Seq.filter (fun f -> (not usingDrirect)
                                                    || f.Type() = "STRING"
                                                    || f.DeviceType() = "A"
                                                    || AutoType.Contains(f.Type())
                                            )
                            |> Seq.map (fun s -> s.GenerateXml())
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
        usingAddress
        |> Seq.iter (fun (args, cmd) -> 
            args
            |> Seq.iter (fun (f) ->
                if(not (commentDic.ContainsKey(f))) then
                    if(not (f.StartsWith("U")) && not (f.StartsWith("N"))        //N -> MCS  영역 MCS
                    && not (f.StartsWith("K"))&& not (f.StartsWith("P"))) then   //P -> Lable영역 JMP
                        if(not (Regex.IsMatch(f, @"(K|H|E)([-.0-9A-Fa-f]+)$"))  //K 10진수 H 16진수 E 실수 타입
                        && not (Regex.IsMatch(f, @"(\S)(Z)(\d)"))
                        && not (Regex.IsMatch(f, @"("")"))) then                // "AB!!" 문자열 타입
                            if(not (newSymbol.ContainsKey(f)))
                            then 
                                if(cmd.Contains("$"))                           //명령어가 "$" string 관련 처리면
                                then
                                    let sym = makeSymbol f "" Variable.Kind.VAR_GLOBAL VarType.STRING
                                    newSymbol.Add (f, sym)
                                else
                                    let sym = makeSymbol f "" Variable.Kind.VAR_GLOBAL VarType.NONE
                                    newSymbol.Add (f, sym)
                                    )
                )

        newSymbol.Values

    let getUsingSymbols pou  (symbols:SymbolInfo seq)=
        let newSymbols = List<SymbolInfo>()
        let usingAddress =
            seq {
                for rung in pou.Rungs do
                    for line in rung do
                        yield! ArgumentOfLine line
            }   |> HashSet
        symbols
        |> Seq.filter (fun f -> usingAddress.Contains(f.OldAddress()))
        |> Seq.map createLocalSymbol
        |> newSymbols.AddRange

        newSymbols

    /// (조건=coil) seq 로부터 rung xml 들의 string 을 생성
    let generateRungs (pous:POUParseResult seq, symbols:SymbolInfo seq, usingDrirect:bool)=
        let errorLog =  ResizeArray<string>()
        let warningLog =  ResizeArray<string>()
        seq {
            for pou in pous do
                let mutable y = 0
                let mutable convertPass = true
                yield "<Program Task=\"스캔 프로그램\" LocalVariable=\"1\" Kind=\"0\" InstanceName=\"\" Comment=\"\" FindProgram=\"1\" Encrytption=\"\">"
                yield sprintf "%s" pou.Name
                yield "<Body>"
                yield "<LDRoutine>"
                let newSymbols = getUsingSymbols pou symbols
                let dicSym = newSymbols |> Seq.map (fun f-> f.OldAddress(), (f.Address, f.Name)) |> dict
                convertPass <- true
                for rung in pou.Rungs do
                    //rung 조건 작성
                    let xml, y' =  rungXml y rung dicSym usingDrirect
                    if(not convertOk)
                    then
                        convertPass <- false
                        //error rung 주석 작성
                        errorLog.Add(sprintf "***************************NG POU : %s***************************" pou.Name)
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
                        yield sprintf "\t<Rung BlockMask=\"0\"><Element ElementType=\"%d\" Coordinate=\"%d\">↑↑↑↑↑↑변환경고↑↑↑↑↑↑\r\n%s</Element></Rung>" 
                                (int ElementType.RungCommentMode) yy (warningHistory|> String.concat "\r\n")
                        y <- y + 1
                        warningHistory.Clear()

                //if(convertPass)
                //then errorLog.Add(sprintf "***************************OK POU : %s(rungs : %d)****************" pou.Name (pou.Rungs.length()))

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
                            | _ -> failwithlog "Unknown"
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
    let getAllSymbols (pous, commentDic:CommentDictionary) =
        let Symbols = getGlobalSymbols commentDic
        /// 2-1. comment 에 없는 new Address 추가
        let newSymbols = generateNewSymbol pous commentDic
        let total = newSymbols |> Seq.append Symbols

        total, newSymbols

    /// POU를  XML 문자열 로 변환
    let getXgiPou(pous, symbols, usingDrirect) =
        let rungsXml, errLog, warnLog = generateRungs (pous, symbols, usingDrirect)
        rungsXml, errLog, warnLog

    /// POU를  XML 로 저장
    let getXgiXMLPou(rungsXml:string, symbols, usingDrirect) =
        let baseXml = XgiBaseXML.createXgiXml()
        let symbolsGlobalXml = generateSymbols symbols true usingDrirect

        let result = wrapWithXml (programsXml rungsXml) symbolsGlobalXml baseXml
        result |> String.concat "\r\n"

    /// 파일을 XML 문자열 로 변환
    let getXgiXML(files, usingDrirect) =
        let pous, commentDic = parseCSVs files
        let symbols, newSym = getAllSymbols (pous, commentDic)
        let baseXml = XgiBaseXML.createXgiXml()

        /// 1. Rungs 의 XML 문자열(with local var)
        let rungsXml, errLog, warnLog = generateRungs (pous, symbols, usingDrirect)
        /// 2. Symbol table 정의 XML 문자열
        let symbolsGlobalXml = generateSymbols symbols true usingDrirect

        /// 1+2 XGI XML 만들기
        // let result = XgiFile.wrapWithXml rungsXml "" baseXml
        let result = wrapWithXml (programsXml rungsXml) symbolsGlobalXml baseXml
        result, errLog, warnLog