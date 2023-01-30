module Old.Dual.Common.ComputationExpressions
open Old.Dual.Common.Internal

let lazylist = LazyListBuilder.lazylist
let retry = RetryBuilder.retry
let result = ResultBuilder.result
let defaultRetryParams = RetryBuilder.defaultRetryParams
let imperative = ImperativeBuilder.imperative
let state = StateBuilder.state
let stringBuilder = StringBuilder.stringBuilder
let untilSome = UntilSomeBuilder.untilSome
let disposable = DisposableBuilder.disposable
let nullable = NullableBuilder.nullable


//let parallelSeq = ParallelSeq.parallelSeq


let maybe = MaybeBuilder.maybe
let option2result = MaybeBuilder.option2result
let result2option = MaybeBuilder.result2option
let trialE = MaybeBuilder.trialE
let trialO = MaybeBuilder.trialO
