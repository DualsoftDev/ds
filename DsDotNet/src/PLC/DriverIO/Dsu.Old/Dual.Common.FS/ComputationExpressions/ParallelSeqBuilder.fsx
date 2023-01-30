#I @"..\..\bin"
#r "Old.Dual.Common.FS.dll"
open Old.Dual.Common
//open Old.Dual.Common.ComputationExpressions
//open Old.Dual.Common.ParallelSeq


// Load the parallel module       
// #load "ParallelSeqBuilder.fs"
ParallelSeq.parallelFor 1 5 (fun n -> printfn "%A" n)


// Listing 14.3 Counting the number of primes (C# and F#)
let isPrime(n) =
  let ms = int(sqrt(float(n)))
  let rec isPrimeUtil(m) =
    if m > ms then true
    elif n % m = 0 then false
    else isPrimeUtil(m + 1)
    
    // Side Note:
    // The same thing can be written more compactly like this:
    //   m >= ns || (n % m <> 0 && (isPrimeUtil(m+1)))
    
  (n > 1) && isPrimeUtil(2)

// Count the primes
let nums = [1000000 .. 2999999]

#time

let primeCount1 =
  nums |> List.filter isPrime
       |> List.length



// Lising 14.4 Counting primes in parallel (C# and F#)
let primeCount2 =
  nums |> ParallelSeq.ofSeq
       |> ParallelSeq.filter isPrime
       |> ParallelSeq.length

// Turn the timing Off
#time
