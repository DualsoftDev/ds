open System.Linq

// https://stackoverflow.com/questions/22821920/how-do-i-automate-chains-of-map-lookups-in-f
let search map = Option.bind (fun k -> Map.tryFind k map)
let employee_department = [("John", "Sales" ); ("Bob",    "IT"    )] |> Map.ofSeq
let department_country  = [("IT",   "USA"   ); ("Sales",  "France")] |> Map.ofSeq
let country_currency    = [("USA",  "Dollar"); ("France", "Euro"  )] |> Map.ofSeq
let exchange_rate       = [("Euro", 1.08    ); ("Dollar", 1.2     )] |> Map.ofSeq

let searchAll = 
  search employee_department
  >> search department_country
  >> search country_currency
  >> search exchange_rate

// John -> Sales -> France -> Euro -> 1.08
searchAll (Some "John") //Some 1.08


