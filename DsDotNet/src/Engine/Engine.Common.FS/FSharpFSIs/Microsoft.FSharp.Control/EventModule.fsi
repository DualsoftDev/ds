[<CompilationRepresentation(enum<CompilationRepresentationFlags> (4))>]
[<RequireQualifiedAccess>]
module Microsoft.FSharp.Control.Event

/// Fires the output event when either of the input events fire.
/// event1: The first input event.
/// event2: The second input event.
val merge : event1:IEvent<'Del1,'T> -> event2:IEvent<'Del2,'T> -> IEvent<'T> when 'Del1 : delegate<'T, unit> and 'Del1 :> System.Delegate and 'Del2 : delegate<'T, unit> and 'Del2 :> System.Delegate
/// Returns a new event that passes values transformed by the given function.
/// map: The function to transform event values.
/// sourceEvent: The input event.
val map : mapping:('T -> 'U) -> sourceEvent:IEvent<'Del,'T> -> IEvent<'U> when 'Del : delegate<'T, unit> and 'Del :> System.Delegate
/// Returns a new event that listens to the original event and triggers the resulting event only when the argument to the event passes the given function.
/// predicate: The function to determine which triggers from the event to propagate.
/// sourceEvent: The input event.
val filter : predicate:('T -> bool) -> sourceEvent:IEvent<'Del,'T> -> IEvent<'T> when 'Del : delegate<'T, unit> and 'Del :> System.Delegate
/// Returns a new event that listens to the original event and triggers the first resulting event if the application of the predicate to the event arguments returned true, and the second event if it returned false.
/// predicate: The function to determine which output event to trigger.
/// sourceEvent: The input event.
val partition : predicate:('T -> bool) -> sourceEvent:IEvent<'Del,'T> -> IEvent<'T> * IEvent<'T> when 'Del : delegate<'T, unit> and 'Del :> System.Delegate
/// Returns a new event that listens to the original event and triggers the first resulting event if the application of the function to the event arguments returned a Choice1Of2, and the second event if it returns a Choice2Of2.
/// splitter: The function to transform event values into one of two types.
/// sourceEvent: The input event.
val split : splitter:('T -> Choice<'U1,'U2>) -> sourceEvent:IEvent<'Del,'T> -> IEvent<'U1> * IEvent<'U2> when 'Del : delegate<'T, unit> and 'Del :> System.Delegate
/// Returns a new event which fires on a selection of messages from the original event.  The selection function takes an original message to an optional new message.
/// chooser: The function to select and transform event values to pass on.
/// sourceEvent: The input event.
val choose : chooser:('T -> 'U option) -> sourceEvent:IEvent<'Del,'T> -> IEvent<'U> when 'Del : delegate<'T, unit> and 'Del :> System.Delegate
/// Returns a new event consisting of the results of applying the given accumulating function to successive values triggered on the input event. An item of internal state records the current value of the state parameter. The internal state is not locked during the execution of the accumulation function, so care should be taken that the input IEvent not triggered by multiple threads simultaneously.
/// collector: The function to update the state with each event value.
/// state: The initial state.
/// sourceEvent: The input event.
val scan : collector:('U -> 'T -> 'U) -> state:'U -> sourceEvent:IEvent<'Del,'T> -> IEvent<'U> when 'Del : delegate<'T, unit> and 'Del :> System.Delegate
/// Runs the given function each time the given event is triggered.
/// callback: The function to call when the event is triggered.
/// sourceEvent: The input event.
val add : callback:('T -> unit) -> sourceEvent:IEvent<'Del,'T> -> unit when 'Del : delegate<'T, unit> and 'Del :> System.Delegate
/// Returns a new event that triggers on the second and subsequent triggerings of the input event.  The Nth triggering of the input event passes the arguments from the N-1th and Nth triggering as a pair. The argument passed to the N-1th triggering is held in hidden internal state until the Nth triggering occurs.
/// sourceEvent: The input event.
val pairwise : sourceEvent:IEvent<'Del,'T> -> IEvent<'T * 'T> when 'Del : delegate<'T, unit> and 'Del :> System.Delegate
