
#load "ImperativeBuilder.fs"
open System
let validateName(arg:string) = imperative {
    // Should be non-empty and should contain space
    if (arg = null) then return false
    let idx = arg.IndexOf(" ")
    if (idx = -1) then return false
    
    // Verify the name and the surname
    let name = arg.Substring(0, idx)
    let surname = arg.Substring(idx + 1, arg.Length - idx - 1)
    if (surname.Length < 1 || name.Length < 1) then return false
    if (Char.IsLower(surname.[0]) || Char.IsLower(name.[0])) then return false

    // Looks like we've got a valid name!
    return true }

let getNumber(arg:string) = imperative {
    if (arg = null) then return ""
    if (arg.Length % 2 = 0) then return ""
    return arg
}
getNumber "a"
getNumber "aa"
getNumber "aaa"
getNumber "aaaa"
getNumber "aaaaa"


let exists f inp = imperative {
    for v in inp do 
      if f(v) then return true
    return false }
// val exists : ('a -> bool) -> seq<'a> -> bool

[ 1 .. 10 ] |> exists (fun v -> v % 3 = 0)

let readFirstName() = imperative {
    // Loop until the user enters valid name
    while true do
      let name = Console.ReadLine()
      // If the name is valid, we return it, otherwise
      // we continue looping...
      if (validateName(name)) then return name
      printfn "That's not a valid name! Try again..." }


