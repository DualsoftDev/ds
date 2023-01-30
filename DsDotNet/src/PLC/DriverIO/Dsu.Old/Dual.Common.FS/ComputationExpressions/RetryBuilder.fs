// https://viralfsharp.com/category/computation-expression/
// http://www.fssnip.net/8o/title/Monadic-Retry

module Old.Dual.Common.RetryBuilder

open System

type RetryParams = {
    /// retry 수행 중간의 pause time : millisecond  : 갯수 만큼 retry 한다.
    budgets: int list

    /// retry 수행시에 작업할 내용 지정
    onRetry: int -> unit

    /// retry 를 계속 수행할지의 여부 지정
    keepRetrying: exn -> bool
}

let defaultRetryParams = {
    budgets = [1000; 1000; 1000]
    onRetry = fun i -> ();
    keepRetrying = fun ex -> true; }

type RetryMonad<'a> = RetryParams -> 'a
let rm<'a> (f : RetryParams -> 'a) : RetryMonad<'a> = f

let internal retryFunc<'a> (f : RetryMonad<'a>) =
    rm (fun retryParams ->
        let rec execWithRetry f nth (budgets:List<int>) e =
            try
                f retryParams
//                    printfn "Retrying %A for %d-th" f i
//                    let result = f retryParams
//                    printfn "Result: %A" result
//                    result
            with e ->
                if (retryParams.keepRetrying(e)) then
                    match budgets with
                    | [] -> raise e
                    | h::t ->
                        System.Threading.Thread.Sleep(h)
                        retryParams.onRetry(nth+1)
                        execWithRetry f (nth+1) t e
                else
                    raise e

        execWithRetry f 0 retryParams.budgets (Exception()) )


type RetryBuilder() =

    member __.Bind (p : RetryMonad<'a>, f : 'a -> RetryMonad<'b>)  =
        rm (fun retryParams ->
            let value = retryFunc p retryParams //extract the value
            f value retryParams                //... and pass it on
        )

    member __.Return (x : 'a) = fun defaultRetryParams -> x
    member __.Run(m : RetryMonad<'a>) = m
    member __.Delay(f : unit -> RetryMonad<'a>) = f ()


    member __.Zero() =
        failwith "RetryBuilder: Zero"
        //None


/// Retry computation expression builder
let retry = RetryBuilder()

