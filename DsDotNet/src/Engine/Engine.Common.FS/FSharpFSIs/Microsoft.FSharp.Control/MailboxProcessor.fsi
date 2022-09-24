namespace Microsoft.FSharp.Control

/// A message-processing agent which executes an asynchronous computation.
[<Sealed>]
[<AutoSerializable(false)>]
[<CompiledName("FSharpMailboxProcessor`1")>]
type MailboxProcessor<'Msg> =
    interface System.IDisposable
    /// Creates an agent. The body function is used to generate the asynchronous computation executed by the agent. This function is not executed until Start is called.
    /// body: The function to produce an asynchronous computation that will be executed as the read loop for the MailboxProcessor when Start is called.
    /// cancellationToken: An optional cancellation token for the body.  Defaults to Async.DefaultCancellationToken.
    new : body:(MailboxProcessor<'Msg> -> Async<unit>) * ?cancellationToken:System.Threading.CancellationToken -> MailboxProcessor<'Msg>
    /// Occurs when the execution of the agent results in an exception.
    [<CLIEvent>]
    member add_Error : Handler<System.Exception> -> unit
    /// Returns the number of unprocessed messages in the message queue of the agent.
    member CurrentQueueLength : int
    /// Raises a timeout exception if a message not received in this amount of time. By default no timeout is used.
    member DefaultTimeout : int with get, set
    /// Occurs when the execution of the agent results in an exception.
    [<CLIEvent>]
    member Error : IEvent<System.Exception>
    /// Posts a message to the message queue of the MailboxProcessor, asynchronously.
    /// message: The message to post.
    member Post : message:'Msg -> unit
    /// Posts a message to an agent and await a reply on the channel, asynchronously.
    /// buildMessage: The function to incorporate the AsyncReplyChannel into the message to be sent.
    /// timeout: An optional timeout parameter (in milliseconds) to wait for a reply message.  Defaults to -1 which corresponds to System.Threading.Timeout.Infinite.
    member PostAndAsyncReply : buildMessage:(AsyncReplyChannel<'Reply> -> 'Msg) * ?timeout:int -> Async<'Reply>
    /// Posts a message to an agent and await a reply on the channel, synchronously.
    /// buildMessage: The function to incorporate the AsyncReplyChannel into the message to be sent.
    /// timeout: An optional timeout parameter (in milliseconds) to wait for a reply message.  Defaults to -1 which corresponds to System.Threading.Timeout.Infinite.
    member PostAndReply : buildMessage:(AsyncReplyChannel<'Reply> -> 'Msg) * ?timeout:int -> 'Reply
    /// Like AsyncPostAndReply, but returns None if no reply within the timeout period.
    /// buildMessage: The function to incorporate the AsyncReplyChannel into the message to be sent.
    /// timeout: An optional timeout parameter (in milliseconds) to wait for a reply message.  Defaults to -1 which corresponds to System.Threading.Timeout.Infinite.
    member PostAndTryAsyncReply : buildMessage:(AsyncReplyChannel<'Reply> -> 'Msg) * ?timeout:int -> Async<'Reply option>
    /// Waits for a message. This will consume the first message in arrival order.
    /// timeout: An optional timeout in milliseconds. Defaults to -1 which corresponds to System.Threading.Timeout.Infinite.
    member Receive : ?timeout:int -> Async<'Msg>
    /// Occurs when the execution of the agent results in an exception.
    [<CLIEvent>]
    member remove_Error : Handler<System.Exception> -> unit
    /// Scans for a message by looking through messages in arrival order until scanner returns a Some value. Other messages remain in the queue.
    /// scanner: The function to return None if the message is to be skipped or Some if the message is to be processed and removed from the queue.
    /// timeout: An optional timeout in milliseconds. Defaults to -1 which corresponds to System.Threading.Timeout.Infinite.
    member Scan : scanner:('Msg -> Async<'T> option) * ?timeout:int -> Async<'T>
    /// Starts the agent.
    member Start : unit -> unit
    /// Like PostAndReply, but returns None if no reply within the timeout period.
    /// buildMessage: The function to incorporate the AsyncReplyChannel into the message to be sent.
    /// timeout: An optional timeout parameter (in milliseconds) to wait for a reply message.  Defaults to -1 which corresponds to System.Threading.Timeout.Infinite.
    member TryPostAndReply : buildMessage:(AsyncReplyChannel<'Reply> -> 'Msg) * ?timeout:int -> 'Reply option
    /// Waits for a message. This will consume the first message in arrival order.
    /// timeout: An optional timeout in milliseconds. Defaults to -1 which corresponds to System.Threading.Timeout.Infinite.
    member TryReceive : ?timeout:int -> Async<'Msg option>
    /// Scans for a message by looking through messages in arrival order until scanner returns a Some value. Other messages remain in the queue.
    /// scanner: The function to return None if the message is to be skipped or Some if the message is to be processed and removed from the queue.
    /// timeout: An optional timeout in milliseconds. Defaults to -1 which corresponds to System.Threading.Timeout.Infinite.
    member TryScan : scanner:('Msg -> Async<'T> option) * ?timeout:int -> Async<'T option>
    /// Creates and starts an agent. The body function is used to generate the asynchronous computation executed by the agent.
    /// body: The function to produce an asynchronous computation that will be executed as the read loop for the MailboxProcessor when Start is called.
    /// cancellationToken: An optional cancellation token for the body.  Defaults to Async.DefaultCancellationToken.
    static member Start : body:(MailboxProcessor<'Msg> -> Async<unit>) * ?cancellationToken:System.Threading.CancellationToken -> MailboxProcessor<'Msg>
