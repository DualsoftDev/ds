[<AutoOpen>]
module Microsoft.FSharp.Core.ExtraTopLevelOperators

open System
open System.IO

/// Print to stdout using the given format.
/// format: The formatter.
val printf : format:Printf.TextWriterFormat<'T> -> 'T
/// Print to stdout using the given format, and add a newline.
/// format: The formatter.
val printfn : format:Printf.TextWriterFormat<'T> -> 'T
/// Print to stderr using the given format.
/// format: The formatter.
val eprintf : format:Printf.TextWriterFormat<'T> -> 'T
/// Print to stderr using the given format, and add a newline.
/// format: The formatter.
val eprintfn : format:Printf.TextWriterFormat<'T> -> 'T
/// Print to a string using the given format.
/// format: The formatter.
val sprintf : format:Printf.StringFormat<'T> -> 'T
/// Print to a string buffer and raise an exception with the given result. Helper printers must return strings.
/// format: The formatter.
val failwithf : format:Printf.StringFormat<'T,'Result> -> 'T
/// Print to a file using the given format.
/// textWriter: The file TextWriter.
/// format: The formatter.
val fprintf : textWriter:TextWriter -> format:Printf.TextWriterFormat<'T> -> 'T
/// Print to a file using the given format, and add a newline.
/// textWriter: The file TextWriter.
/// format: The formatter.
val fprintfn : textWriter:TextWriter -> format:Printf.TextWriterFormat<'T> -> 'T
/// Builds a set from a sequence of objects. The objects are indexed using generic comparison.
/// elements: The input sequence of elements.
val set : elements:seq<'T> -> Set<'T> when 'T : comparison
/// Builds an asynchronous workflow using computation expression syntax.
val async : AsyncBuilder
/// Converts the argument to 32-bit float.
val inline single : value: ^T -> single when ^T : (static member op_Explicit : unit -> single)
/// Converts the argument to 64-bit float.
val inline double : value: ^T -> double when ^T : (static member op_Explicit : unit -> double)
/// Converts the argument to byte.
val inline uint8 : value: ^T -> uint8 when ^T : (static member op_Explicit : unit -> uint8)
/// Converts the argument to signed byte.
val inline int8 : value: ^T -> int8 when ^T : (static member op_Explicit : unit -> int8)
/// Builds a read-only lookup table from a sequence of key/value pairs. The key objects are indexed using generic hashing and equality.
val dict : keyValuePairs:seq<'Key * 'Value> -> Collections.Generic.IDictionary<'Key,'Value> when 'Key : equality
/// Builds a 2D array from a sequence of sequences of elements.
val array2D : rows:seq<'a> -> 'T [,] when 'T3415 :> seq<'T>
/// Special prefix operator for splicing typed expressions into quotation holes.
val ( ~% ) : expression:Quotations.Expr<'T> -> 'T
/// Special prefix operator for splicing untyped expressions into quotation holes.
val ( ~%% ) : expression:Quotations.Expr -> 'T
/// An active pattern to force the execution of values of type Lazy<_>.
val (|Lazy|) : input:Lazy<'T> -> 'T
/// Builds a query using query syntax and operators.
val query : Linq.QueryBuilder

module Checked = 
    
    /// Converts the argument to byte.
    val inline uint8 : value: ^T -> byte when ^T : (static member op_Explicit : unit -> uint8)
    /// Converts the argument to signed byte.
    val inline int8 : value: ^T -> sbyte when ^T : (static member op_Explicit : unit -> int8)

