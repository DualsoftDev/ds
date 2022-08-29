#load "0.FullLoading.fsx"


#I @"../../bin"

#I @"../../packages/NumSharp.0.20.5/lib/netstandard2.0"
#I @"../../packages/System.Memory.4.5.3/lib/netstandard2.0"
#I @"../../packages/System.Runtime.CompilerServices.Unsafe.4.5.2\lib\netstandard2.0"
// #r "NumSharp.Core.dll"
#r "System.Memory.dll"
#r "System.Runtime.CompilerServices.Unsafe.dll"
#r "Dual.Common.FS.dll"
#r "Dual.Core.FS.dll"
#r "Newtonsoft.Json.dll"

open Dual.Core.Types
open Dual.Common
open Dual.Core
open Dual.Core.Prelude
open System
open System.Reflection
open Newtonsoft.Json
open System.Collections.Generic
//open Dual.Core.DomainModels
//open Dual.Core.DomainModels.JsonMetaTypes

// #load "../BuildSample.fs"

//let file = BuildSample.buildSampleSolution()


#r "FSharpPlus.dll"
open FSharpPlus



let toText obj =
    let toText = obj.GetType().GetMethods() |> Seq.tryFind(fun m -> m.Name = "ToText")
    match toText with
    | Some(toText) ->
        toText.Invoke(obj, null).ToString()
    | None ->
        obj.ToString()

/// Option element 를 꺼내서 toText 적용
let elementToText ele =
    ele |> Option.map toText |> Option.toNullable

    let xx = Some "a" |> Option.toNullable

/// P (process) 에 포함되는 요소 개별에 관한 type.  참조와 action 으로 구분
type PUnit(reference, action) =
    /// Process 에 포함되는 참조.  추후 소멸 예정
    member val Reference:Expression option = reference with get, set
    member val Action:PAction option = action with get, set

type JMPUnit(reference:Expression option, action:PAction option) =
    /// Process 에 포함되는 참조.  추후 소멸 예정
    member val Reference = elementToText(reference) with get, set
    member val Action = JsonConvert.SerializeObject(action, Formatting.Indented) with get, set
    member x.ConvertBack() =
        let ref = ParserAlgo.parseExpression(x.Reference) |> Option.ofResult
        let act = JsonConvert.DeserializeObject<PAction option>(x.Action)
        PUnit(ref, act)




let jsonSettings =
    new JsonSerializerSettings(
        TypeNameHandling = TypeNameHandling.All,
        // Newtonsoft.Json.JsonSerializationException: Self referencing loop detected for property ....  https://stackoverflow.com/questions/13510204/json-net-self-referencing-loop-detected
        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
        PreserveReferencesHandling = PreserveReferencesHandling.All,
        Formatting = Formatting.Indented)

let punit =
    let taga = PLCTag("tagA", Some TagType.State)
    let tagb = PLCTag("OnSymbol", Some TagType.Action)
    let reference = Some <| Terminal(taga)
    let action = Some <| OnOffAction(TurnOn(tagb))
    let s = JsonConvert.SerializeObject(action, jsonSettings)
    PUnit(reference, action)
let jsonFull = JsonConvert.SerializeObject(punit, jsonSettings)
printfn "%s" jsonFull

// Newtonsoft.Json.JsonSerializationException:
// Could not create an instance of type Dual.Core.Types.IExpressionTerminal.
// Type is an interface or abstract class and cannot be instantiated. Path '[0]', line 8, position 11.
//
//let punitFull2 = JsonConvert.DeserializeObject<PUnit>(jsonFull)
let jmpUnitFull = JMPUnit(punit.Reference, punit.Action)
let jsonFull2 = JsonConvert.SerializeObject(jmpUnitFull, Formatting.Indented)
printfn "%s" jsonFull2



let punitEmpty = PUnit(None, None)
let jsonEmpty = JsonConvert.SerializeObject(punitEmpty, Formatting.Indented)
printfn "%s" jsonEmpty
let punitEmpty2 = JsonConvert.DeserializeObject<PUnit>(jsonEmpty)

type Cause() =    
    member val Pure:Expression option = None with get, set
    member val Impure:Expression option = None with get, set
    //member x.Cause
    member x.DoNothing() = ()


let inline toText (x : ^T) = (^T : (member ToText : unit->string) (x))


open System.Collections.Generic
let KeyValuePairToTuple (kv:KeyValuePair<'t1, 't2>) =
    kv.Key, kv.Value



let a = [1] |> List.pairwise
let b = [1; 2] |> List.pairwise


