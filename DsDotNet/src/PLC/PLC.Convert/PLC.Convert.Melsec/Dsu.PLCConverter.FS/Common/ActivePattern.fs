module Dsu.PLCConverter.FS.ActivePattern

open System
open System.Text.RegularExpressions
open Microsoft.FSharp.Reflection
open Microsoft.FSharp.Quotations.Patterns
open System.Collections.Generic

/// RegexPattern parses a regular expression and returns a list of the strings that match each group in
/// the regular expression.
/// List.tail is called to eliminate the first element in the list, which is the full matched expression,
/// since only the matches for each group are wanted.
let (|RegexPattern|_|) regex str =
    let m = Regex(regex).Match(str)
    if m.Success then Some (List.tail [ for x in m.Groups -> x.Value ])
    else None

let (|FirstRegexGroupPattern|_|) pattern input =
    let m = Regex(pattern).Match(input)
    if m.Success then Some m.Groups.[1].Value
    else None

let (|StartsWith|_|) needle (haystack : string) = if haystack.StartsWith(needle) then Some() else None
let (|EndsWith|_|) needle (haystack : string) = if haystack.EndsWith(needle) then Some() else None
let (|Equals|_|) x y = if x = y then Some() else None
let (|Contains|_|) needle (haystack : string) = if haystack.Contains(needle) then Some() else None

let (|DatePattern|_|) (input : string) =
    match DateTime.TryParse(input) with
    | true, v -> Some(v)
    | _ -> None

let (|Int32Pattern|_|) (str: string) =
    match System.Int32.TryParse(str) with
    | true, v -> Some(v)
    | _ -> None

let (|FloatPattern|_|) (str: string) =
    match System.Double.TryParse(str) with
    | true, v -> Some(v)
    | _ -> None


let (|EmailPattern|_|) (str: string) =
    let regex_email = "(^\w+([-+.']\w+)*)@(\w+([-.]\w+)*\.\w+([-.]\w+)*)$"
    match str with
    | RegexPattern regex_email email -> Some (str)
    | _ -> None

let (|UrlPattern|_|) (str: string) =
    let regex_url = "^(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&%\$#_]*)?$"
    match str with
    | RegexPattern regex_url url -> Some (str)
    | _ -> None

let (|IpPattern|_|) (str: string) =
    let regex_ip = "^([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$"
    match str with
    | RegexPattern regex_ip ip -> Some (str)
    | _ -> None

/// range 사이에 위치하는지 검사 (a < x < b)
let (|Between|_|) a b x =
    if  a < x && x < b then
        Some Between
    else
        None

/// range 사이에 위치하는지 검사 (a <= x <= b)
let (|InClosedRange|_|) a b x =
    if  a <= x && x <= b then
        Some InClosedRange
    else
        None



let private do_test() =
    [ "kwak@dualsoft.co.kr"; "http://dualsoft.co.kr"; "192.168.0.4" ; "256" ]
        |> Seq.iter(fun str ->
            match str with
            | EmailPattern email -> printfn "Email:%s" email
            | UrlPattern url -> printfn "URL:%s" url
            | IpPattern url -> printfn "Ip:%s" url
            | _ -> printfn "None:%s" str
        )

    match 10 with
      | Between 0 5 -> printfn "Passed1"
      | Between 6 8 -> printfn "Passed2"
      | Between 9 11 -> printfn "Passed3"
      | Between 3 15 -> printfn "Will not be called"
      | _ -> printfn "Outside range"



// http://www.fssnip.net/9l/title/toString-and-fromString-for-discriminated-unions
let toString (x:'a) = 
    match FSharpValue.GetUnionFields(x, typeof<'a>) with
    | case, _ -> case.Name

let fromString<'a> (s:string) =
    match FSharpType.GetUnionCases typeof<'a> |> Array.filter (fun case -> case.Name = s) with
    |[|case|] -> Some(FSharpValue.MakeUnion(case,[||]) :?> 'a)
    |_ -> None

    

//https://stackoverflow.com/questions/3363184/f-how-to-elegantly-select-and-group-discriminated-unions/3365084#3365084
/// UnionCase 판정
/// e.g isUnionCase<@ OnOffAction @> action => action  이 OnOffAction 인지 판정 
/// e.g isUnionCase<@ OnOffAction, PLCAction @> action => action  이 OnOffAction 이거나 PLCAction 인지 판정 
let rec isUnionCase = function
    | Lambda (_, expr) | Let (_, _, expr) -> isUnionCase expr
    | NewTuple exprs -> 
        let iucs = List.map isUnionCase exprs
        fun value -> List.exists ((|>) value) iucs
    | NewUnionCase (uci, _) ->
        let utr = FSharpValue.PreComputeUnionTagReader uci.DeclaringType
        box >> utr >> (=) uci.Tag
    | _ -> failwithf "Expression is no union case."




[<AutoOpen>]
module StringExt =
    type String with
        member x.SplitByLine(splitOption:StringSplitOptions) = x.Split([|'\r'; '\n'|], splitOption)
        member x.SplitByLine() = x.SplitByLine(StringSplitOptions.RemoveEmptyEntries)
        member x.SplitBy(separator:string, splitOption:StringSplitOptions) = x.Split([|separator|], splitOption)
        member x.SplitBy(separator:string) = x.SplitBy(separator, StringSplitOptions.RemoveEmptyEntries)
        member x.SplitBy(separator:char, splitOption:StringSplitOptions) = x.Split([|separator|], splitOption)
        member x.SplitBy(separator:char) = x.SplitBy(separator, StringSplitOptions.RemoveEmptyEntries)

    let split (text:string) (seperators:char array) (splitOption:StringSplitOptions) =
        text.Split(seperators, splitOption)
    let splitByLines (text:string) = text.SplitByLine()
    let splibBy(text:string) (seperator:char) = text.SplitBy(seperator)


    

[<RequireQualifiedAccess>]
module Tuple =
    // https://stackoverflow.com/questions/2920094/how-can-i-convert-between-f-list-and-f-tuple
    let toSeq t = 
        if FSharpType.IsTuple(t.GetType()) 
            then FSharpValue.GetTupleFields t |> seq
            else Seq.empty

    let ofSeq sequ =
        let arr = sequ |> Array.ofSeq
        let types = arr |> Array.map (fun o -> o.GetType())
        let tupleType = FSharpType.MakeTupleType types
        FSharpValue.MakeTuple (arr , tupleType)

    #if INTERACTIVE
    (1,2,3) |> Tuple.toSeq  // => Some [1; 2; 3]
    [1..3] |> List.map box |> Tuple.ofSeq   // => (1, 2, 3)
    #endif

    // https://stackoverflow.com/questions/27924235/f-how-to-provide-fst-for-tuples-triples-and-quadruples-without-sacrifying-ru/27929600
    type Tuple1st = Tuple1st with
        static member ($) (Tuple1st, (x1,_)) = x1
        static member ($) (Tuple1st, (x1,_,_)) = x1
        static member ($) (Tuple1st, (x1,_,_,_)) = x1
        static member ($) (Tuple1st, (x1,_,_,_,_)) = x1
        // more overloads
    type Tuple2nd = Tuple2nd with
        static member ($) (Tuple2nd, (_,x1)) = x1
        static member ($) (Tuple2nd, (_,x1,_)) = x1
        static member ($) (Tuple2nd, (_,x1,_,_)) = x1
        static member ($) (Tuple2nd, (_,x1,_,_,_)) = x1
        // more overloads
    type Tuple3rd = Tuple3rd with
        static member ($) (Tuple3rd, (_,_,x1)) = x1
        static member ($) (Tuple3rd, (_,_,x1,_)) = x1
        static member ($) (Tuple3rd, (_,_,x1,_,_)) = x1
        // more overloads
    type Tuple4th = Tuple4th with
        static member ($) (Tuple4th, (_,_,_,x1)) = x1
        static member ($) (Tuple4th, (_,_,_,x1,_)) = x1
        // more overloads

    let inline tuple1st x = Tuple1st $ x
    let inline tuple2nd x = Tuple2nd $ x
    let inline tuple3rd x = Tuple3rd $ x
    let inline tuple4th x = Tuple4th $ x

    let keyValuePairToTuple (kv:KeyValuePair<'t1, 't2>) =
        kv.Key, kv.Value




