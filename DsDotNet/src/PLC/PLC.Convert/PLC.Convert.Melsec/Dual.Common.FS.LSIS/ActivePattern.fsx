open System.Text.RegularExpressions

// https://fsharpforfunandprofit.com/posts/convenience-active-patterns/

do
    printfn __SOURCE_DIRECTORY__



// Partial Active Patterns
// https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/active-patterns
// 
// Sometimes, you need to partition only part of the input space. In that case, you write 
// a set of partial patterns each of which match some inputs but fail to match other inputs. 
// Active patterns that do not always produce a value are called partial active patterns;  
// they have a return value that is an option type. To define a partial active pattern,  
// you use a wildcard character (_) at the end of the list of patterns inside the banana clips.  
// The following code illustrates the use of a partial active pattern.
//
let (|Integer|_|) (str: string) =
   match System.Int32.TryParse(str) with
   | true, v -> Some(v)
   | _ -> None

let (|Float|_|) (str: string) =
   match System.Double.TryParse(str) with
   | true, v -> Some(v)
   | _ -> None

let parseNumeric = function
     | Integer i -> printfn "%d : Integer" i
     | Float f -> printfn "%f : Floating point" f
     | str -> printfn "%s : Not matched." str

parseNumeric "1.1"
parseNumeric "0"
parseNumeric "0.0"
parseNumeric "10"
parseNumeric "Something else"

let (|Even|Odd|) n = if n % 2 = 0 then Even else Odd
match 3 with
| Even -> printfn "It's Even"
| Odd -> printfn "It's Odd"

open System.Text.RegularExpressions

// ParseRegex parses a regular expression and returns a list of the strings that match each group in
// the regular expression.
// List.tail is called to eliminate the first element in the list, which is the full matched expression,
// since only the matches for each group are wanted.
let (|ParseRegex|_|) regex str =
   let m = Regex(regex).Match(str)
   if m.Success
   then Some (List.tail [ for x in m.Groups -> x.Value ])
   else None

// Three different date formats are demonstrated here. The first matches two-
// digit dates and the second matches full dates. This code assumes that if a two-digit
// date is provided, it is an abbreviation, not a year in the first century.
let parseDate str =
   match str with
     | ParseRegex "(\d{1,2})/(\d{1,2})/(\d{1,2})$" [Integer m; Integer d; Integer y]
          -> new System.DateTime(y + 2000, m, d)
     | ParseRegex "(\d{1,2})/(\d{1,2})/(\d{3,4})" [Integer m; Integer d; Integer y]
          -> new System.DateTime(y, m, d)
     | ParseRegex "(\d{1,4})-(\d{1,2})-(\d{1,2})" [Integer y; Integer m; Integer d]
          -> new System.DateTime(y, m, d)
     | _ -> new System.DateTime()

let dt1 = parseDate "12/22/08"
let dt2 = parseDate "1/1/2009"
let dt3 = parseDate "2008-1-15"
let dt4 = parseDate "1995-12-28"









open System.Text.RegularExpressions
// create an *PARTIAL* active pattern
let (|FirstRegexGroup|_|) pattern input =
   let m = Regex(pattern).Match(input) 
   if (m.Success) then Some m.Groups.[1].Value else None  

// create a function to call the pattern
let testRegex str = 
    match str with
    | FirstRegexGroup "http://([^/]*).*" host -> 
           printfn "The value is a url and the host is %s" host
    | FirstRegexGroup ".*?@(.*)" host -> 
           printfn "The value is an email and the host is %s" host
    | _ -> printfn "The value '%s' is something else" str
   
// test
testRegex "http://google.com/test"
testRegex "http://google.com"
testRegex "alice@hotmail.com"





open System
// definition of the *COMPLETE* active pattern
let (|Bool|Int|Float|String|) input =
    // attempt to parse a bool
    let success, res = Boolean.TryParse input
    if success then Bool(res)
    else
        // attempt to parse an int
        let success, res = Int32.TryParse input
        if success then Int(res)
        else
            // attempt to parse a float (Double)
            let success, res = Double.TryParse input
            if success then Float(res)
            else String(input)
// function to print the results by pattern
// matching over the active pattern
let printInputWithType input =
    match input with
    | Bool b -> printfn "Boolean: %b" b
    | Int i -> printfn "Integer: %i" i
    | Float f -> printfn "Floating point: %f" f
    | String s -> printfn "String: %s" s

// print the results
printInputWithType "true"
printInputWithType "12"
printInputWithType "-12.1"
printInputWithType "Something else"







