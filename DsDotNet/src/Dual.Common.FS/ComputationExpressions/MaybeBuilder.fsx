#I @"..\..\bin"
#r "Dual.Common.FS.dll"
open Dual.Common.ComputationExpressions

// #load "MaybeBuilder.fs"

type MaybeBuilder() =
    member __.Bind(v,f) = Option.bind f v
    member __.Return v = Some v
    member __.ReturnFrom o = o
    member __.Delay(f) = f()

let maybe = MaybeBuilder()





// 위의 maybe 를 이용해도 됨.  (FSharpX 대신)
// https://alfredodinapoli.wordpress.com/2012/04/02/humbly-simple-f-maybe-monad-application-scenario/
// https://gist.github.com/adinapoli/2274497
/// Type synonims
type ProductId = string
type Price = float


type Inventory() =
    let inv_ = new System.Collections.Generic.Dictionary<ProductId, Price>()

    member this.Stock (id : ProductId) (price : Price) =
        inv_.Add(id, price)

    member this.Price (id : ProductId) =
        try
            Some(inv_.[id])
        with
            | :? System.Collections.Generic.KeyNotFoundException -> None


let inline (|@|) (p1 : Price option) (p2 : Price option) =
    maybe {
        let! v1 = p1
        let! v2 = p2
        //return! Some(v1 + v2)
        return v1 + v2
    }

let reporter (priceSum : Price option) : unit =
    match priceSum with
    | Some(p) -> printfn "Total price: %g." p
    | None    -> printfn "One or more id not found."


//Initialize a basic inventory and throw inside a bunch of items
let inventory = new Inventory()
inventory.Stock "MyWidget" 10.3
inventory.Stock "Gizmos" 4.34
inventory.Stock "Foo1000" 8.12

//Sum prices
inventory.Price("MyWidget") |@| inventory.Price("Gizmos") |> reporter

//A further step, price sum pipelining
inventory.Price("MyWidget")
    |> (|@|) (inventory.Price("Gizmos"))
    |> (|@|) (inventory.Price("Foo1000"))
    |> reporter

//A failing computation
inventory.Price("MyWidget")
    |> (|@|) (inventory.Price("Gizmos"))
    |> (|@|) (inventory.Price("DoesNotExist"))
    |> reporter


//A completely automatic procedure
//let sumAndReport (inventory : Inventory) (ids : ProductId list) : unit =
//    let basket = List.map (fun pid -> inventory.Price(pid)) ids
//    in List.reduce (fun p1 p2 -> p1 |@| p2) basket
//    |> reporter


//A completely automatic procedure
let sumAndReport (inventory : Inventory) (ids : ProductId list) : unit =
    List.map inventory.Price ids
    |> List.reduce (|@|)            // |> List.reduce (fun p1 p2 -> p1 |@| p2)
    |> reporter

sumAndReport inventory ["Gizmos"; "Foo1000"]        // Total price: 12.46.
sumAndReport inventory ["Gizmos"; "NonExisting"]    // One or more id not found.













// https://bradcollins.com/2015/07/03/f-friday-the-option-type-part-2/
type User(id : string, name : string option) =
    new(id : string) = User(id, None)
    member x.Id = id
    member x.Name = name
type Session(user : User option) =
    new() = Session(None)
    member x.User = user
type App(session : Session option) =
    new() = App(None)
    member x.Session = session


let u1 = User("kwak", Some("Kwak, JongGeun"))
let s1 = Session(Some(u1))
let a1 = App(Some(s1))

let someUser = maybe {
    let! session = a1.Session
    let! user = session.User
    return! user.Name
}
// val someUser : string option = Some "Kwak, JongGeun"

let someUser2 =
    a1.Session
        |> Option.bind (fun s -> s.User)
        |> Option.bind (fun u -> u.Name)
// val someUser2 : string option = Some "Kwak, JongGeun"





// NONE
maybe {
    let! session = App(None).Session
    let! user = session.User
    return user.Name
}


// NONE
maybe {
    let s1 = Session(None)
    let a1 = App(Some(s1))

    let! session = a1.Session
    let! user = session.User
    return user.Name
}












trialE { return "suceeded!" }
trialE { return 100 }
trialE { return! Ok ("suceeded!") }
trialE { return! Some ("suceeded!") }

trialE {
    let! s1 = Some "Ok1"
    let! s2 = Some 100
    return sprintf "%s-%d" s1 s2
}

trialE {
    let! s1 = Ok "Ok1"
    let! s2 = Some "Ok2"
    let! s3 = Ok 100
    return sprintf "%s-%s-%d" s1 s2 s3
}


trialE { return! option<int>.None }
trialE { return! Result<int, exn>.Error (new System.Exception("Error")) }

(* Does not work *)
trialE { return! None }                         // Option<^T> : type 이 정해지지 않은 option 은 수용할 수 없다.
//trialE { return! Error ("Error") }          // !!!!! Excetpion 이 아닌 Error 를 수용할 수 없다.!!!!
trialE { return! Error (new System.Exception("Error")) }    // Result<'a, 'b> 의 type 이 정해지지 않은 Error 는 수용할 수 없다.
trialE {
    let! e1 = Error (new System.Exception("Error"))
    return e1
}
(* Does not work *)


trialE {
    let! (e1:option<int>) = None
    return e1
}


trialE {
    failwith "Local scope function evaluation error"        // <--- requires Delay() implementation
    return "Failed: will not reach here"
}

let mytrial x : option<'t> =
    match x with
    | Some v -> Some v
    | None -> failwith "Function evaluation error"

trialE {

    let! a = Some "aaa"
    let! b = mytrial None
    //let! e1 = Error (new System.Exception("Error string"))
    let! c = Ok "kwak"
    //let! e2 = None
    //let! e3 = mytrial None
    return sprintf "%s-%s-%s" a b c
}

trialE {
    ()  // <--- requires Zero() implementation
}










trialO { return "suceeded!" }
trialO { return 100 }
trialO { return! Ok ("suceeded!") }
trialO { return! Some ("suceeded!") }

trialO {
    let! s1 = Some "Ok1"
    let! s2 = Some 100
    return sprintf "%s-%d" s1 s2
}

trialO {
    let! s1 = Ok "Ok1"
    let! s2 = Some "Ok2"
    let! s3 = Ok 100
    return sprintf "%s-%s-%d" s1 s2 s3
}


trialO { return! option<int>.None }
trialO { return! Result<int, exn>.Error (new System.Exception("Error")) }

(* Does not work *)
trialO { return! None }                         // Option<^T> : type 이 정해지지 않은 option 은 수용할 수 없다.
trialO { return! Error ("Error") }          // !!!!! Excetpion 이 아닌 Error 를 수용할 수 없다.!!!!
trialO { return! Error (new System.Exception("Error")) }    // Result<'a, 'b> 의 type 이 정해지지 않은 Error 는 수용할 수 없다.
trialO {
    let! e1 = Error ("Error")
    return e1
}
(* Does not work *)


trialO {
    let! (e1:option<int>) = None
    return e1
}


trialO {
    failwith "Local scope function evaluation error"        // <--- requires Delay() implementation
    return "Failed: will not reach here"
}


trialO {

    let! a = Some "aaa"
    let! b = mytrial None
    //let! e1 = Error (new System.Exception("Error string"))
    let! c = Ok "kwak"
    //let! e2 = None
    //let! e3 = mytrial None
    return sprintf "%s-%s-%s" a b c
}

trialO {
    ()  // <--- requires Zero() implementation
}
