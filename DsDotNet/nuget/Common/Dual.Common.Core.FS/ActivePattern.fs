namespace Dual.Common.Core.FS

open System
open System.Text.RegularExpressions
open System.Globalization
open System.Collections.Generic

[<AutoOpen>]
module Parse =
    /// b 가 true 이면 Some v 값을, false 이면 None 을 반환
    ///
    /// - TryParse 종류의 함수 결과값을 option 으로 변환하기 위해 주로 사용
    let inline tryToOption (b, v) = if b then Some v else None
    /// b 가 true 이면 Some v 값을, false 이면 None 을 반환
    let inline tryParseToOption (b, v) = if b then Some v else None

    /// Attempts to parse a string into an int and returns Some upon success and None upon failure.
    let TryInt (s:string) = System.Int32.TryParse(s) |> tryToOption
    /// Attempts to parse a string into a double and returns Some upon success and None upon failure.
    let TryDouble (s:string) = System.Double.TryParse(s) |> tryToOption
    /// Attempts to parse a string into a decimal and returns Some upon success and None upon failure.
    let TryDecimal (s:string) = System.Decimal.TryParse(s) |> tryToOption
    /// Attempts to parse a string into a DateTime and returns Some upon success and None upon failure.
    let TryDate (s:string) = System.DateTime.TryParse(s) |> tryToOption

    let TryBool (s:string) = System.Boolean.TryParse(s) |> tryToOption

    /// Returns true if the string is parsable into an int.
    let IsInt (s:string) = System.Int32.TryParse(s) |> fst
    /// Returns true if the string is parsable into a double.
    let IsDouble (s:string) = System.Double.TryParse(s) |> fst
    /// Returns true if the string is parsable into a decimal.
    let IsDecimal (s:string) = System.Decimal.TryParse(s) |> fst
    /// Returns true if the string is parsable into a DateTime.
    let IsDate (s:string) = System.DateTime.TryParse(s) |> fst


[<AutoOpen>]
module ActivePattern =
    /// Active pattern for Functional F# list
    let (|FList|) xs = List.ofSeq xs
    /// Active pattern for array
    let (|Array|) xs = Array.ofSeq xs
    /// Active pattern for ResizeArray (C# list)
    let (|ResizeArray|) (xs:'x seq) = xs |> System.Collections.Generic.List<'x>
    /// Active pattern for HashSet
    let (|Hash|) (xs:'X seq) = xs |> HashSet

    /// Active pattern for Key part of KeyValuePair
    let (|KeyOfKeyValue|) (kv:KeyValuePair<'k, 'v>) = kv.Key
    /// Active pattern for Value part of KeyValuePair
    let (|ValueOfKeyValue|) (kv:KeyValuePair<'k, 'v>) = kv.Value

    /// KeyValue 쌍에서 key 값 반환
    let keyOfKeyValue kv = (|KeyOfKeyValue|) kv
    /// KeyValue 쌍에서 value 값 반환
    let valueOfKeyValue kv = (|ValueOfKeyValue|) kv

    /// Sequence 의 Head, Tail pair 반환.
    /// - Head : 첫 번째 element
    /// - Tail : 첫 번째 제외한 나머지 elements
    let (|HeadAndTail|_|) (FList(xs)) =
        match xs with
        | [] -> None
        | h::ts -> Some(h, ts)


    /// Sequence 의 Init, Last pair 반환
    /// - Init : 마지막 제외한 나머지 elements
    /// - Last : 마지막 element
    let (|InitAndLast|_|) (FList(xs)) =     // (|InitAndLast|_|) [1..10] ==> Some ([1; 2; 3; 4; 5; 6; 7; 8; 9], 10)
        match xs with
        | [] -> None
        | _ ->
            let rev = List.rev xs
            Some(List.rev rev.Tail, rev.Head)


    /// Sequence 의 Head, Last pair 반환.
    /// - Head : 첫 번째 element
    /// - Last : 마지막 element
    let (|HeadAndLast|_|) (FList(xs)) =
        match xs with
        | [] -> None
        | x::[] -> None
        | h::ts -> Some(h, List.last ts)

    /// sequence 를 head 와 tails 로 분할
    ///
    /// - Head : 첫 번째 element
    ///
    /// - Tail : 첫 번째 제외한 나머지 elements
    let tryHeadAndTail xs = (|HeadAndTail|_|) xs
    [<Obsolete("Use tryHeadAndTail, instead")>]
    let headAndTail xs = (|HeadAndTail|_|) xs

    /// sequence 를 inits 과 last 로 분할
    ///
    /// - Init : 마지막 제외한 나머지 elements.
    ///
    /// - Last : 마지막 element
    let tryInitAndLast xs = (|InitAndLast|_|) xs
    [<Obsolete("Use tryInitAndLast, instead")>]
    let initAndLast xs = (|InitAndLast|_|) xs


    /// sequence 를 head 와 last 로 분할
    ///
    /// - Head : 첫 번째 element
    ///
    /// - Last : 마지막 element
    let tryHeadAndLast xs = (|HeadAndLast|_|) xs
    [<Obsolete("Use tryHeadAndLast, instead")>]
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

    /// Regex Match 성공하면 Some(), 실패하면 None 값 반환
    let (|RegexMatches|_|) (pattern:string) (x:string) = if Regex.IsMatch(x, pattern) then Some() else None

    /// 문자열 haystack 이 문자열 needle로 시작하면 Some(), 아니면 None 값 반환
    let (|StartsWith|_|) (needle:string) (haystack : string) = if haystack.StartsWith(needle) then Some() else None
    /// 문자열 haystack 이 문자열 needle로 끝나면 Some(), 아니면 None 값 반환
    let (|EndsWith|_|) (needle:string) (haystack : string) = if haystack.EndsWith(needle) then Some() else None
    /// x, y 가 같으면 Some(), 아니면 None 값 반환
    let (|Equals|_|) x y = if x = y then Some() else None
    /// 문자열 haystack 에 문자열 needle이 포함되면 Some(), 아니면 None 값 반환
    let (|ContainsSubstring|_|) (needle:string) (haystack : string) = if haystack.Contains(needle) then Some() else None

    /// xs 에 x 가 포함되면 Some(), 아니면 None 값 반환
    let (|Contains|_|) x xs =
        if Seq.contains x xs then Some () else None

    let (|Try|_|) (f: 'a -> 'b option) a = f a

    /// Option.ofObj 적용
    ///
    /// C#: var font = shape?.TextFrame?.TextRange?.Font
    ///
    /// F#:
        (*
        let font = 
            match shape with
            | OfObj s -> 
                match s.TextFrame with
                | OfObj tf -> 
                    match tf.TextRange with
                    | OfObj tr -> Some tr.Font
                    | _ -> None
                | _ -> None
            | _ -> None
        *)
    let (|OfObj|_|) x = Option.ofObj x

    /// Dictionary dic에 key 가 포함되면 Some value, 아니면 None 값 반환
    let (|ContainedInDictionary|_|) (dic:IDictionary<'k,'v>) key =
        let success, value = dic.TryGetValue key
        if success then Some value else None

    /// xs 에 x 가 포함되면 Some(), 아니면 None 값 반환
    let (|ContainedIn|_|) xs x = if Seq.contains x xs then Some() else None

    /// Matches strings that are parsable to an int.
    let (|Int|_|) = Parse.TryInt
    /// Matches strings that are parsable to a double.
    let (|Double|_|) = Parse.TryDouble
    /// Matches strings that are parsable to a decimal.
    let (|Decimal|_|) = Parse.TryDecimal
    /// Matches strings that are parsable to a DateTime.
    let (|Date|_|) = Parse.TryDate

    /// 날짜 pattern 이면 Some(DateTime), 아니면 None 값 반환
    let (|DatePattern|_|) (input : string) =
        match DateTime.TryParse(input) with
        | true, v -> Some(v)
        | _ -> None

    /// int32 pattern 이면 Some(int), 아니면 None 값 반환
    let (|Int32Pattern|_|) (str: string) =
        match System.Int32.TryParse(str) with
        | true, v -> Some(v)
        | _ -> None

    /// float pattern 이면 Some(float), 아니면 None 값 반환
    let (|FloatPattern|_|) (str: string) =
        match System.Double.TryParse(str) with
        | true, v -> Some(v)
        | _ -> None

    /// hexadecimal pattern 이면 Some(int), 아니면 None 값 반환.  hexadecimal 문자열을 int 값으로 변환한 값
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
        let regex_ip =
            let ff = "([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])"
            $"^{ff}\.{ff}\.{ff}\.{ff}$"
        match str with
        | RegexPattern regex_ip ip -> Some (str)
        | _ -> None

    // 추후 재정의 됨..
    let private ofBool t (boolValue:bool) =
        if boolValue then Some t else None

    /// Open ~ Open ragne
    let (|InRangeOO|_|) a b x = (a < x && x < b) |> ofBool InRangeOO
    /// Open ~ Close ragne
    let (|InRangeOC|_|) a b x = (a < x && x <= b) |> ofBool InRangeOC
    /// Close ~ Open ragne
    let (|InRangeCO|_|) a b x = (a <= x && x < b) |> ofBool InRangeCO
    /// Close ~ Close ragne
    let (|InRangeCC|_|) a b x = (a <= x && x <= b) |> ofBool InRangeCC


    /// range 사이에 위치하는지 검사 (a < x < b)
    let (|InOpenRange|_|) a b x = (|InRangeOO|_|) a b x
    /// range 사이에 위치하는지 검사 (a < x < b)
    let (|Between|_|) a b x = (|InRangeOO|_|) a b x

    /// range 사이에 위치하는지 검사 (a <= x <= b)
    let (|InClosedRange|_|) a b x = (|InRangeCC|_|) a b x

    /// [<Flag>]로 정의된 enum 에 flag 존재 여부 반환
    let (|HasFlag|_|) (flag:Enum) (enum:Enum) = enum.HasFlag(flag) |> ofBool


    /// Default argument 를 위한 active pattern
    // https://stackoverflow.com/questions/30255271/how-to-set-default-argument-value-in-f
    let inline (|Default|) defaultValue input =
        defaultArg input defaultValue

    [<Obsolete("Use EnsureNotNull")>]
    let inline (|NotNull|) v = failwith "Obsoleted.  Use EnsureNotNull instead"

    // https://riptutorial.com/fsharp/example/14889/active-patterns-can-be-used-to-validate-and-transform-function-arguments
    let inline (|EnsureNotNull|) v =
        match v with
        | null  -> raise (NullReferenceException "Should be non null."(* + nameof(v)*))
        | _ -> v

module private ParseTestSample =
    let private do_test() =
        let verify x = if not x then failwith "ERROR"
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
          | InRangeOO 0 5 -> printfn "Passed1"
          | Between 6 8 -> printfn "Passed2"
          | ContainedIn listxs  -> printfn "passed by list comparison"
          | Between 9 11 -> printfn "Passed3"
          | Between 3 15 -> printfn "Will not be called"
          | _ -> printfn "Outside range"
        ()
