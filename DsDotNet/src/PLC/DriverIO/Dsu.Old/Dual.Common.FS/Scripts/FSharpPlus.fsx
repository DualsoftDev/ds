#r "nuget: FSharpPlus" 

open FSharpPlus

// sample code from http://en.wikibooks.org/wiki/Haskell/MonadPlus
let pythags = monad {
  let! z = [1..50]
  let! x = [1..z]
  let! y = [x..z]
  do! guard (x*x + y*y = z*z)
  return (x, y, z)}

// same operation but using the monad.plus computation expression
let pythags' = monad.plus {
  let! z = [1..50]
  let! x = [1..z]
  let! y = [x..z]
  if (x*x + y*y = z*z) then return (x, y, z)}

