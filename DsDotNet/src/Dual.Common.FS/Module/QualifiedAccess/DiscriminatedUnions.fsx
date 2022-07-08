#I @"F:\Git\dual\soft\Delta\bin"
#r "../../obj/Debug/netcoreapp3.1/Dual.Common.FS.dll"

open System
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Reflection
open Dual.Common

let rec isUnionCase = function
    | Lambda (_, expr) | Let (_, _, expr) -> isUnionCase expr
    | NewTuple exprs -> 
        let iucs = List.map isUnionCase exprs
        fun value -> List.exists ((|>) value) iucs
    | NewUnionCase (uci, _) ->
        let utr = FSharpValue.PreComputeUnionTagReader uci.DeclaringType
        box >> utr >> (=) uci.Tag
    | _ -> failwithlog "Expression is no union case."


type SomeType =
    | SomeCase1
    | SomeCase2 of int
    | SomeCase3 of int * int
    | SomeCase4 of int * int * int
    | SomeCase5 of int * int * int * int

let list =
    [
        SomeCase1
        SomeCase2  1
        SomeCase3 (2, 3)
        SomeCase4 (4, 5, 6)
        SomeCase5 (7, 8, 9, 10)
    ]

list 
    |> List.filter (isUnionCase <@ SomeCase4 @>)
    |> printfn "Matching SomeCase4: %A"

list
    |> List.filter (isUnionCase <@ SomeCase3, SomeCase4 @>)
    |> printfn "Matching SomeCase3 || SomeCase4: %A"


// https://fsharpforfunandprofit.com/posts/enum-types/
type SizeUnion = Small | Medium | Large         // union : struct 기반
type ColorEnum = Red=0 | Yellow=1 | Blue=2      // enum : int 기반
type MyEnum = Yes = 'Y' | No ='N'  // Ok because char was used.
let redInt = int ColorEnum.Red  
let redEnum:ColorEnum = LanguagePrimitives.EnumOfValue(redInt)
let redAgain:ColorEnum = enum redInt // cast to a specified enum type 
let yellowAgain = enum<ColorEnum>(1) // or create directly
let allColors = Enum.GetValues(typeof<ColorEnum>)


type Distance = TwentyFive=25 | Fifty=50 | Hundred=100
printfn "%d" ((uint)Distance.TwentyFive)


module TestImplicitDiscriminatedUnion =
    // http://www.fssnip.net/7SO/title/Implicit-conversion-to-discriminated-union
    // Caution!
    // Don't use in real code.

    module Domain =
        type UnionType = 
            | Int of int
            | Long of int64
            | String of string
        with
            static member ($) (UnionType, x:int) = Int(x)
            static member ($) (UnionType, x:int64) = Long(x)
            static member ($) (UnionType, x:string) = String(x) 

        let inline (|UnionType|) x = Unchecked.defaultof<UnionType> $ x

    open Domain

    let show x = 
      match x with
      | Int x -> printfn "int: %d" x
      | Long x -> printfn "long: %d" x
      | String x -> printfn "string: %s" x

    let inline showImplicit (UnionType x) = show x

    showImplicit "Hello world!"
    showImplicit 23
    showImplicit 23L


module TestShow2 =
// https://francotiveron.wordpress.com/2018/08/29/f-a-trading-strategy-backtester-2/
type Shower =
    static member ($) (_:Shower, m:seq<_>) = printfn "Seq: %A" m
    static member ($) (_:Shower, n:int) = printfn "Int: %d" n
    static member ($) (_:Shower, d:float) = printfn "Float: %f" d
let inline show o = Unchecked.defaultof<Shower> $ o

show 1
show 1.0
show [1..10]





// #r "nuget: FSharpPlus" 
//open FSharpPlus

module TestFMap =
    // http://nut-cracker.azurewebsites.net/blog/2011/11/15/typeclasses-for-fsharp/
    // https://kwangyulseo.com/2015/01/21/emulating-haskell-type-classes-in-f/
    type Fmap = Fmap with
        static member ($) (Fmap, x:option<_>) = fun f -> Option.map f x
        static member ($) (Fmap, x:list<_>)   = fun f -> List.map f x
        static member ($) (Fmap, x:seq<_>)    = fun f -> Seq.map f x
        static member ($) (Fmap, x:array<_>)  = fun f -> Array.map f x
        static member ($) (Fmap, x:Result<_, _>)  = fun f -> Result.map f x
    let inline map f x = Fmap $ x <| f


    type Fbind = Fbind with
        static member ($) (Fbind, x:option<_>) = fun f -> Option.bind f x
        static member ($) (Fbind, x:list<_>)   = fun f -> List.collect f x
        static member ($) (Fbind, x:seq<_>)    = fun f -> Seq.collect f x
        static member ($) (Fbind, x:array<_>)  = fun f -> Array.collect f x
        static member ($) (Fbind, x:Result<_, _>)  = fun f -> Result.bind f x
    let inline bind f x = Fbind $ x <| f


    type Fmapi = Fmapi with
        static member ($) (Fmapi, x:list<_>)     = fun f -> List.mapi f x
        static member ($) (Fmapi, x:seq<_>)      = fun f -> Seq.mapi f x
        static member ($) (Fmapi, x:array<_>)    = fun f -> Array.mapi f x
    let inline mapi f x = Fmapi $ x <| f

    type Fbindi = Fbindi with
        static member ($) (Fbindi, x:list<_>)    = fun f -> List.mapi f x  |> List.collect id
        static member ($) (Fbindi, x:seq<_>)     = fun f -> Seq.mapi f x   |> Seq.collect id
        static member ($) (Fbindi, x:array<_>)   = fun f -> Array.mapi f x |> Array.collect id
    let inline bindi f x = Fbindi $ x <| f

    type Fchoosei = Fchoosei with
        static member ($) (Fchoosei, x:list<_>)  = fun f -> List.mapi f x  |> List.choose id
        static member ($) (Fchoosei, x:seq<_>)   = fun f -> Seq.mapi f x   |> Seq.choose id
        static member ($) (Fchoosei, x:array<_>) = fun f -> Array.mapi f x |> Array.choose id
    let inline choosei f x = Fchoosei $ x <| f

    type Fpicki = Fpicki with
        static member ($) (Fpicki, x:list<_>)    = fun f -> List.mapi f x  |> List.pick id
        static member ($) (Fpicki, x:seq<_>)     = fun f -> Seq.mapi f x   |> Seq.pick id
        static member ($) (Fpicki, x:array<_>)   = fun f -> Array.mapi f x |> Array.pick id
    let inline picki f x = Fpicki $ x <| f

    type Fiter = Fiter with
        static member ($) (Fiter, x:option<_>)   = fun f -> Option.iter f x
        static member ($) (Fiter, x:list<_>)     = fun f -> List.iter f x
        static member ($) (Fiter, x:seq<_>)      = fun f -> Seq.iter f x
        static member ($) (Fiter, x:array<_>)    = fun f -> Array.iter f x
        //static member ($) (Fiter, x:Result<_, _>)  = fun f -> Result.iter f x
    let inline iter f x = Fiter $ x <| f


    // https://withouttheloop.com/articles/2014-10-21-fsharp-adhoc-polymorphism/
    type A = { thing: int } with
        static member show a = sprintf "%A" a
    type B = { label: string } with
        static member show b = sprintf "%A" b

    let inline show (x:^t) =
        (^t: (static member show: ^t -> string) (x))
    // val inline show :
    //  x: ^t -> string when  ^t : (static member show :  ^t -> string)


    //> let inline twice x = x + x;;
    //val inline twice :
    //  x: ^a ->  ^b when  ^a : (static member ( + ) :  ^a *  ^a ->  ^b)


    { thing = 98 } |> show |> Console.WriteLine
    { label = "Car" } |> show |> Console.WriteLine




    let add x y = x + y
    let result = add 1.3 2.1


    let add1 = (+) 1
    let sq x = x*x
    let xs1 = [1..10] |> map sq
    let xs2 = (Some 5) |> map sq
    let xs3 = seq{1..5} |> map sq
    let aa = map ((+) 2) (Some 3)

    let even n = if n % 2 = 0 then Ok n else Error (sprintf "Odd %d" n)
    let resultOk = even 2 |> map add1
    let resultError = even 1|> map add1

    let split = fun (x:string) -> x.Split [|' '|]

    // List 입력인 경우, split 이 Array 를 반환하므로 binding 안됨. 
    // 다음 라인은 컴파일 안됨 : Type mismatch
    // [ "hello world good"; "by the way" ] |> bind split

    // split 에 맞게 입력을 array 로 변경하면 OK
    // val it : string [] = [|"hello"; "world"; "good"; "by"; "the"; "way"|]
    [| "hello world good"; "by the way" |] |> bind split
    [| "hello world good"; "by the way" |] |> bindi (fun n x -> if n % 2 = 0 then split x else [|"NONE"|])

    // Seq 는 array 와 공존가능하므로 ok
    // val it : seq<string> = seq ["hello"; "world"; "good"; "by"; ...]
    seq { "hello world good"; "by the way" } |> bind split

