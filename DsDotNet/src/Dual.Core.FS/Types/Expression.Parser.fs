// https://github.com/mattchanner/expression-parser-fparsec/blob/master/MC.Expression.Parser/Expressions.Parser.fs

namespace Dual.Core

open FParsec
open Dual.Common
open Dual.Core.Types



/// A private module containing the main parser implementation
module internal ParserImpl =
    let all, allRef = createParserForwardedToRef()

    (* Helpers *)
    /// Shorthand for parsing an expected string
    let str s = pstring s

    /// Represents a line comment
    let comment =  str "#" >>. skipRestOfLine true .>> spaces

    /// Shorthand for skipping 0 to many white space characters
    let ws = comment <|> spaces
    let str_ws str = skipString str >>. ws

    // succeeds when p is applied between strings s1 and s2
    let betweenStrings s1 s2 p = p |> between (str s1) (str s2)

    /// Applies a parser to return a list of items between the open and close strings.
    let listBetweenStrings sOpen sClose pElement f =
        between (str sOpen) (str sClose)
                (ws >>. sepBy (pElement .>> ws) (str "," .>> ws) |>> f)

    /// Applies a parser to return a list of items between the open and close strings.
    /// sOpen 과 sClose 사이에 존재하는 문자열을 separator 로 분리하여 pElement parser 를 적용한 결과를 f 적용
    let listBetweenStrings1SepBy sOpen sClose separator pElement f =
        between (str sOpen) (str sClose)
                (ws >>. sepBy1 (pElement .>> ws) (str separator .>> ws) |>> f)

    /// Applies a parser to return a list of items between the open and close strings.
    /// sOpen 과 sClose 사이에 존재하는 문자열을 "," 로 분리하여 pElement parser 를 적용한 결과를 f 적용
    let listBetweenStrings1 sOpen sClose pElement f = listBetweenStrings1SepBy sOpen sClose "," pElement f

    /// Creates a parser to parse an identifier - this must start with a letter or underscore, but can then be followed
    /// by any number of letters, numbers of underscores
    let ident: Parser<identifier, _> =
        let isValidStart c = isLetter c || c = '_'
        let isValidIdent c = isLetter c || isDigit c || ['_'; '.'; '+'; '-'] |> Seq.contains c
        (many1Satisfy2L isValidStart isValidIdent "identifier")

    /// Creates a parser to parse any character up until the end char.  This can include whitespaces
    let anyCharsUntil charEnd = manySatisfy (fun c -> c <> charEnd)

    /// Creates a parser to parse any valid identifier, returning the result as an Ident
    let xident = parse {
        let! ident = ident
        return Terminal(PseudoTerminalM.PseudoTerminal(ident))
    }

    /// Creates a parser to parse a list of expressions enclosed in round brackets, returning the
    /// result as an Args type. This parser will only succeed when the arg list has at least one element
    //let xplccode =
    //    betweenStrings "@[" "]" (anyCharsUntil ']') |>> fun x -> PLCCode(x)
    let xplccode =
        let p = pipe5 (pstring "@") ident (ws >>. pstring "[") (anyCharsUntil ']') (pstring "]") (fun _ command _ code _ -> command, code.Trim())
        ws >>. p .>> ws

    /// A combined parser to parse all primitive types
    let xprim = choice [xident]

    /// Configure the operator precedence parser to handle complex expressions
    let oppa = new OperatorPrecedenceParser<Expression, unit, unit>()

    let parithmetic = oppa.ExpressionParser

    oppa.TermParser <- (xprim .>> ws) <|> between (str "(" .>> ws) (str ")" .>> ws) parithmetic

    type Assoc = Associativity

    /// Binary Operators
    oppa.AddOperator(InfixOperator("&", ws, 3, Assoc.Left,  fun x y -> Binary(x, Op.And,     y)))
    oppa.AddOperator(InfixOperator("|", ws, 3, Assoc.Left,  fun x y -> Binary(x, Op.Or,      y)))

    /// Unary Operators
    oppa.AddOperator(PrefixOperator("!", ws, 6, true, fun x -> Unary(Op.Neg, x)))


    /// Logical Binary Operators
    let oppc = new OperatorPrecedenceParser<Expression, unit, unit>()
    let pcomparison = oppc.ExpressionParser
    let termc = (parithmetic .>> ws) <|> between (str "(" .>> ws) (str ")" .>> ws) pcomparison
    oppc.TermParser <- termc


    // do exprRef := opp.ExpressionParser
    allRef := oppc.ExpressionParser

    let statements = choice[ all ]

    // The full parser, terminating with an EOF marker
    let xparser = ws >>. statements .>> ws .>> eof

    let expressionParser = ws >>. all .>> ws .>> eof

/// The public parser module
module Parser =
    let private parser = ParserImpl.xparser
    let private expressionParser = ParserImpl.expressionParser
    let private processParser = ParserImpl.xparser

    /// Represents the result of the parse operation
    type ParseResult(expr: Expression option, msg: string option) =

        /// Gets the expression as an option type
        member x.Expression = expr

        /// Gets the parser error message as an option type
        member x.ParseError = msg

        /// Returns a value indicating whether the parse result is OK or not.
        member x.Ok = expr.IsSome

        member x.ToResult() =
            match x.Expression, x.ParseError with
            | Some(expr), _   -> Result.Ok expr
            | None, Some(msg) -> Result.Error msg
            | _               -> failwithf "ERROR"

    /// Parse the given expression, returning a ParseResult
    let ParseString expr =
        match run parser expr with
        | Success(expr, _, _) -> ParseResult(Some(expr), None)
        | Failure(msg, _, _)  -> ParseResult(None, Some(msg))

    let ParseExpression expr =
        match run expressionParser expr with
        | Success(expr, _, _) -> ParseResult(Some(expr), None)
        | Failure(msg, _, _)  -> ParseResult(None, Some(msg))

    let ParseProcess proc =
        match run processParser proc with
        | Success(expr, _, _) -> ParseResult(Some(expr), None)
        | Failure(msg, _, _)  -> ParseResult(None, Some(msg))

    let TestParse expr =
        printf "=== %s\n" expr
        match run parser expr with
        | Success(expr, a, b) -> printf "%A\n\n" expr
        | Failure(msg, _, _)  -> printf "%s" msg

module ParserAlgo =
    /// 문자열로 주어진 수식을 parsing 해서 Expr option 으로 변환
    let parseExpression (exp:string) =
        if exp = null || exp = "" then
            Result.Error "Empty input."
        else
            let parseResult = Parser.ParseExpression exp
            parseResult.ToResult()


    /// module abbreviation
    module P = ParserImpl

// TODO : Gamma
#if false
    /// String type(from Json) 의 Process 기술 항목을 분석해서 PUnit list option 로 반환
    /// PUnit
    ///     Action
    ///         OnOffAction, ParallelActions, PLCAction
    ///     Reference
    let parseProcessToOption (proc:string) =
        /// 켜고 끄기 구문 parser
        let onOffParser = 
            let toAction nf = Action(OnOffAction(nf))
            let pOn1 = pstring "+" >>. P.ident |>> (fun ident -> toAction (TurnOn(PseudoTerminal(ident))))
            let pOn2 = P.ident |>> (fun ident -> toAction(TurnOn(PseudoTerminal(ident))))
            let pOff = pstring "-" >>. P.ident |>> (fun ident -> toAction(TurnOff(PseudoTerminal(ident))))
            pOn1 <|> pOn2 <|> pOff
        //let referenceParser = P.listBetweenStrings "(" ")" P.all id
        let betweenStrings' s1 s2 p = p |> between (P.str_ws s1) (P.str_ws s2)
        /// 참조 구문 parser
        let referenceParser = betweenStrings' "(" ")" P.all |>> (fun x -> Reference(x))

        /// PLC action 구문 parser.  "@RESET[I_S300_2ND_CLAMP1_RET]" 등을 분석
        let plcActionParser = P.xplccode |>> (fun (cmd, param) -> Action(PLCAction(cmd, param)))

        /// parallel 구문 parser.  [] 내에 & 로 구분됨
        let parallelBlockParser =
            let parallelInnerParser = onOffParser <|> plcActionParser
            P.listBetweenStrings1SepBy "[" "]" "&" parallelInnerParser (fun x ->
                let parallels = 
                    x
                        |> Seq.map (fun u -> 
                            match u with
                            | Action(act) -> Some(act)
                            | _ -> None)
                        |> Seq.choose id
                        |> ResizeArray
                Action(ParallelActions(parallels)))

        /// Process 에 기술되는 모든 종류에 대한, 하나의 항목 parser
        let allPUnitParser = referenceParser <|> onOffParser <|> plcActionParser <|> parallelBlockParser

        /// 최종 Process parser.  모든 항목이 ',' 로 구분됨
        let processParser =
            sepBy1 ( P.ws >>. allPUnitParser .>> P.ws) (P.str "," .>> P.ws)

        if proc.isNullOrEmpty() then
            None
        else
            match run processParser proc with
            | Success(s, _, _) -> Some(s)
            | _ -> None

    /// String type(from Json) 의 Process 기술 항목을 분석해서 PUnit seq 로 반환
    /// PUnit
    ///     Action
    ///         OnOffAction, ParallelActions, PLCAction
    ///     Reference
    let parseProcess (proc:string) =
        match parseProcessToOption proc with
        | Some(lst) -> lst |> seq
        | None -> empty
#endif

