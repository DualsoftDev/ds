/// Basic operations on options.
[<CompilationRepresentation(enum<CompilationRepresentationFlags> (4))>]
module Microsoft.FSharp.Core.Option

open System

/// Returns true if the option is not None.
/// option: The input option.
val isSome : option:'T option -> bool
/// Returns true if the option is None.
/// option: The input option.
val isNone : option:'T option -> bool
/// Gets the value associated with the option.
/// option: The input option.
val get : option:'T option -> 'T
/// count inp evaluates to match inp with None -> 0 | Some _ -> 1.
/// option: The input option.
val count : option:'T option -> int
/// fold f s inp evaluates to match inp with None -> s | Some x -> f s x.
/// folder: A function to update the state data when given a value from an option.
/// state: The initial state.
/// option: The input option.
val fold : folder:('State -> 'T -> 'State) -> state:'State -> option:'T option -> 'State
/// fold f inp s evaluates to match inp with None -> s | Some x -> f x s.
/// folder: A function to update the state data when given a value from an option.
/// option: The input option.
/// state: The initial state.
val foldBack : folder:('T -> 'State -> 'State) -> option:'T option -> state:'State -> 'State
/// exists p inp evaluates to match inp with None -> false | Some x -> p x.
/// predicate: A function that evaluates to a boolean when given a value from the option type.
/// option: The input option.
val exists : predicate:('T -> bool) -> option:'T option -> bool
/// forall p inp evaluates to match inp with None -> true | Some x -> p x.
/// predicate: A function that evaluates to a boolean when given a value from the option type.
/// option: The input option.
val forall : predicate:('T -> bool) -> option:'T option -> bool
/// iter f inp executes match inp with None -> () | Some x -> f x.
/// action: A function to apply to the option value.
/// option: The input option.
val iter : action:('T -> unit) -> option:'T option -> unit
/// map f inp evaluates to match inp with None -> None | Some x -> Some (f x).
/// mapping: A function to apply to the option value.
/// option: The input option.
val map : mapping:('T -> 'U) -> option:'T option -> 'U option
/// bind f inp evaluates to match inp with None -> None | Some x -> f x
/// binder: A function that takes the value of type T from an option and transforms it into an option containing a value of type U.
/// option: The input option.
val bind : binder:('T -> 'U option) -> option:'T option -> 'U option
/// filter f inp evaluates to match inp with None -> None | Some x -> if f x then Some x else None.
/// predicate: A function that evaluates whether the value contained in the option should remain, or be filtered out.
/// option: The input option.
val filter : predicate:('T -> bool) -> option:'T option -> 'T option
/// Convert the option to an array of length 0 or 1.
/// option: The input option.
val toArray : option:'T option -> 'T []
/// Convert the option to a list of length 0 or 1.
/// option: The input option.
val toList : option:'T option -> 'T list
/// Convert the option to a Nullable value.
/// option: The input option.
val toNullable : option:'T option -> Nullable<'T> when 'T : (new : unit -> 'T) and 'T : struct and 'T :> ValueType
/// Convert a Nullable value to an option.
/// value: The input nullable value.
val ofNullable : value:Nullable<'T> -> 'T option when 'T : (new : unit -> 'T) and 'T : struct and 'T :> ValueType
/// Convert a potentially null value to an option.
/// value: The input value.
val ofObj : value:'T -> 'T option when 'T : null
/// Convert an option to a potentially null value.
/// value: The input value.
val toObj : value:'T option -> 'T when 'T : null
