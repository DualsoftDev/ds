// https://devblogs.microsoft.com/dotnet/announcing-fsharp-8/

type Person = {Name : string; Age : int}
let people = [ {Name = "Joe"; Age = 20} ; {Name = "Will"; Age = 30} ; {Name = "Joe"; Age = 51}]

let beforeThisFeature =
    people
    |> List.distinctBy (fun x -> x.Name)
    |> List.groupBy (fun x -> x.Age)
    |> List.map (fun (x,y) -> y)
    |> List.map (fun x -> x.Head.Name)
    |> List.sortBy (fun x -> x.ToString())

let possibleNow =   // val possibleNow: string list = ["Joe"; "Will"]
    people
    |> List.distinctBy _.Name
    |> List.groupBy _.Age
    |> List.map snd
    |> List.map _.Head.Name
    |> List.sortBy _.ToString()


let inline myPropGetter (x: 'a when 'a:(member WhatANiceProperty:string)) =
      x |> _.WhatANiceProperty

let inline nameGetter (x: 'a when 'a:(member Name:string)) =
      x |> _.Name
people |> List.map nameGetter   // ["Joe"; "Will"; "Joe"]







type SteeringWheel = { Type: string }
type CarInterior = { Steering: SteeringWheel; Seats: int }
type Car = { Interior: CarInterior; ExteriorColor: string option }

let beforeThisFeature x =
    { x with
        Interior = {
            x.Interior with
                Steering = {x.Interior.Steering with Type = "yoke"}
                Seats = 5 }
    }
let withTheFeature x =
    { x with
        Interior.Steering.Type = "yoke"
        Interior.Seats = 4 }

let i40 =
    {
        Interior = {
            Steering = {Type = "NiceSteering" }
            Seats = 5 }
        ExteriorColor = Some "gray"}

withTheFeature i40
(*
> withTheFeature i40;;
val it: Car = { Interior = { Steering = { Type = "yoke" }
                             Seats = 4 }
                ExteriorColor = Some "gray" }
*)


let alsoWorksForAnonymous (x:Car) = {| x with Interior.Seats = 7; Price = 99_999 |}
alsoWorksForAnonymous i40







let mutable count = 0
let asyncCondition = async {
    return count < 10
}
// WHILE BANG: while!
let doStuffWithWhileBang =
    async {
        while! asyncCondition do
            count <- count + 2
        return count
    } |> Async.RunSynchronously



let [<Literal>] bytesInKB = 2f ** 10f
let [<Literal>] bytesInMB = bytesInKB * bytesInKB
let [<Literal>] bytesInGB = 1 <<< 30
let [<Literal>] customBitMask = 0b01010101uy
let [<Literal>] inverseBitMask = ~~~ customBitMask


type MyEnum =
    | A = (1 <<< 5)
    | B = (17 * 45 % 13)
    | C = bytesInGB


// With F# 8, the compiler will only require the attribute on the extension method, and will automatically add the type-level attribute for you.
open System.Runtime.CompilerServices
// 이거 생략해도 가능...   [<Extension>]
type Foo =
    [<Extension>]
    static member PlusOne (a:int) : int = a + 1
let f (b:int) = b.PlusOne()




[<TailCall>]        // open Microsoft.FSharp.Core
let rec factorialClassic n =
    match n with
    | 0u | 1u -> 1u
    | _ -> n * (factorialClassic (n - 1u))


open System
type Color =
    | [<Obsolete("Use B instead")>] Red = 0
    | Green = 1

let c = Color.Red // warning "This construct is deprecated. Use B instead" at this line