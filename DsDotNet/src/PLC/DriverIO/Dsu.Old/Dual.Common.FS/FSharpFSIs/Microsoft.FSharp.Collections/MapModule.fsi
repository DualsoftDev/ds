/// Functional programming operators related to the Map<_,_> type.
[<CompilationRepresentation(enum<CompilationRepresentationFlags> (4))>]
[<RequireQualifiedAccess>]
module Microsoft.FSharp.Collections.Map

val add : key:'Key -> value:'T -> table:Map<'Key,'T> -> Map<'Key,'T> when 'Key : comparison
/// Returns a new map made from the given bindings.
/// elements: The input list of key/value pairs.
val ofList : elements:('Key * 'T) list -> Map<'Key,'T> when 'Key : comparison
/// Returns a new map made from the given bindings.
/// elements: The input array of key/value pairs.
val ofArray : elements:('Key * 'T) [] -> Map<'Key,'T> when 'Key : comparison
/// Returns a new map made from the given bindings.
/// elements: The input sequence of key/value pairs.
val ofSeq : elements:seq<'Key * 'T> -> Map<'Key,'T> when 'Key : comparison
/// Views the collection as an enumerable sequence of pairs.  The sequence will be ordered by the keys of the map.
/// table: The input map.
val toSeq : table:Map<'Key,'T> -> seq<'Key * 'T> when 'Key : comparison
/// Returns a list of all key-value pairs in the mapping.  The list will be ordered by the keys of the map.
/// table: The input map.
val toList : table:Map<'Key,'T> -> ('Key * 'T) list when 'Key : comparison
/// Returns an array of all key-value pairs in the mapping.  The array will be ordered by the keys of the map.
/// table: The input map.
val toArray : table:Map<'Key,'T> -> ('Key * 'T) [] when 'Key : comparison
/// Is the map empty?
/// table: The input map.
val isEmpty : table:Map<'Key,'T> -> bool when 'Key : comparison
/// The empty map.
val empty : Map<'Key,'T> when 'Key : comparison
/// Lookup an element in the map, raising KeyNotFoundException if no binding exists in the map.
/// key: The input key.
/// table: The input map.
val find : key:'Key -> table:Map<'Key,'T> -> 'T when 'Key : comparison
/// Searches the map looking for the first element where the given function returns a Some value.
/// chooser: The function to generate options from the key/value pairs.
/// table: The input map.
val tryPick : chooser:('Key -> 'T -> 'U option) -> table:Map<'Key,'T> -> 'U option when 'Key : comparison
/// Searches the map looking for the first element where the given function returns a Some value
/// chooser: The function to generate options from the key/value pairs.
/// table: The input map.
val pick : chooser:('Key -> 'T -> 'U option) -> table:Map<'Key,'T> -> 'U when 'Key : comparison
/// Folds over the bindings in the map.
/// folder: The function to update the state given the input key/value pairs.
/// table: The input map.
/// state: The initial state.
val foldBack : folder:('Key -> 'T -> 'State -> 'State) -> table:Map<'Key,'T> -> state:'State -> 'State when 'Key : comparison
/// Folds over the bindings in the map
/// folder: The function to update the state given the input key/value pairs.
/// state: The initial state.
/// table: The input map.
val fold : folder:('State -> 'Key -> 'T -> 'State) -> state:'State -> table:Map<'Key,'T> -> 'State when 'Key : comparison
/// Applies the given function to each binding in the dictionary
/// action: The function to apply to each key/value pair.
/// table: The input map.
val iter : action:('Key -> 'T -> unit) -> table:Map<'Key,'T> -> unit when 'Key : comparison
/// Returns true if the given predicate returns true for one of the bindings in the map.
/// predicate: The function to test the input elements.
/// table: The input map.
val exists : predicate:('Key -> 'T -> bool) -> table:Map<'Key,'T> -> bool when 'Key : comparison
/// Builds a new map containing only the bindings for which the given predicate returns 'true'.
/// predicate: The function to test the key/value pairs.
/// table: The input map.
val filter : predicate:('Key -> 'T -> bool) -> table:Map<'Key,'T> -> Map<'Key,'T> when 'Key : comparison
/// Returns true if the given predicate returns true for all of the bindings in the map.
/// predicate: The function to test the input elements.
/// table: The input map.
val forall : predicate:('Key -> 'T -> bool) -> table:Map<'Key,'T> -> bool when 'Key : comparison
/// Builds a new collection whose elements are the results of applying the given function to each of the elements of the collection. The key passed to the function indicates the key of element being transformed.
/// mapping: The function to transform the key/value pairs.
/// table: The input map.
val map : mapping:('Key -> 'T -> 'U) -> table:Map<'Key,'T> -> Map<'Key,'U> when 'Key : comparison
/// Tests if an element is in the domain of the map.
/// key: The input key.
/// table: The input map.
val containsKey : key:'Key -> table:Map<'Key,'T> -> bool when 'Key : comparison
/// Builds two new maps, one containing the bindings for which the given predicate returns 'true', and the other the remaining bindings.
/// predicate: The function to test the input elements.
/// table: The input map.
val partition : predicate:('Key -> 'T -> bool) -> table:Map<'Key,'T> -> Map<'Key,'T> * Map<'Key,'T> when 'Key : comparison
/// Removes an element from the domain of the map. No exception is raised if the element is not present.
/// key: The input key.
/// table: The input map.
val remove : key:'Key -> table:Map<'Key,'T> -> Map<'Key,'T> when 'Key : comparison
/// Lookup an element in the map, returning a Some value if the element is in the domain of the map and None if not.
/// key: The input key.
/// table: The input map.
val tryFind : key:'Key -> table:Map<'Key,'T> -> 'T option when 'Key : comparison
/// Evaluates the function on each mapping in the collection. Returns the key for the first mapping where the function returns 'true'. Raise KeyNotFoundException if no such element exists.
/// predicate: The function to test the input elements.
/// table: The input map.
val findKey : predicate:('Key -> 'T -> bool) -> table:Map<'Key,'T> -> 'Key when 'Key : comparison
/// Returns the key of the first mapping in the collection that satisfies the given predicate. Returns 'None' if no such element exists.
/// predicate: The function to test the input elements.
/// table: The input map.
val tryFindKey : predicate:('Key -> 'T -> bool) -> table:Map<'Key,'T> -> 'Key option when 'Key : comparison
