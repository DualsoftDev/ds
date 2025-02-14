namespace Dual.Common.Core.FS

open System
open System.Collections.Generic

// https://tomasp.net/blog/imperative-i-return.aspx/
[<AutoOpen>]
module ImperativeBuilderModule =
    type Imperative<'T> = unit -> option<'T>

    type ImperativeBuilder() =
      member x.Combine(a, b) = (fun () ->
        match a() with
        | Some(v) -> Some(v)
        | _ -> b() )
      member x.Delay(f:unit -> Imperative<_>) : Imperative<_> = (fun () -> f()())
      member x.Return(v) : Imperative<_> = (fun () -> Some(v))
      member x.Zero() = (fun () -> None)
      member x.Run(imp) =
        match imp() with
        | Some(v) -> v
        | _ -> failwith "Nothing returned!"

      member x.For(inp:seq<_>, f) =
        let rec loop(en:IEnumerator<_>) =
          if not(en.MoveNext()) then x.Zero() else
            x.Combine(f(en.Current), x.Delay(fun () -> loop(en)))
        loop(inp.GetEnumerator())
      member x.While(gd, body) =
        let rec loop() =
          if not(gd()) then x.Zero() else
            x.Combine(body, x.Delay(fun () -> loop()))
        loop()

    let imperative = new ImperativeBuilder()


#if false

let test = imperative {
  return 0
  return 1 }

let validateName(arg:string) = imperative {
  if (arg = null) then return false
  let idx = arg.IndexOf(" ")
  if (idx = -1) then return false
  let name = arg.Substring(0, idx)
  let surname = arg.Substring(idx + 1, arg.Length - idx - 1)
  if (surname.Length < 1 || name.Length < 1) then return false
  if (Char.IsLower(surname.[0]) || Char.IsLower(name.[0])) then return false
  return true }

validateName(null)
validateName("Tomas")
validateName("Tomas Petricek")



let readFirstName() = imperative {
  while true do
    let name = Console.ReadLine()
    if (validateName(name)) then
      return name
    printfn "That's not a valid name! Try again..." }

let exists f inp = imperative {
  for v in inp do
    printfn "testing %A" v
    if f(v) then return true
  return false }

[ 1 .. 10 ] |> exists (fun v -> v % 3 = 0)

#endif