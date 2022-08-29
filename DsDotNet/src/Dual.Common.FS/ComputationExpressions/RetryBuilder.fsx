#I @"..\..\bin"
#r "Dual.Common.FS.dll"

open Dual.Common.ComputationExpressions
open Dual.Common.RetryBuilder


// #load "RetryBuilder.fs"


//Examples
let test() =
    
    let fn1 (x:float) (y:float) = rm (fun rp -> x * y)
    let fn2 (x:float) (y:float) = rm (fun rp -> if y = 0. then raise (invalidArg "y" "cannot be 0") else x / y)

    try
        let x = 
            (retry {
                let! a = fn1 7. 5.
                let! b = fn1 a 10.
                return b
            }) defaultRetryParams 

        printfn "first retry: %f" x

        // retry 구현
        // 1, 1, 3초 간격으로 세번 더 retry.
        let onRetry = fun i -> printfn "%d-th retrying.." i
        let keepRetrying = fun ex -> true
        let retryParams =
            {budgets = [1000; 1000; 3000]; onRetry = onRetry; keepRetrying = keepRetrying}

        let ym = 
            retry {
                let! a = fn1 7. 5.
                let! b = fn1 a a
                let! c = fn2 b 0. //division by 0.
                return c
            }

        let y = ym retryParams
        0
    with
        e -> printfn "%s" e.Message; 1

test()
