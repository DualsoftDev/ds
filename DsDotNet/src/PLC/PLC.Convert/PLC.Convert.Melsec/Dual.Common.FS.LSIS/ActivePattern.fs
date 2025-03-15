module Dual.Common.FS.LSIS.ActivePattern

open System
open System.Text.RegularExpressions

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
