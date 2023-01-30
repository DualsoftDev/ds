#I @"..\..\bin"
#r "Old.Dual.Common.FS.dll"
open Old.Dual.Common.ComputationExpressions

// #load "LazyListBuilder.fs"
open Old.Dual.Common.LazyListBuilder


/// Lazily constructed random generator
let lazyRandom = lazy System.Random()

/// Generate a lazy list with random numbers
let nums =
  lazylist {
    for x in 1 .. 5 do
        let! rnd = lazyRandom
        yield rnd.Next(10) }

/// Generate a lazy list by using 'nunms' twice
let twiceNums =
  lazylist {
    yield! nums
    for n in nums do
        yield n * 10 }

// Run this line to see the result
twiceNums |> toList





///// Law that specifies lifting w.r.t. unit
//// we assume that l is the underlying monad (i.e. lazy) and ll is the result of monad transformer application (i.e. lazy list).
//let liftUnit v =
//  let m1 = lazylist { let! x = l { return v } in yield x }
//  let m2 = lazylist { yield v }
//  m1 |> shouldEqual m2

///// Law that specifies lifting w.r.t. bind
//let liftBind m g f =
//  let m1 = lazylist { let! y = l { let! x = m in return! f x }
//                yield! g y }
//  let m2 = lazylist { let! x = m in let! y = l { return! f x }
//                yield! g y }
//  m1 |> shouldEqual m2