/// Functional programming operators related to the Set<_> type.
[<CompilationRepresentation(enum<CompilationRepresentationFlags> (4))>]
[<RequireQualifiedAccess>]
module Microsoft.FSharp.Collections.Set

/// The empty set for the type 'T.
val empty : Set<'T> when 'T : comparison
/// The set containing the given element.
/// value: The value for the set to contain.
val singleton : value:'T -> Set<'T> when 'T : comparison
/// Returns a new set with an element added to the set. No exception is raised if the set already contains the given element.
/// value: The value to add.
/// set: The input set.
val add : value:'T -> set:Set<'T> -> Set<'T> when 'T : comparison
/// Evaluates to "true" if the given element is in the given set.
/// element: The element to test.
/// set: The input set.
val contains : element:'T -> set:Set<'T> -> bool when 'T : comparison
/// Evaluates to "true" if all elements of the first set are in the second
/// set1: The potential subset.
/// set2: The set to test against.
val isSubset : set1:Set<'T> -> set2:Set<'T> -> bool when 'T : comparison
/// Evaluates to "true" if all elements of the first set are in the second, and at least one element of the second is not in the first.
/// set1: The potential subset.
/// set2: The set to test against.
val isProperSubset : set1:Set<'T> -> set2:Set<'T> -> bool when 'T : comparison
/// Evaluates to "true" if all elements of the second set are in the first.
/// set1: The potential superset.
/// set2: The set to test against.
val isSuperset : set1:Set<'T> -> set2:Set<'T> -> bool when 'T : comparison
/// Evaluates to "true" if all elements of the second set are in the first, and at least one element of the first is not in the second.
/// set1: The potential superset.
/// set2: The set to test against.
val isProperSuperset : set1:Set<'T> -> set2:Set<'T> -> bool when 'T : comparison
/// Returns the number of elements in the set. Same as size.
/// set: The input set.
val count : set:Set<'T> -> int when 'T : comparison
/// Tests if any element of the collection satisfies the given predicate.  If the input function is predicate and the elements are i0...iN then computes p i0 or ... or p iN.
/// predicate: The function to test set elements.
/// set: The input set.
val exists : predicate:('T -> bool) -> set:Set<'T> -> bool when 'T : comparison
/// Returns a new collection containing only the elements of the collection for which the given predicate returns true.
/// predicate: The function to test set elements.
/// set: The input set.
val filter : predicate:('T -> bool) -> set:Set<'T> -> Set<'T> when 'T : comparison
/// Returns a new collection containing the results of applying the given function to each element of the input set.
/// mapping: The function to transform elements of the input set.
/// set: The input set.
val map : mapping:('T -> 'U) -> set:Set<'T> -> Set<'U> when 'T : comparison and 'U : comparison
/// Applies the given accumulating function to all the elements of the set
/// folder: The accumulating function.
/// state: The initial state.
/// set: The input set.
val fold : folder:('State -> 'T -> 'State) -> state:'State -> set:Set<'T> -> 'State when 'T : comparison
/// Applies the given accumulating function to all the elements of the set.
/// folder: The accumulating function.
/// set: The input set.
/// state: The initial state.
val foldBack : folder:('T -> 'State -> 'State) -> set:Set<'T> -> state:'State -> 'State when 'T : comparison
/// Tests if all elements of the collection satisfy the given predicate.  If the input function is f and the elements are i0...iN and "j0...jN" then computes p i0 && ... && p iN.
/// predicate: The function to test set elements.
/// set: The input set.
val forall : predicate:('T -> bool) -> set:Set<'T> -> bool when 'T : comparison
/// Computes the intersection of the two sets.
/// set1: The first input set.
/// set2: The second input set.
val intersect : set1:Set<'T> -> set2:Set<'T> -> Set<'T> when 'T : comparison
/// Computes the intersection of a sequence of sets. The sequence must be non-empty.
/// sets: The sequence of sets to intersect.
val intersectMany : sets:seq<Set<'T>> -> Set<'T> when 'T : comparison
/// Computes the union of the two sets.
/// set1: The first input set.
/// set2: The second input set.
val union : set1:Set<'T> -> set2:Set<'T> -> Set<'T> when 'T : comparison
/// Computes the union of a sequence of sets.
/// sets: The sequence of sets to union.
val unionMany : sets:seq<Set<'T>> -> Set<'T> when 'T : comparison
/// Returns "true" if the set is empty.
/// set: The input set.
val isEmpty : set:Set<'T> -> bool when 'T : comparison
/// Applies the given function to each element of the set, in order according to the comparison function.
/// action: The function to apply to each element.
/// set: The input set.
val iter : action:('T -> unit) -> set:Set<'T> -> unit when 'T : comparison
/// Splits the set into two sets containing the elements for which the given predicate returns true and false respectively.
/// predicate: The function to test set elements.
/// set: The input set.
val partition : predicate:('T -> bool) -> set:Set<'T> -> Set<'T> * Set<'T> when 'T : comparison
/// Returns a new set with the given element removed. No exception is raised if the set doesn't contain the given element.
/// value: The element to remove.
/// set: The input set.
val remove : value:'T -> set:Set<'T> -> Set<'T> when 'T : comparison
/// Returns the lowest element in the set according to the ordering being used for the set.
/// set: The input set.
val minElement : set:Set<'T> -> 'T when 'T : comparison
/// Returns the highest element in the set according to the ordering being used for the set.
/// set: The input set.
val maxElement : set:Set<'T> -> 'T when 'T : comparison
/// Builds a set that contains the same elements as the given list.
/// elements: The input list.
val ofList : elements:'T list -> Set<'T> when 'T : comparison
/// Builds a list that contains the elements of the set in order.
/// set: The input set.
val toList : set:Set<'T> -> 'T list when 'T : comparison
/// Builds a set that contains the same elements as the given array.
/// array: The input array.
val ofArray : array:'T [] -> Set<'T> when 'T : comparison
/// Builds an array that contains the elements of the set in order.
/// set: The input set.
val toArray : set:Set<'T> -> 'T [] when 'T : comparison
/// Returns an ordered view of the collection as an enumerable object.
/// set: The input set.
val toSeq : set:Set<'T> -> seq<'T> when 'T : comparison
/// Builds a new collection from the given enumerable object.
/// elements: The input sequence.
val ofSeq : elements:seq<'T> -> Set<'T> when 'T : comparison
/// Returns a new set with the elements of the second set removed from the first.
/// set1: The first input set.
/// set2: The set whose elements will be removed from set1.
val difference : set1:Set<'T> -> set2:Set<'T> -> Set<'T> when 'T : comparison
