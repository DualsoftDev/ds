#I @"..\..\bin"
#I @"..\..\packages\FsUnit.xUnit.3.4.0\lib\net46"
#I @"..\..\Dual.Common.xUnit.FS\bin\Debug"
#I @"..\..\packages\NHamcrest.2.0.1\lib\net451"
#r "System.Xml.ReaderWriter.dll"
#r "System.Xml.Linq.dll"
open System.Xml
open System.Linq
open System.Xml
open System.Xml.Linq

#r "FsUnit.Xunit.dll"
#r "NHamcrest.dll"
#r "Dual.Common.FS.dll"
#r "Old.Dual.Core.FS.dll"
//#r "Dual.Common.xUnit.FS.dll"
//#r "FSharpx.Collections.Experimental.dll"

// #load loads script file
// #load @"..\..\Dual.Common.xUnit.FS\OnlyOnce.fs"
// #I __SOURCE_DIRECTORY__


open System
open System.IO
open System.Collections.Generic;;
open Old.Dual.Common
open FsUnit
open Xunit
//open Old.Dual.Common.UnitTest.FS

open Old.Dual.Core
open Old.Dual.Core.Types




let testme0() =
    let a = Int32.TryParse("6")

    let b = Int32.TryParse("6.7")

    let toOption (b, v) = if b then Some(v) else None


    let six = Int32.TryParse("6") |> toOption 

    let x = Option.fold (+) 2 (Int32.TryParse("7.5") |> toOption) //(Int32.TryParse("6") |> toOption)


    let x = Some 99
    let folded = x |> Option.fold (fun _ v -> v * 2) 0 



    let x = Some 99
    defaultArg x 0 


let noSpaceComparer =
    let replace(s:string) = s.Replace(" ", "")
    {   new IEqualityComparer<_> with
        member x.Equals(a, b) = String.Equals(replace(a), replace(b))
        member x.GetHashCode(s) = replace(s).GetHashCode() }

let scaleNames = new Dictionary<_, _>(noSpaceComparer)
scaleNames.Add("100", "hundred")
scaleNames.Add("1 000", "thousand")
scaleNames.Add("1 000 000", "million")



scaleNames.["10 00"];;
//  val it : string = "thousand"
scaleNames.["1000000"];;
//  val it : string = "million"


let changeColor(clr) =
    let orig = Console.ForegroundColor
    Console.ForegroundColor <- clr
    {   new IDisposable with
            member x.Dispose() =
                Console.WriteLine("Reverting color")
                Console.ForegroundColor <- orig }
let hello() =
    use clr = changeColor(ConsoleColor.Red)
    Console.WriteLine("Hello world!")



let rnd = new System.Random()
let a = List.init 1000 (fun _ -> rnd.Next(-50, 51))
let sumList lst =
    let rec sum lst acc =
        match lst with
        | [] -> acc
        | h::t -> sum t (h + acc)
    sum lst 0

printfn "Sum=%d" (sumList a)
a |> List.sum


(*
Func<T, R> Memoize<T, R>(Func<T, R> func) {
    var cache = new Dictionary<T, R>();
    return arg => {
        R val;
        if (cache.TryGetValue(arg, out val)) return val;
        else {
            val = func(arg);
            cache.Add(arg, val);
            return val;
        }
    };
}
*)

let memoize(f) =
    let cache = new Dictionary<_, _>()
    (fun x ->
        match cache.TryGetValue(x) with
        | true, v -> v
        | _ ->
            let v = f(x)
            cache.Add(x, v)
            v)

let rec factorial(x) =
    printf "factorial(%d); " x
    if (x <= 0) then 1 else x * factorial(x - 1)

let factorialMem = memoize factorial

factorialMem(12)



#nowarn "40"
let rec factorial2 = memoize(fun x ->
    printf "factorial2(%d); " x
    if (x <= 0) then 1 else x * factorial2(x - 1))
factorial2(2)
factorial2(3)
factorial2(6)



open System.Drawing
// F# version using loop and recursion
let rec greenBlackColors = seq {
    for g in 0 .. 25 .. 255 do
        yield Color.FromArgb(g / 2, g, g / 3)
    yield! greenBlackColors }

let rec generate = seq {
    yield! seq {1..10}
    yield! generate
}

let testme1() =
    let generateCircularSeq (lst:'a list) =
        let rec next () =
            seq {
                for element in lst do
                    yield element
                yield! next()
            }
        next()


    let generate = generateCircularSeq ([1..10])
    ()


let testme2() =
    let cities = [ ("New York", "USA"); ("London", "UK"); ("Cambridge", "UK"); ("Cambridge", "USA") ]
    let entered = [ "London"; "Cambridge" ]

    let dict = cities |> dict
    let countries = entered |> List.map( fun e -> dict.[e])

    for (n, c) in cities do
    for e in entered do
        if n = e then
            printfn "(%s, %s)" n c



type OptionBuilder() =
    member x.Bind(opt, f) =
        match opt with
            | Some(value) -> f(value)
            | _ -> None
    member x.Return(v) = Some(v)
let option = new OptionBuilder()



type Tree<'a> =
    | Leaf of 'a
    | Stem of Tree<'a> list

type BinaryTree<'a> =
    | Leaf of 'a
    | Stem of BinaryTree<'a> * BinaryTree<'a>


module Option =
    let ofTuple<'v> (b, v:'v) = if b then Some v else None
    let toTuple x =
        match x with
        | Some(v) -> true, v
        | None -> false, null

let testme3() =
    let a = (true, "a") |> Option.ofTuple
    let b = (false, null)  |> Option.ofTuple<string>
    let n = Int32.TryParse("32") |> Option.ofTuple
    let x = Int32.TryParse("32.7") |> Option.ofTuple
    ()


module IEC61131_test =
    open Old.Dual.Core.Prelude.NewIEC61131

    let (===) a b = assert(a = b)
    let hwconf = HwStorageConfig3(5, 2, 16)
    hwconf.FileBitLength === 2 * 16
    hwconf.GetElementBitLength() === 16

    hwconf.GetBitOffset(0, 0, 0) === 0
    hwconf.GetBitOffset(0, 0, 1) === 1
    hwconf.GetBitOffset(0, 0, 7) === 7
    hwconf.GetBitOffset(0, 0, 15) === 15
    hwconf.GetBitOffset(0, 0, 16) === 16
    hwconf.GetBitOffset(0, 0, 17) === 17  // 범위 넘어서는 것 허용
    hwconf.GetBitOffset(0, 1, 0) === 16
    hwconf.GetBitOffset(0, 1, 1) === 17
    hwconf.GetBitOffset(4, 1, 3) === 4*(2*16) + 1*16 + 3  // 147


    let a0 = parseTag "%IX0.0.0"
    let a1 = parseTag "%IX0.0.1"
    let a2 = parseTag "%IX0.0.17"
    let w3 = parseTag "%IW1.0"
    let w4 = parseTag "%IW2"
    let a4 = parseTag "%IX0.0.0"
    let a5 = parseTag "%IX0.0.0"

    parseTag "%IX0.1.0" |> getBitOffset hwconf === 16
    parseTag "%IX1.0.0" |> getBitOffset hwconf === 32
    parseTag "%IX1.0.1" |> getBitOffset hwconf === 33
    parseTag "%IX1.0.17" |> getBitOffset hwconf === 49
    parseTag "%IX1.1.1" |> getBitOffset hwconf === 49


    parseTag "%IW0" |> getBitOffset hwconf === 16
    parseTag "%IW0" |> getBitStartOffset hwconf === 0
    parseTag "%IB1" |> getBitStartOffset hwconf === 8
    parseTag "%IB2" |> getBitStartOffset hwconf === 16
    parseTag "%IW1" |> getBitStartOffset hwconf === 16
