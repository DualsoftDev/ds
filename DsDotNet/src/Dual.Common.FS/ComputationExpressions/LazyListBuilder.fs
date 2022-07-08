// http://tryjoinads.org/docs/computations/layered.html

module Dual.Common.LazyListBuilder

type LazyList<'T> = L of Lazy<LazyListInner<'T>>
and LazyListInner<'T> = Nil | Cons of 'T * LazyList<'T>


/// Creates a lazy list containing a single element
let unit v = L (lazy Cons(v, L (lazy Nil)))
/// Creates a lazy list that is empty
let zero () = L (lazy Nil)

/// Concatenates the elements of two lazy lists 
let rec combine (L l1) (L l2) = L (Lazy.Create(fun () ->
  match l1.Value with 
  | Nil -> l2.Value
  | Cons(x, xs) -> Cons(x, combine xs (L l2))))

/// For every input, generates a new lazy list using
/// the provided function and concatenates the results
let rec bind f (L l) = L (Lazy.Create(fun () ->
  match l.Value with
  | Nil -> Nil
  | Cons(x, xs) -> 
      let (L res) = combine (f x) (bind f xs)
      res.Value))


(*
 * The following code sampel adds a number of functions for converting between lists, lazy lists and lazy values:
 *)
/// Wraps a function with untracked effects into a lazy list
let delay (f:unit -> LazyList<_>) = L (Lazy.Create(fun () ->
  let (L inner) = f () in inner.Value))


/// Turn a lazy value into a singleton list
let ofLazy (l:Lazy<_>) = delay (fun () -> unit (l.Value))
/// Turn a non-lazy list into a lazy list
let rec ofList l = delay (fun () -> 
  match l with
  | [] -> zero ()
  | x::xs -> combine (unit x) (ofList xs))

/// Evaluate a lazy list and obtain a list
let rec toList (L inner) = 
  match inner.Value with
  | Nil -> []
  | Cons(x, xs) -> x::(toList xs)




/// Computation builder for creating lazy lists
type LazyListBuilder() = 
  // Operations from the monoidal structure
  member x.Yield(v) = unit v
  member x.YieldFrom(lv) = lv
  member x.Combine(l1, l2) = combine l1 l2
  member x.Zero() = zero()
  member x.Delay(f) = delay f

  // Bind and lifted bind operators
  member x.For(ll, f) = bind f ll
  member x.For(l, f) = bind f (ofList (List.ofSeq l))
  member x.Bind(lv, f) = bind f (ofLazy lv)

/// Single instance of the computation builder
let lazylist = LazyListBuilder()