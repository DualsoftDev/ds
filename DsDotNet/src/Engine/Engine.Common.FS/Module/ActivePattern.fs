namespace Engine.Common.FS

open System
open System.Text.RegularExpressions
open System.Globalization
open System.Collections.Generic

[<AutoOpen>]
module Parse =
    let inline private tryToOption (s, v) = if s then Some v else None

    /// Attempts to parse a string into an int and returns Some upon success and None upon failure.
    let Int s = System.Int32.TryParse(s) |> tryToOption
    /// Attempts to parse a string into a double and returns Some upon success and None upon failure.
    let Double s = System.Double.TryParse(s) |> tryToOption
    /// Attempts to parse a string into a decimal and returns Some upon success and None upon failure.
    let Decimal s = System.Decimal.TryParse(s) |> tryToOption
    /// Attempts to parse a string into a DateTime and returns Some upon success and None upon failure.
    let Date s = System.DateTime.TryParse(s) |> tryToOption

    /// Returns true if the string is parsable into an int.
    let IsInt s = System.Int32.TryParse(s) |> fst
    /// Returns true if the string is parsable into a double.
    let IsDouble s = System.Double.TryParse(s) |> fst
    /// Returns true if the string is parsable into a decimal.
    let IsDecimal s = System.Decimal.TryParse(s) |> fst
    /// Returns true if the string is parsable into a DateTime.
    let IsDate s = System.DateTime.TryParse(s) |> fst


[<AutoOpen>]
module ActivePattern =
    /// Active pattern for Functional F# list
    let (|FList|) xs = List.ofSeq xs
    let (|Array|) xs = Array.ofSeq xs
    let (|ResizeArray|) (xs:'x seq) = xs |> System.Collections.Generic.List<'x>
    let (|Hash|) (xs:'X seq) = xs |> HashSet

    let (|KeyOfKeyValue|) (kv:KeyValuePair<'k, 'v>) = kv.Key
    let (|ValueOfKeyValue|) (kv:KeyValuePair<'k, 'v>) = kv.Value

    let keyOfKeyValue kv = (|KeyOfKeyValue|) kv
    let valueOfKeyValue kv = (|ValueOfKeyValue|) kv

    /// Sequence 의 Head, Tail pair 반환
    let (|HeadAndTail|_|) (FList(xs)) =
        match xs with
        | [] -> None
        | h::ts -> Some(h, ts)


    /// Sequence 의 Init, Last pair 반환
    let (|InitAndLast|_|) (FList(xs)) =     // (|InitAndLast|_|) [1..10] ==> Some ([1; 2; 3; 4; 5; 6; 7; 8; 9], 10)
        match xs with
        | [] -> None
        | _ ->
            let rev = List.rev xs
            Some(List.rev rev.Tail, rev.Head)


    let (|HeadAndLast|_|) (FList(xs)) =
        match xs with
        | [] -> None
        | x::[] -> None
        | h::ts -> Some(h, List.last ts)

    let headAndTail xs = (|HeadAndTail|_|) xs
    let initAndLast xs = (|InitAndLast|_|) xs
    let headAndLast xs = (|HeadAndLast|_|) xs


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

    let (|RegexMatches|_|) (pattern:string) x = if Regex.IsMatch(x, pattern) then Some() else None

    let (|StartsWith|_|) (needle:string) (haystack : string) = if haystack.StartsWith(needle) then Some() else None
    let (|EndsWith|_|) (needle:string) (haystack : string) = if haystack.EndsWith(needle) then Some() else None
    let (|Equals|_|) x y = if x = y then Some() else None
    let (|Contains|_|) (needle:string) (haystack : string) = if haystack.Contains(needle) then Some() else None


    let (|Try|_|) (f: 'a -> 'b option) a = f a
    let (|ContainedInDictionary|_|) (dic:IDictionary<'k,'v>) key =
        let success, value = dic.TryGetValue key
        if success then Some value else None
    let (|ContainedInSet|_|) xs x = if Set.contains x xs then Some() else None
    let (|ContainedInHashSet|_|) (xs:HashSet<'a>) (x:'a) = if xs.Contains x then Some() else None
    let (|ContainedInList|_|) xs x = if List.contains x xs then Some() else None
    let (|ContainedInArray|_|) xs x = if Array.contains x xs then Some() else None

    /// Matches strings that are parsable to an int.
    let (|Int|_|) = Parse.Int
    /// Matches strings that are parsable to a double.
    let (|Double|_|) = Parse.Double
    /// Matches strings that are parsable to a decimal.
    let (|Decimal|_|) = Parse.Decimal
    /// Matches strings that are parsable to a DateTime.
    let (|Date|_|) = Parse.Date

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

    let (|HexPattern|_|) (str: string) =
        match Int32.TryParse(str, NumberStyles.HexNumber, CultureInfo.CurrentCulture) with
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


    let (|HasFlag|_|) (flag:Enum) (enum:Enum) =
        if enum.HasFlag(flag) then Some HasFlag else None


    /// Default argument 를 위한 active pattern
    // https://stackoverflow.com/questions/30255271/how-to-set-default-argument-value-in-f
    let inline (|Default|) defaultValue input =
        defaultArg input defaultValue

    // https://riptutorial.com/fsharp/example/14889/active-patterns-can-be-used-to-validate-and-transform-function-arguments
    let inline (|NotNull|) v =
        match v with
        | null  -> raise (NullReferenceException "Should be non null."(* + nameof(v)*))
        | _ -> v

    let private do_test() =
        [ "kwak@dualsoft.co.kr"; "http://dualsoft.co.kr"; "192.168.0.4" ; "256" ]
            |> Seq.iter(fun str ->
                match str with
                | EmailPattern email -> printfn "Email:%s" email
                | UrlPattern url -> printfn "URL:%s" url
                | IpPattern url -> printfn "Ip:%s" url
                | _ -> printfn "None:%s" str
            )

        let incrInts (FList(xs:int list)) =
            xs |> List.map (fun n -> n + 1)
        [|1..10|]   |> incrInts = [1..10] |> verify
        seq {1..10} |> incrInts = [1..10] |> verify


        let listxs = [9..11]
        match 10 with
          | Between 0 5 -> printfn "Passed1"
          | Between 6 8 -> printfn "Passed2"
          | ContainedInList listxs  -> printfn "passed by list comparison"
          | Between 9 11 -> printfn "Passed3"
          | Between 3 15 -> printfn "Will not be called"
          | _ -> printfn "Outside range"

