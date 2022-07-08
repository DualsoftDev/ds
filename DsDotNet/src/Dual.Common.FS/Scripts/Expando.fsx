open System.Dynamic
open System.Collections
open System.Collections.Generic


let dic = Dictionary<string, obj>()
dic.["say"] <- fun () -> printfn "Hello"
let a:unit->unit = dic.["say"] :?> unit -> unit


let ex = new ExpandoObject()
ex.Add("Age", 100)
let expandoDictionary = ex :> IDictionary<string,obj>
for paramValue in param do
    expandoDictionary.Add(paramValue.Key, paramValue.Value :> obj)
ex.Age <- 100
ex.Name <- "kwak"


open FSharp. .Interop.Dynamic
let ex1 = ExpandoObject()
ex1?Test<-"Hi"//Set Dynamic Property
ex1?Test //Get Dynamic
