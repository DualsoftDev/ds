namespace Microsoft.FSharp.Control

open System

/// This static class holds members for creating and manipulating asynchronous computations.
[<Sealed>]
[<CompiledName("FSharpAsync")>]
[<Class>]
type Async =
    /// Creates three functions that can be used to implement the .NET Asynchronous Programming Model (APM) for a given asynchronous computation.
    /// computation: A function generating the asynchronous computation to split into the traditional .NET Asynchronous Programming Model.
    static member AsBeginEnd : computation:('Arg -> Async<'T>) -> ('Arg * AsyncCallback * obj -> IAsyncResult) * (IAsyncResult -> 'T) * (IAsyncResult -> unit)
    /// Creates an asynchronous computation that waits for a single invocation of a CLI event by adding a handler to the event. Once the computation completes or is cancelled, the handler is removed from the event.
    /// event: The event to handle once.
    /// cancelAction: An optional function to execute instead of cancelling when a cancellation is issued.
    static member AwaitEvent : event:IEvent<'Del,'T> * ?cancelAction:(unit -> unit) -> Async<'T> when 'Del : delegate<'T, unit> and 'Del :> Delegate
    /// Creates an asynchronous computation that will wait on the IAsyncResult.
    /// iar: The IAsyncResult to wait on.
    /// millisecondsTimeout: The timeout value in milliseconds. If one is not provided then the default value of -1 corresponding to System.Threading.Timeout.Infinite.
    static member AwaitIAsyncResult : iar:IAsyncResult * ?millisecondsTimeout:int -> Async<bool>
    /// Return an asynchronous computation that will wait for the given task to complete and return its result.
    static member AwaitTask : task:Threading.Tasks.Task -> Async<unit>
    /// Return an asynchronous computation that will wait for the given task to complete and return its result.
    static member AwaitTask : task:Threading.Tasks.Task<'T> -> Async<'T>
    /// Creates an asynchronous computation that will wait on the given WaitHandle.
    /// waitHandle: The WaitHandle that can be signalled.
    /// millisecondsTimeout: The timeout value in milliseconds. If one is not provided then the default value of -1 corresponding to System.Threading.Timeout.Infinite.
    static member AwaitWaitHandle : waitHandle:Threading.WaitHandle * ?millisecondsTimeout:int -> Async<bool>
    /// Raises the cancellation condition for the most recent set of asynchronous computations started without any specific CancellationToken. Replaces the global CancellationTokenSource with a new global token source for any asynchronous computations created after this point without any specific CancellationToken.
    static member CancelDefaultToken : unit -> unit
    /// Creates an asynchronous computation that returns the CancellationToken governing the execution of the computation.
    static member CancellationToken : Async<Threading.CancellationToken>
    /// Creates an asynchronous computation that executes computation.  If this computation completes successfully then return Choice1Of2 with the returned value. If this computation raises an exception before it completes then return Choice2Of2 with the raised exception.
    /// computation: The input computation that returns the type T.
    static member Catch : computation:Async<'T> -> Async<Choice<'T,exn>>
    /// Gets the default cancellation token for executing asynchronous computations.
    static member DefaultCancellationToken : Threading.CancellationToken
    /// Creates an asynchronous computation in terms of a Begin/End pair of actions in the style used in CLI APIs. This overload should be used if the operation is qualified by three arguments. For example, Async.FromBeginEnd(arg1,arg2,arg3,ws.BeginGetWeather,ws.EndGetWeather) When the computation is run, beginFunc is executed, with a callback which represents the continuation of the computation. When the callback is invoked, the overall result is fetched using endFunc.
    /// arg1: The first argument for the operation.
    /// arg2: The second argument for the operation.
    /// arg3: The third argument for the operation.
    /// beginAction: The function initiating a traditional CLI asynchronous operation.
    /// endAction: The function completing a traditional CLI asynchronous operation.
    /// cancelAction: An optional function to be executed when a cancellation is requested.
    static member FromBeginEnd : arg1:'Arg1 * arg2:'Arg2 * arg3:'Arg3 * beginAction:('Arg1 * 'Arg2 * 'Arg3 * AsyncCallback * obj -> IAsyncResult) * endAction:(IAsyncResult -> 'T) * ?cancelAction:(unit -> unit) -> Async<'T>
    /// Creates an asynchronous computation in terms of a Begin/End pair of actions in the style used in CLI APIs. This overload should be used if the operation is qualified by two arguments. For example, Async.FromBeginEnd(arg1,arg2,ws.BeginGetWeather,ws.EndGetWeather) When the computation is run, beginFunc is executed, with a callback which represents the continuation of the computation. When the callback is invoked, the overall result is fetched using endFunc.
    /// arg1: The first argument for the operation.
    /// arg2: The second argument for the operation.
    /// beginAction: The function initiating a traditional CLI asynchronous operation.
    /// endAction: The function completing a traditional CLI asynchronous operation.
    /// cancelAction: An optional function to be executed when a cancellation is requested.
    static member FromBeginEnd : arg1:'Arg1 * arg2:'Arg2 * beginAction:('Arg1 * 'Arg2 * AsyncCallback * obj -> IAsyncResult) * endAction:(IAsyncResult -> 'T) * ?cancelAction:(unit -> unit) -> Async<'T>
    /// Creates an asynchronous computation in terms of a Begin/End pair of actions in the style used in CLI APIs. This overload should be used if the operation is qualified by one argument. For example, Async.FromBeginEnd(place,ws.BeginGetWeather,ws.EndGetWeather) When the computation is run, beginFunc is executed, with a callback which represents the continuation of the computation. When the callback is invoked, the overall result is fetched using endFunc.
    /// arg: The argument for the operation.
    /// beginAction: The function initiating a traditional CLI asynchronous operation.
    /// endAction: The function completing a traditional CLI asynchronous operation.
    /// cancelAction: An optional function to be executed when a cancellation is requested.
    static member FromBeginEnd : arg:'Arg1 * beginAction:('Arg1 * AsyncCallback * obj -> IAsyncResult) * endAction:(IAsyncResult -> 'T) * ?cancelAction:(unit -> unit) -> Async<'T>
    /// Creates an asynchronous computation in terms of a Begin/End pair of actions in the style used in CLI APIs. For example, Async.FromBeginEnd(ws.BeginGetWeather,ws.EndGetWeather) When the computation is run, beginFunc is executed, with a callback which represents the continuation of the computation. When the callback is invoked, the overall result is fetched using endFunc.
    /// beginAction: The function initiating a traditional CLI asynchronous operation.
    /// endAction: The function completing a traditional CLI asynchronous operation.
    /// cancelAction: An optional function to be executed when a cancellation is requested.
    static member FromBeginEnd : beginAction:(AsyncCallback * obj -> IAsyncResult) * endAction:(IAsyncResult -> 'T) * ?cancelAction:(unit -> unit) -> Async<'T>
    /// Creates an asynchronous computation that captures the current success, exception and cancellation continuations. The callback must eventually call exactly one of the given continuations.
    /// callback: The function that accepts the current success, exception, and cancellation continuations.
    static member FromContinuations : callback:(('T -> unit) * (exn -> unit) * (OperationCanceledException -> unit) -> unit) -> Async<'T>
    /// Creates an asynchronous computation that runs the given computation and ignores its result.
    /// computation: The input computation.
    static member Ignore : computation:Async<'T> -> Async<unit>
    /// Generates a scoped, cooperative cancellation handler for use within an asynchronous workflow.
    /// interruption: The function that is executed on the thread performing the cancellation.
    static member OnCancel : interruption:(unit -> unit) -> Async<IDisposable>
    /// Creates an asynchronous computation that executes all the given asynchronous computations, initially queueing each as work items and using a fork/join pattern.
    /// computationList: A sequence of distinct computations to be parallelized.
    static member Parallel : computations:seq<Async<'T>> -> Async<'T []>
    /// Runs the asynchronous computation and await its result.
    /// computation: The computation to run.
    /// timeout: The amount of time in milliseconds to wait for the result of the computation before raising a System.TimeoutException. If no value is provided for timeout then a default of -1 is used to correspond to System.Threading.Timeout.Infinite.
    /// cancellationToken: The cancellation token to be associated with the computation.  If one is not supplied, the default cancellation token is used.
    static member RunSynchronously : computation:Async<'T> * ?timeout:int * ?cancellationToken:Threading.CancellationToken -> 'T
    /// Creates an asynchronous computation that will sleep for the given time. This is scheduled using a System.Threading.Timer object. The operation will not block operating system threads for the duration of the wait.
    /// millisecondsDueTime: The number of milliseconds to sleep.
    static member Sleep : millisecondsDueTime:int -> Async<unit>
    /// Starts the asynchronous computation in the thread pool. Do not await its result.
    /// computation: The computation to run asynchronously.
    /// cancellationToken: The cancellation token to be associated with the computation.  If one is not supplied, the default cancellation token is used.
    static member Start : computation:Async<unit> * ?cancellationToken:Threading.CancellationToken -> unit
    /// Executes a computation in the thread pool.
    static member StartAsTask : computation:Async<'T> * ?taskCreationOptions:Threading.Tasks.TaskCreationOptions * ?cancellationToken:Threading.CancellationToken -> Threading.Tasks.Task<'T>
    /// Starts a child computation within an asynchronous workflow. This allows multiple asynchronous computations to be executed simultaneously.
    /// computation: The child computation.
    /// millisecondsTimeout: The timeout value in milliseconds. If one is not provided then the default value of -1 corresponding to System.Threading.Timeout.Infinite.
    static member StartChild : computation:Async<'T> * ?millisecondsTimeout:int -> Async<Async<'T>>
    /// Creates an asynchronous computation which starts the given computation as a System.Threading.Tasks.Task
    static member StartChildAsTask : computation:Async<'T> * ?taskCreationOptions:Threading.Tasks.TaskCreationOptions -> Async<Threading.Tasks.Task<'T>>
    /// Runs an asynchronous computation, starting immediately on the current operating system thread.
    /// computation: The asynchronous computation to execute.
    /// cancellationToken: The CancellationToken to associate with the computation.  The default is used if this parameter is not provided.
    static member StartImmediate : computation:Async<unit> * ?cancellationToken:Threading.CancellationToken -> unit
    /// Runs an asynchronous computation, starting immediately on the current operating system thread. Call one of the three continuations when the operation completes.
    /// computation: The asynchronous computation to execute.
    /// continuation: The function called on success.
    /// exceptionContinuation: The function called on exception.
    /// cancellationContinuation: The function called on cancellation.
    /// cancellationToken: The CancellationToken to associate with the computation.  The default is used if this parameter is not provided.
    static member StartWithContinuations : computation:Async<'T> * continuation:('T -> unit) * exceptionContinuation:(exn -> unit) * cancellationContinuation:(OperationCanceledException -> unit) * ?cancellationToken:Threading.CancellationToken -> unit
    /// Creates an asynchronous computation that runs its continuation using syncContext.Post. If syncContext is null then the asynchronous computation is equivalent to SwitchToThreadPool().
    /// syncContext: The synchronization context to accept the posted computation.
    static member SwitchToContext : syncContext:Threading.SynchronizationContext -> Async<unit>
    /// Creates an asynchronous computation that creates a new thread and runs its continuation in that thread.
    static member SwitchToNewThread : unit -> Async<unit>
    /// Creates an asynchronous computation that queues a work item that runs its continuation.
    static member SwitchToThreadPool : unit -> Async<unit>
    /// Creates an asynchronous computation that executes computation.  If this computation is cancelled before it completes then the computation generated by running compensation is executed.
    /// computation: The input asynchronous computation.
    /// compensation: The function to be run if the computation is cancelled.
    static member TryCancelled : computation:Async<'T> * compensation:(OperationCanceledException -> unit) -> Async<'T>
