/// Basic operations on first class event and other observable objects.
[<CompilationRepresentation(enum<CompilationRepresentationFlags> (4))>]
[<RequireQualifiedAccess>]
module Microsoft.FSharp.Control.Observable

/// Returns an observable for the merged observations from the sources. The returned object propagates success and error values arising from either source and completes when both the sources have completed.
/// source1: The first Observable.
/// source2: The second Observable.
val merge : source1:System.IObservable<'T> -> source2:System.IObservable<'T> -> System.IObservable<'T>
/// Returns an observable which transforms the observations of the source by the given function. The transformation function is executed once for each subscribed observer. The returned object also propagates error observations arising from the source and completes when the source completes.
/// mapping: The function applied to observations from the source.
/// source: The input Observable.
val map : mapping:('T -> 'U) -> source:System.IObservable<'T> -> System.IObservable<'U>
/// Returns an observable which filters the observations of the source by the given function. The observable will see only those observations for which the predicate returns true. The predicate is executed once for each subscribed observer. The returned object also propagates error observations arising from the source and completes when the source completes.
/// filter: The function to apply to observations to determine if it should be kept.
/// source: The input Observable.
val filter : predicate:('T -> bool) -> source:System.IObservable<'T> -> System.IObservable<'T>
/// Returns two observables which partition the observations of the source by the given function. The first will trigger observations for those values for which the predicate returns true. The second will trigger observations for those values where the predicate returns false. The predicate is executed once for each subscribed observer. Both also propagate all error observations arising from the source and each completes when the source completes.
/// predicate: The function to determine which output Observable will trigger a particular observation.
/// source: The input Observable.
val partition : predicate:('T -> bool) -> source:System.IObservable<'T> -> System.IObservable<'T> * System.IObservable<'T>
/// Returns two observables which split the observations of the source by the given function. The first will trigger observations x for which the splitter returns Choice1Of2 x. The second will trigger observations y for which the splitter returns Choice2Of2 y The splitter is executed once for each subscribed observer. Both also propagate error observations arising from the source and each completes when the source completes.
/// splitter: The function that takes an observation an transforms it into one of the two output Choice types.
/// source: The input Observable.
val split : splitter:('T -> Choice<'U1,'U2>) -> source:System.IObservable<'T> -> System.IObservable<'U1> * System.IObservable<'U2>
/// Returns an observable which chooses a projection of observations from the source using the given function. The returned object will trigger observations x for which the splitter returns Some x. The returned object also propagates all errors arising from the source and completes when the source completes.
/// chooser: The function that returns Some for observations to be propagated and None for observations to ignore.
/// source: The input Observable.
val choose : chooser:('T -> 'U option) -> source:System.IObservable<'T> -> System.IObservable<'U>
/// Returns an observable which, for each observer, allocates an item of state and applies the given accumulating function to successive values arising from the input. The returned object will trigger observations for each computed state value, excluding the initial value. The returned object propagates all errors arising from the source and completes when the source completes.
/// collector: The function to update the state with each observation.
/// state: The initial state.
/// source: The input Observable.
val scan : collector:('U -> 'T -> 'U) -> state:'U -> source:System.IObservable<'T> -> System.IObservable<'U>
/// Creates an observer which permanently subscribes to the given observable and which calls the given function for each observation.
/// callback: The function to be called on each observation.
/// source: The input Observable.
val add : callback:('T -> unit) -> source:System.IObservable<'T> -> unit
/// Creates an observer which subscribes to the given observable and which calls the given function for each observation.
/// callback: The function to be called on each observation.
/// source: The input Observable.
val subscribe : callback:('T -> unit) -> source:System.IObservable<'T> -> System.IDisposable
/// Returns a new observable that triggers on the second and subsequent triggerings of the input observable.  The Nth triggering of the input observable passes the arguments from the N-1th and Nth triggering as a pair. The argument passed to the N-1th triggering is held in hidden internal state until the Nth triggering occurs.
/// source: The input Observable.
val pairwise : source:System.IObservable<'T> -> System.IObservable<'T * 'T>
