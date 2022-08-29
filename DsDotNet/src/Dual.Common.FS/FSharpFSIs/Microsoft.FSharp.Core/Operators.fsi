/// Basic F# Operators. This module is automatically opened in all F# code.
[<AutoOpen>]
module Microsoft.FSharp.Core.Operators

open System

/// Overloaded unary negation.
/// n: The value to negate.
val inline ( ~- ) : n: ^T ->  ^T when ^T : (static member ( ~- ) : unit ->  ^T)
/// Overloaded addition operator
/// x: The first parameter.
/// y: The second parameter.
val inline ( + ) : x: ^T1 -> y: ^T2 ->  ^T3 when ^T1 : (static member ( + ) :  ^T1 *  ^T2 ->  ^T3) and ^T2 : (static member ( + ) :  ^T1 *  ^T2 ->  ^T3)
/// Overloaded subtraction operator
/// x: The first parameter.
/// y: The second parameter.
val inline ( - ) : x: ^T1 -> y: ^T2 ->  ^T3 when ^T1 : (static member ( - ) :  ^T1 *  ^T2 ->  ^T3) and ^T2 : (static member ( - ) :  ^T1 *  ^T2 ->  ^T3)
/// Overloaded multiplication operator
/// x: The first parameter.
/// y: The second parameter.
val inline ( * ) : x: ^T1 -> y: ^T2 ->  ^T3 when ^T1 : (static member ( * ) :  ^T1 *  ^T2 ->  ^T3) and ^T2 : (static member ( * ) :  ^T1 *  ^T2 ->  ^T3)
/// Overloaded division operator
/// x: The first parameter.
/// y: The second parameter.
val inline ( / ) : x: ^T1 -> y: ^T2 ->  ^T3 when ^T1 : (static member ( / ) :  ^T1 *  ^T2 ->  ^T3) and ^T2 : (static member ( / ) :  ^T1 *  ^T2 ->  ^T3)
/// Overloaded modulo operator
/// x: The first parameter.
/// y: The second parameter.
val inline ( % ) : x: ^T1 -> y: ^T2 ->  ^T3 when ^T1 : (static member ( % ) :  ^T1 *  ^T2 ->  ^T3) and ^T2 : (static member ( % ) :  ^T1 *  ^T2 ->  ^T3)
/// Overloaded bitwise-AND operator
/// x: The first parameter.
/// y: The second parameter.
val inline ( &&& ) : x: ^T -> y: ^T ->  ^T when ^T : (static member ( &&& ) :  ^T *  ^T ->  ^T)
/// Overloaded bitwise-OR operator
/// x: The first parameter.
/// y: The second parameter.
val inline ( ||| ) : x: ^T -> y: ^T ->  ^T when ^T : (static member ( ||| ) :  ^T *  ^T ->  ^T)
/// Overloaded bitwise-XOR operator
/// x: The first parameter.
/// y: The second parameter.
val inline ( ^^^ ) : x: ^T -> y: ^T ->  ^T when ^T : (static member ( ^^^ ) :  ^T *  ^T ->  ^T)
/// Overloaded byte-shift left operator by a specified number of bits
/// value: The input value.
/// shift: The amount to shift.
val inline ( <<< ) : value: ^T -> shift:int32 ->  ^T when ^T : (static member ( <<< ) :  ^T * int32 ->  ^T)
/// Overloaded byte-shift right operator by a specified number of bits
/// value: The input value.
/// shift: The amount to shift.
val inline ( >>> ) : value: ^T -> shift:int32 ->  ^T when ^T : (static member ( >>> ) :  ^T * int32 ->  ^T)
/// Overloaded bitwise-NOT operator
/// value: The input value.
val inline ( ~~~ ) : value: ^T ->  ^T when ^T : (static member ( ~~~ ) : unit ->  ^T)
/// Overloaded prefix-plus operator
/// value: The input value.
val inline ( ~+ ) : value: ^T ->  ^T when ^T : (static member ( ~+ ) : unit ->  ^T)
/// Structural less-than comparison
/// x: The first parameter.
/// y: The second parameter.
val inline ( < ) : x:'T -> y:'T -> bool when 'T : comparison
/// Structural greater-than
/// x: The first parameter.
/// y: The second parameter.
val inline ( > ) : x:'T -> y:'T -> bool when 'T : comparison
/// Structural greater-than-or-equal
/// x: The first parameter.
/// y: The second parameter.
val inline ( >= ) : x:'T -> y:'T -> bool when 'T : comparison
/// Structural less-than-or-equal comparison
/// x: The first parameter.
/// y: The second parameter.
val inline ( <= ) : x:'T -> y:'T -> bool when 'T : comparison
/// Structural equality
/// x: The first parameter.
/// y: The second parameter.
val inline ( = ) : x:'T -> y:'T -> bool when 'T : equality
/// Structural inequality
/// x: The first parameter.
/// y: The second parameter.
val inline ( <> ) : x:'T -> y:'T -> bool when 'T : equality
/// Compose two functions, the function on the left being applied first
/// func1: The first function to apply.
/// func2: The second function to apply.
val inline ( >> ) : func1:('T1 -> 'T2) -> func2:('T2 -> 'T3) -> 'T1 -> 'T3
/// Compose two functions, the function on the right being applied first
/// func2: The second function to apply.
/// func1: The first function to apply.
val inline ( << ) : func2:('T2 -> 'T3) -> func1:('T1 -> 'T2) -> 'T1 -> 'T3
/// Apply a function to a value, the value being on the left, the function on the right
/// arg: The argument.
/// func: The function.
val inline ( |> ) : arg:'T1 -> func:('T1 -> 'U) -> 'U
/// Apply a function to two values, the values being a pair on the left, the function on the right
/// arg1: The first argument.
/// arg2: The second argument.
/// func: The function.
val inline ( ||> ) : arg1:'T1 * arg2:'T2 -> func:('T1 -> 'T2 -> 'U) -> 'U
/// Apply a function to three values, the values being a triple on the left, the function on the right
/// arg1: The first argument.
/// arg2: The second argument.
/// arg3: The third argument.
/// func: The function.
val inline ( |||> ) : arg1:'T1 * arg2:'T2 * arg3:'T3 -> func:('T1 -> 'T2 -> 'T3 -> 'U) -> 'U
/// Apply a function to a value, the value being on the right, the function on the left
/// func: The function.
/// arg1: The argument.
val inline ( <| ) : func:('T -> 'U) -> arg1:'T -> 'U
/// Apply a function to two values, the values being a pair on the right, the function on the left
/// func: The function.
/// arg1: The first argument.
/// arg2: The second argument.
val inline ( <|| ) : func:('T1 -> 'T2 -> 'U) -> arg1:'T1 * arg2:'T2 -> 'U
/// Apply a function to three values, the values being a triple on the right, the function on the left
/// func: The function.
/// arg1: The first argument.
/// arg2: The second argument.
/// arg3: The third argument.
val inline ( <||| ) : func:('T1 -> 'T2 -> 'T3 -> 'U) -> arg1:'T1 * arg2:'T2 * arg3:'T3 -> 'U
/// Used to specify a default value for an optional argument in the implementation of a function
/// arg: An option representing the argument.
/// defaultValue: The default value of the argument.
val defaultArg : arg:'T option -> defaultValue:'T -> 'T
/// Concatenate two strings. The operator '+' may also be used.
val ( ^ ) : s1:string -> s2:string -> string
/// Raises an exception
/// exn: The exception to raise.
val raise : exn:Exception -> 'T
/// Rethrows an exception. This should only be used when handling an exception
val inline rethrow : unit -> 'T
/// Rethrows an exception. This should only be used when handling an exception
val inline reraise : unit -> 'T
/// Builds a System.Exception object.
/// message: The message for the Exception.
val Failure : message:string -> exn
/// Matches System.Exception objects whose runtime type is precisely System.Exception
/// error: The input exception.
val (|Failure|_|) : error:exn -> string option
/// Return the first element of a tuple, fst (a,b) = a.
/// tuple: The input tuple.
val fst : tuple:('T1 * 'T2) -> 'T1
/// Return the second element of a tuple, snd (a,b) = b.
/// tuple: The input tuple.
val snd : tuple:('T1 * 'T2) -> 'T2
/// Generic comparison.
/// e1: The first value.
/// e2: The second value.
val inline compare : e1:'T -> e2:'T -> int when 'T : comparison
/// Maximum based on generic comparison
/// e1: The first value.
/// e2: The second value.
val inline max : e1:'T -> e2:'T -> 'T when 'T : comparison
/// Minimum based on generic comparison
/// e1: The first value.
/// e2: The second value.
val inline min : e1:'T -> e2:'T -> 'T when 'T : comparison
/// Ignore the passed value. This is often used to throw away results of a computation.
/// value: The value to ignore.
val inline ignore : value:'T -> unit
/// Unbox a strongly typed value.
/// value: The boxed value.
val inline unbox : value:obj -> 'T
/// Boxes a strongly typed value.
/// value: The value to box.
val inline box : value:'T -> obj
/// Try to unbox a strongly typed value.
/// value: The boxed value.
val inline tryUnbox : value:obj -> 'T option
/// Determines whether the given value is null.
/// value: The value to check.
val inline isNull : value:'T -> bool when 'T : null
/// Throw a System.Exception exception.
/// message: The exception message.
val failwith : message:string -> 'T
/// Throw a System.ArgumentException exception with the given argument name and message.
/// argumentName: The argument name.
/// message: The exception message.
val inline invalidArg : argumentName:string -> message:string -> 'T
/// Throw a System.ArgumentNullException exception
/// argumentName: The argument name.
val inline nullArg : argumentName:string -> 'T
/// Throw a System.InvalidOperationException exception
/// message: The exception message.
val inline invalidOp : message:string -> 'T
/// The identity function
/// x: The input value.
val id : x:'T -> 'T
/// Create a mutable reference cell
/// value: The value to contain in the cell.
val ref : value:'T -> 'T ref
/// Assign to a mutable reference cell
/// cell: The cell to mutate.
/// value: The value to set inside the cell.
val ( := ) : cell:'T ref -> value:'T -> unit
/// Dereference a mutable reference cell
/// cell: The cell to dereference.
val ( ! ) : cell:'T ref -> 'T
/// Decrement a mutable reference cell containing an integer
/// cell: The reference cell.
val decr : cell:int ref -> unit
/// Increment a mutable reference cell containing an integer
/// cell: The reference cell.
val incr : cell:int ref -> unit
/// Concatenate two lists.
/// list1: The first list.
/// list2: The second list.
val ( @ ) : list1:'T list -> list2:'T list -> 'T list
/// Negate a logical value. not true equals false and not false equals true
/// value: The value to negate.
val inline not : value:bool -> bool
/// Builds a sequence using sequence expression syntax
/// sequence: The input sequence.
val seq : sequence:seq<'T> -> seq<'T>
/// Exit the current hardware isolated process, if security settings permit, otherwise raise an exception. Calls System.Environment.Exit.
/// exitcode: The exit code to use.
val exit : exitcode:int -> 'T
/// Equivalent to System.Double.PositiveInfinity
val infinity : float
/// Equivalent to System.Double.NaN
val nan : float
/// Equivalent to System.Single.PositiveInfinity
val infinityf : float32
/// Equivalent to System.Single.NaN
val nanf : float32
/// Reads the value of the property System.Console.In.
val stdin : IO.TextReader
/// Reads the value of the property System.Console.Error.
val stderr : IO.TextWriter
/// Reads the value of the property System.Console.Out.
val stdout : IO.TextWriter
/// The standard overloaded range operator, e.g. [n..m] for lists, seq {n..m} for sequences
/// start: The start value of the range.
/// finish: The end value of the range.
val inline ( .. ) : start: ^T -> finish: ^T -> seq< ^T> when ^T : (static member ( + ) :  ^T *  ^T ->  ^T) and ^T : (static member One :  ^T) and ^T : comparison
/// The standard overloaded skip range operator, e.g. [n..skip..m] for lists, seq {n..skip..m} for sequences
/// start: The start value of the range.
/// step: The step value of the range.
/// finish: The end value of the range.
val inline ( .. .. ) : start: ^T -> step: ^Step -> finish: ^T -> seq< ^T> when ^T : (static member ( + ) :  ^T *  ^Step ->  ^T) and ^T : comparison and ^Step : (static member ( + ) :  ^T *  ^Step ->  ^T) and ^Step : (static member Zero :  ^Step)
/// Execute the function as a mutual-exclusion region using the input value as a lock.
/// lockObject: The object to be locked.
/// action: The action to perform during the lock.
val inline lock : lockObject:'Lock -> action:(unit -> 'T) -> 'T when 'Lock : not struct
/// Clean up resources associated with the input object after the completion of the given function.  Cleanup occurs even when an exception is raised by the protected code.
/// resource: The resource to be disposed after action is called.
/// action: The action that accepts the resource.
val using : resource:'T -> action:('T -> 'U) -> 'U when 'T :> IDisposable
/// Generate a System.Type runtime representation of a static type.
val typeof : Type
/// An internal, library-only compiler intrinsic for compile-time generation of a RuntimeMethodHandle.
val methodhandleof : ('T -> 'TResult) -> RuntimeMethodHandle
/// Generate a System.Type representation for a type definition. If the input type is a generic type instantiation then return the generic type definition associated with all such instantiations.
val typedefof : Type
/// Returns the internal size of a type in bytes. For example, sizeof<int> returns 4.
val sizeof : int
/// A generic hash function, designed to return equal hash values for items that are equal according to the "=" operator. By default it will use structural hashing for F# union, record and tuple types, hashing the complete contents of the type. The exact behaviour of the function can be adjusted on a type-by-type basis by implementing GetHashCode for each type.
/// obj: The input object.
val inline hash : obj:'T -> int when 'T : equality
/// A generic hash function. This function has the same behaviour as 'hash', however the default structural hashing for F# union, record and tuple types stops when the given limit of nodes is reached. The exact behaviour of the function can be adjusted on a type-by-type basis by implementing GetHashCode for each type.
/// limit: The limit of nodes.
/// obj: The input object.
val inline limitedHash : limit:int -> obj:'T -> int when 'T : equality
/// Absolute value of the given number.
/// value: The input value.
val inline abs : value: ^T ->  ^T when ^T : (static member Abs : unit ->  ^T)
/// Inverse cosine of the given number
/// value: The input value.
val inline acos : value: ^T ->  ^T when ^T : (static member Acos : unit ->  ^T)
/// Inverse sine of the given number
/// value: The input value.
val inline asin : value: ^T ->  ^T when ^T : (static member Asin : unit ->  ^T)
/// Inverse tangent of the given number
/// value: The input value.
val inline atan : value: ^T ->  ^T when ^T : (static member Atan : unit ->  ^T)
/// Inverse tangent of x/y where x and y are specified separately
/// y: The y input value.
/// x: The x input value.
val inline atan2 : y: ^T1 -> x: ^T1 -> 'T2 when ^T1 : (static member Atan2 :  ^T1 *  ^T1 -> 'T2)
/// Ceiling of the given number
/// value: The input value.
val inline ceil : value: ^T ->  ^T when ^T : (static member Ceiling : unit ->  ^T)
/// Exponential of the given number
/// value: The input value.
val inline exp : value: ^T ->  ^T when ^T : (static member Exp : unit ->  ^T)
/// Floor of the given number
/// value: The input value.
val inline floor : value: ^T ->  ^T when ^T : (static member Floor : unit ->  ^T)
/// Sign of the given number
/// value: The input value.
val inline sign : value: ^T -> int when ^T : (member Sign : int)
/// Round the given number
/// value: The input value.
val inline round : value: ^T ->  ^T when ^T : (static member Round : unit ->  ^T)
/// Natural logarithm of the given number
/// value: The input value.
val inline log : value: ^T ->  ^T when ^T : (static member Log : unit ->  ^T)
/// Logarithm to base 10 of the given number
/// value: The input value.
val inline log10 : value: ^T ->  ^T when ^T : (static member Log10 : unit ->  ^T)
/// Square root of the given number
/// value: The input value.
val inline sqrt : value: ^T ->  ^U when ^T : (static member Sqrt : unit ->  ^U)
/// Cosine of the given number
/// value: The input value.
val inline cos : value: ^T ->  ^T when ^T : (static member Cos : unit ->  ^T)
/// Hyperbolic cosine of the given number
/// value: The input value.
val inline cosh : value: ^T ->  ^T when ^T : (static member Cosh : unit ->  ^T)
/// Sine of the given number
/// value: The input value.
val inline sin : value: ^T ->  ^T when ^T : (static member Sin : unit ->  ^T)
/// Hyperbolic sine of the given number
/// value: The input value.
val inline sinh : value: ^T ->  ^T when ^T : (static member Sinh : unit ->  ^T)
/// Tangent of the given number
/// value: The input value.
val inline tan : value: ^T ->  ^T when ^T : (static member Tan : unit ->  ^T)
/// Hyperbolic tangent of the given number
/// value: The input value.
val inline tanh : value: ^T ->  ^T when ^T : (static member Tanh : unit ->  ^T)
/// Overloaded truncate operator.
/// value: The input value.
val inline truncate : value: ^T ->  ^T when ^T : (static member Truncate : unit ->  ^T)
/// Overloaded power operator.
/// x: The input base.
/// y: The input exponent.
val inline ( ** ) : x: ^T -> y: ^U ->  ^T when ^T : (static member Pow :  ^T *  ^U ->  ^T)
/// Overloaded power operator. If n > 0 then equivalent to x*...*x for n occurrences of x.
/// x: The input base.
/// n: The input exponent.
val inline pown : x: ^T -> n:int ->  ^T when ^T : (static member One :  ^T) and ^T : (static member ( * ) :  ^T *  ^T ->  ^T) and ^T : (static member ( / ) :  ^T *  ^T ->  ^T)
/// Converts the argument to byte. This is a direct conversion for all primitive numeric types. For strings, the input is converted using Byte.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
/// value: The input value.
val inline byte : value: ^T -> byte when ^T : (static member op_Explicit : unit -> byte)
/// Converts the argument to signed byte. This is a direct conversion for all primitive numeric types. For strings, the input is converted using SByte.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
/// value: The input value.
val inline sbyte : value: ^T -> sbyte when ^T : (static member op_Explicit : unit -> sbyte)
/// Converts the argument to signed 16-bit integer. This is a direct conversion for all primitive numeric types. For strings, the input is converted using Int16.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
/// value: The input value.
val inline int16 : value: ^T -> int16 when ^T : (static member op_Explicit : unit -> int16)
/// Converts the argument to unsigned 16-bit integer. This is a direct conversion for all primitive numeric types. For strings, the input is converted using UInt16.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
/// value: The input value.
val inline uint16 : value: ^T -> uint16 when ^T : (static member op_Explicit : unit -> uint16)
/// Converts the argument to signed 32-bit integer. This is a direct conversion for all primitive numeric types. For strings, the input is converted using Int32.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
/// value: The input value.
val inline int : value: ^T -> int when ^T : (static member op_Explicit : unit -> int)
/// Converts the argument to a particular enum type.
/// value: The input value.
val inline enum : value:int32 ->  ^U when 'U : enum<int32>
/// Converts the argument to signed 32-bit integer. This is a direct conversion for all primitive numeric types. For strings, the input is converted using Int32.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
/// value: The input value.
val inline int32 : value: ^T -> int32 when ^T : (static member op_Explicit : unit -> int32)
/// Converts the argument to unsigned 32-bit integer. This is a direct conversion for all primitive numeric types. For strings, the input is converted using UInt32.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
/// value: The input value.
val inline uint32 : value: ^T -> uint32 when ^T : (static member op_Explicit : unit -> uint32)
/// Converts the argument to signed 64-bit integer. This is a direct conversion for all primitive numeric types. For strings, the input is converted using Int64.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
/// value: The input value.
val inline int64 : value: ^T -> int64 when ^T : (static member op_Explicit : unit -> int64)
/// Converts the argument to unsigned 64-bit integer. This is a direct conversion for all primitive numeric types. For strings, the input is converted using UInt64.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
/// value: The input value.
val inline uint64 : value: ^T -> uint64 when ^T : (static member op_Explicit : unit -> uint64)
/// Converts the argument to 32-bit float. This is a direct conversion for all primitive numeric types. For strings, the input is converted using Single.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
/// value: The input value.
val inline float32 : value: ^T -> float32 when ^T : (static member op_Explicit : unit -> float32)
/// Converts the argument to 64-bit float. This is a direct conversion for all primitive numeric types. For strings, the input is converted using Double.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
/// value: The input value.
val inline float : value: ^T -> float when ^T : (static member op_Explicit : unit -> float)
/// Converts the argument to signed native integer. This is a direct conversion for all primitive numeric types. Otherwise the operation requires an appropriate static conversion method on the input type.
/// value: The input value.
val inline nativeint : value: ^T -> nativeint when ^T : (static member op_Explicit : unit -> nativeint)
/// Converts the argument to unsigned native integer using a direct conversion for all primitive numeric types. Otherwise the operation requires an appropriate static conversion method on the input type.
/// value: The input value.
val inline unativeint : value: ^T -> unativeint when ^T : (static member op_Explicit : unit -> unativeint)
/// Converts the argument to a string using ToString.
/// value: The input value.
val inline string : value: ^T -> string
/// Converts the argument to System.Decimal using a direct conversion for all primitive numeric types. For strings, the input is converted using UInt64.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
/// value: The input value.
val inline decimal : value: ^T -> decimal when ^T : (static member op_Explicit : unit -> decimal)
/// Converts the argument to character. Numeric inputs are converted according to the UTF-16 encoding for characters. String inputs must be exactly one character long. For other input types the operation requires an appropriate static conversion method on the input type.
/// value: The input value.
val inline char : value: ^T -> char when ^T : (static member op_Explicit : unit -> char)
/// An active pattern to match values of type System.Collections.Generic.KeyValuePair
/// keyValuePair: The input key/value pair.
val (|KeyValue|) : keyValuePair:Collections.Generic.KeyValuePair<'Key,'Value> -> 'Key * 'Value

/// A module of compiler intrinsic functions for efficient implementations of F# integer ranges and dynamic invocations of other F# operators
module OperatorIntrinsics = 
    
    /// Gets a slice of an array
    /// source: The input array.
    /// start: The start index.
    /// finish: The end index.
    val inline GetArraySlice : source:'T [] -> start:int option -> finish:int option -> 'T []
    /// Sets a slice of an array
    /// target: The target array.
    /// start: The start index.
    /// finish: The end index.
    /// source: The source array.
    val inline SetArraySlice : target:'T [] -> start:int option -> finish:int option -> source:'T [] -> unit
    /// Gets a region slice of an array
    /// source: The source array.
    /// start1: The start index of the first dimension.
    /// finish1: The end index of the first dimension.
    /// start2: The start index of the second dimension.
    /// finish2: The end index of the second dimension.
    val GetArraySlice2D : source:'T [,] -> start1:int option -> finish1:int option -> start2:int option -> finish2:int option -> 'T [,]
    /// Gets a vector slice of a 2D array. The index of the first dimension is fixed.
    /// source: The source array.
    /// index1: The index of the first dimension.
    /// start2: The start index of the second dimension.
    /// finish2: The end index of the second dimension.
    val inline GetArraySlice2DFixed1 : source:'T [,] -> index1:int -> start2:int option -> finish2:int option -> 'T []
    /// Gets a vector slice of a 2D array. The index of the second dimension is fixed.
    /// source: The source array.
    /// start1: The start index of the first dimension.
    /// finish1: The end index of the first dimension.
    /// index2: The fixed index of the second dimension.
    val inline GetArraySlice2DFixed2 : source:'T [,] -> start1:int option -> finish1:int option -> index2:int -> 'T []
    /// Sets a region slice of an array
    /// target: The target array.
    /// start1: The start index of the first dimension.
    /// finish1: The end index of the first dimension.
    /// start2: The start index of the second dimension.
    /// finish2: The end index of the second dimension.
    /// source: The source array.
    val SetArraySlice2D : target:'T [,] -> start1:int option -> finish1:int option -> start2:int option -> finish2:int option -> source:'T [,] -> unit
    /// Sets a vector slice of a 2D array. The index of the first dimension is fixed.
    /// target: The target array.
    /// index1: The index of the first dimension.
    /// start2: The start index of the second dimension.
    /// finish2: The end index of the second dimension.
    /// source: The source array.
    val inline SetArraySlice2DFixed1 : target:'T [,] -> index1:int -> start2:int option -> finish2:int option -> source:'T [] -> unit
    /// Sets a vector slice of a 2D array. The index of the second dimension is fixed.
    /// target: The target array.
    /// start1: The start index of the first dimension.
    /// finish1: The end index of the first dimension.
    /// index2: The index of the second dimension.
    /// source: The source array.
    val inline SetArraySlice2DFixed2 : target:'T [,] -> start1:int option -> finish1:int option -> index2:int -> source:'T [] -> unit
    /// Gets a slice of an array
    /// source: The source array.
    /// start1: The start index of the first dimension.
    /// finish1: The end index of the first dimension.
    /// start2: The start index of the second dimension.
    /// finish2: The end index of the second dimension.
    /// start3: The start index of the third dimension.
    /// finish3: The end index of the third dimension.
    val GetArraySlice3D : source:'T [,,] -> start1:int option -> finish1:int option -> start2:int option -> finish2:int option -> start3:int option -> finish3:int option -> 'T [,,]
    /// Sets a slice of an array
    /// target: The target array.
    /// start1: The start index of the first dimension.
    /// finish1: The end index of the first dimension.
    /// start2: The start index of the second dimension.
    /// finish2: The end index of the second dimension.
    /// start3: The start index of the third dimension.
    /// finish3: The end index of the third dimension.
    /// source: The source array.
    val SetArraySlice3D : target:'T [,,] -> start1:int option -> finish1:int option -> start2:int option -> finish2:int option -> start3:int option -> finish3:int option -> source:'T [,,] -> unit
    /// Gets a slice of an array
    /// source: The source array.
    /// start1: The start index of the first dimension.
    /// finish1: The end index of the first dimension.
    /// start2: The start index of the second dimension.
    /// finish2: The end index of the second dimension.
    /// start3: The start index of the third dimension.
    /// finish3: The end index of the third dimension.
    /// start4: The start index of the fourth dimension.
    /// finish4: The end index of the fourth dimension.
    val GetArraySlice4D : source:'T [,,,] -> start1:int option -> finish1:int option -> start2:int option -> finish2:int option -> start3:int option -> finish3:int option -> start4:int option -> finish4:int option -> 'T [,,,]
    /// Sets a slice of an array
    /// target: The target array.
    /// start1: The start index of the first dimension.
    /// finish1: The end index of the first dimension.
    /// start2: The start index of the second dimension.
    /// finish2: The end index of the second dimension.
    /// start3: The start index of the third dimension.
    /// finish3: The end index of the third dimension.
    /// start4: The start index of the fourth dimension.
    /// finish4: The end index of the fourth dimension.
    /// source: The source array.
    val SetArraySlice4D : target:'T [,,,] -> start1:int option -> finish1:int option -> start2:int option -> finish2:int option -> start3:int option -> finish3:int option -> start4:int option -> finish4:int option -> source:'T [,,,] -> unit
    /// Gets a slice from a string
    /// source: The source string.
    /// start: The index of the first character of the slice.
    /// finish: The index of the last character of the slice.
    val inline GetStringSlice : source:string -> start:int option -> finish:int option -> string
    /// Generate a range of integers
    val RangeInt32 : start:int -> step:int -> stop:int -> seq<int>
    /// Generate a range of float values
    val RangeDouble : start:float -> step:float -> stop:float -> seq<float>
    /// Generate a range of float32 values
    val RangeSingle : start:float32 -> step:float32 -> stop:float32 -> seq<float32>
    /// Generate a range of int64 values
    val RangeInt64 : start:int64 -> step:int64 -> stop:int64 -> seq<int64>
    /// Generate a range of uint64 values
    val RangeUInt64 : start:uint64 -> step:uint64 -> stop:uint64 -> seq<uint64>
    /// Generate a range of uint32 values
    val RangeUInt32 : start:uint32 -> step:uint32 -> stop:uint32 -> seq<uint32>
    /// Generate a range of nativeint values
    val RangeIntPtr : start:nativeint -> step:nativeint -> stop:nativeint -> seq<nativeint>
    /// Generate a range of unativeint values
    val RangeUIntPtr : start:unativeint -> step:unativeint -> stop:unativeint -> seq<unativeint>
    /// Generate a range of int16 values
    val RangeInt16 : start:int16 -> step:int16 -> stop:int16 -> seq<int16>
    /// Generate a range of uint16 values
    val RangeUInt16 : start:uint16 -> step:uint16 -> stop:uint16 -> seq<uint16>
    /// Generate a range of sbyte values
    val RangeSByte : start:sbyte -> step:sbyte -> stop:sbyte -> seq<sbyte>
    /// Generate a range of byte values
    val RangeByte : start:byte -> step:byte -> stop:byte -> seq<byte>
    /// Generate a range of char values
    val RangeChar : start:char -> stop:char -> seq<char>
    /// Generate a range of values using the given zero, add, start, step and stop values
    val RangeGeneric : one:'T -> add:('T -> 'T -> 'T) -> start:'T -> stop:'T -> seq<'T>
    /// Generate a range of values using the given zero, add, start, step and stop values
    val RangeStepGeneric : zero:'Step -> add:('T -> 'Step -> 'T) -> start:'T -> step:'Step -> stop:'T -> seq<'T>
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val AbsDynamic : x:'T -> 'T
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val AcosDynamic : x:'T -> 'T
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val AsinDynamic : x:'T -> 'T
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val AtanDynamic : x:'T -> 'T
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val Atan2Dynamic : y:'T1 -> x:'T1 -> 'T2
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val CeilingDynamic : x:'T -> 'T
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val ExpDynamic : x:'T -> 'T
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val FloorDynamic : x:'T -> 'T
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val TruncateDynamic : x:'T -> 'T
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val RoundDynamic : x:'T -> 'T
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val SignDynamic : 'T -> int
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val LogDynamic : x:'T -> 'T
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val Log10Dynamic : x:'T -> 'T
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val SqrtDynamic : 'T1 -> 'T2
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val CosDynamic : x:'T -> 'T
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val CoshDynamic : x:'T -> 'T
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val SinDynamic : x:'T -> 'T
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val SinhDynamic : x:'T -> 'T
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val TanDynamic : x:'T -> 'T
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val TanhDynamic : x:'T -> 'T
    /// This is a library intrinsic. Calls to this function may be generated by evaluating quotations.
    val PowDynamic : x:'T -> y:'U -> 'T
    /// This is a library intrinsic. Calls to this function may be generated by uses of the generic 'pown' operator on values of type 'byte'
    val PowByte : x:byte -> n:int -> byte
    /// This is a library intrinsic. Calls to this function may be generated by uses of the generic 'pown' operator on values of type 'sbyte'
    val PowSByte : x:sbyte -> n:int -> sbyte
    /// This is a library intrinsic. Calls to this function may be generated by uses of the generic 'pown' operator on values of type 'int16'
    val PowInt16 : x:int16 -> n:int -> int16
    /// This is a library intrinsic. Calls to this function may be generated by uses of the generic 'pown' operator on values of type 'uint16'
    val PowUInt16 : x:uint16 -> n:int -> uint16
    /// This is a library intrinsic. Calls to this function may be generated by uses of the generic 'pown' operator on values of type 'int32'
    val PowInt32 : x:int32 -> n:int -> int32
    /// This is a library intrinsic. Calls to this function may be generated by uses of the generic 'pown' operator on values of type 'uint32'
    val PowUInt32 : x:uint32 -> n:int -> uint32
    /// This is a library intrinsic. Calls to this function may be generated by uses of the generic 'pown' operator on values of type 'int64'
    val PowInt64 : x:int64 -> n:int -> int64
    /// This is a library intrinsic. Calls to this function may be generated by uses of the generic 'pown' operator on values of type 'uint64'
    val PowUInt64 : x:uint64 -> n:int -> uint64
    /// This is a library intrinsic. Calls to this function may be generated by uses of the generic 'pown' operator on values of type 'nativeint'
    val PowIntPtr : x:nativeint -> n:int -> nativeint
    /// This is a library intrinsic. Calls to this function may be generated by uses of the generic 'pown' operator on values of type 'unativeint'
    val PowUIntPtr : x:unativeint -> n:int -> unativeint
    /// This is a library intrinsic. Calls to this function may be generated by uses of the generic 'pown' operator on values of type 'float32'
    val PowSingle : x:float32 -> n:int -> float32
    /// This is a library intrinsic. Calls to this function may be generated by uses of the generic 'pown' operator on values of type 'float'
    val PowDouble : x:float -> n:int -> float
    /// This is a library intrinsic. Calls to this function may be generated by uses of the generic 'pown' operator on values of type 'decimal'
    val PowDecimal : x:decimal -> n:int -> decimal
    /// This is a library intrinsic. Calls to this function may be generated by uses of the generic 'pown' operator
    val PowGeneric : one:'T * mul:('T -> 'T -> 'T) * value:'T * exponent:int -> 'T

/// This module contains basic operations which do not apply runtime and/or static checks
module Unchecked = 
    
    /// Unboxes a strongly typed value. This is the inverse of box, unbox<t>(box<t> a) equals a.
    /// value: The boxed value.
    val inline unbox : obj -> 'T
    /// Generate a default value for any type. This is null for reference types, For structs, this is struct value where all fields have the default value. This function is unsafe in the sense that some F# values do not have proper null values.
    val defaultof : 'T
    /// Perform generic comparison on two values where the type of the values is not statically required to have the 'comparison' constraint.
    val inline compare : 'T -> 'T -> int
    /// Perform generic equality on two values where the type of the values is not statically required to satisfy the 'equality' constraint.
    val inline equals : 'T -> 'T -> bool
    /// Perform generic hashing on a value where the type of the value is not statically required to satisfy the 'equality' constraint.
    val inline hash : 'T -> int

/// A module of comparison and equality operators that are statically resolved, but which are not fully generic and do not make structural comparison. Opening this module may make code that relies on structural or generic comparison no longer compile.
module NonStructuralComparison = 
    
    /// Compares the two values for less-than
    /// x: The first parameter.
    /// y: The second parameter.
    val inline ( < ) : x: ^T -> y: ^U -> bool when ^T : (static member ( < ) :  ^T *  ^U -> bool) and ^U : (static member ( < ) :  ^T *  ^U -> bool)
    /// Compares the two values for greater-than
    /// x: The first parameter.
    /// y: The second parameter.
    val inline ( > ) : x: ^T -> y: ^U -> bool when ^T : (static member ( > ) :  ^T *  ^U -> bool) and ^U : (static member ( > ) :  ^T *  ^U -> bool)
    /// Compares the two values for greater-than-or-equal
    /// x: The first parameter.
    /// y: The second parameter.
    val inline ( >= ) : x: ^T -> y: ^U -> bool when ^T : (static member ( >= ) :  ^T *  ^U -> bool) and ^U : (static member ( >= ) :  ^T *  ^U -> bool)
    /// Compares the two values for less-than-or-equal
    /// x: The first parameter.
    /// y: The second parameter.
    val inline ( <= ) : x: ^T -> y: ^U -> bool when ^T : (static member ( <= ) :  ^T *  ^U -> bool) and ^U : (static member ( <= ) :  ^T *  ^U -> bool)
    /// Compares the two values for equality
    /// x: The first parameter.
    /// y: The second parameter.
    val inline ( = ) : x: ^T -> y: ^T -> bool when ^T : (static member ( = ) :  ^T *  ^T -> bool)
    /// Compares the two values for inequality
    /// x: The first parameter.
    /// y: The second parameter.
    val inline ( <> ) : x: ^T -> y: ^T -> bool when ^T : (static member ( <> ) :  ^T *  ^T -> bool)
    /// Compares the two values
    /// e1: The first value.
    /// e2: The second value.
    val inline compare : e1: ^T -> e2: ^T -> int when ^T : (static member ( < ) :  ^T *  ^T -> bool) and ^T : (static member ( > ) :  ^T *  ^T -> bool)
    /// Maximum of the two values
    /// e1: The first value.
    /// e2: The second value.
    val inline max : e1: ^T -> e2: ^T ->  ^T when ^T : (static member ( < ) :  ^T *  ^T -> bool)
    /// Minimum of the two values
    /// e1: The first value.
    /// e2: The second value.
    val inline min : e1: ^T -> e2: ^T ->  ^T when ^T : (static member ( < ) :  ^T *  ^T -> bool)
    /// Calls GetHashCode() on the value
    /// e1: The value.
    val inline hash : value:'T -> int when 'T : equality

/// This module contains the basic arithmetic operations with overflow checks.
module Checked = 
    
    /// Overloaded unary negation (checks for overflow)
    /// value: The input value.
    val inline ( ~- ) : value: ^T ->  ^T when ^T : (static member ( ~- ) : unit ->  ^T)
    /// Overloaded subtraction operator (checks for overflow)
    /// x: The first value.
    /// y: The second value.
    val inline ( - ) : x: ^T1 -> y: ^T2 ->  ^T3 when ^T1 : (static member ( - ) :  ^T1 *  ^T2 ->  ^T3) and ^T2 : (static member ( - ) :  ^T1 *  ^T2 ->  ^T3)
    /// Overloaded addition operator (checks for overflow)
    /// x: The first value.
    /// y: The second value.
    val inline ( + ) : x: ^T1 -> y: ^T2 ->  ^T3 when ^T1 : (static member ( + ) :  ^T1 *  ^T2 ->  ^T3) and ^T2 : (static member ( + ) :  ^T1 *  ^T2 ->  ^T3)
    /// Overloaded multiplication operator (checks for overflow)
    /// x: The first value.
    /// y: The second value.
    val inline ( * ) : x: ^T1 -> y: ^T2 ->  ^T3 when ^T1 : (static member ( * ) :  ^T1 *  ^T2 ->  ^T3) and ^T2 : (static member ( * ) :  ^T1 *  ^T2 ->  ^T3)
    /// Converts the argument to byte. This is a direct, checked conversion for all primitive numeric types. For strings, the input is converted using System.Byte.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
    /// value: The input value.
    val inline byte : value: ^T -> byte when ^T : (static member op_Explicit : unit -> byte)
    /// Converts the argument to sbyte. This is a direct, checked conversion for all primitive numeric types. For strings, the input is converted using System.SByte.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
    /// value: The input value.
    val inline sbyte : value: ^T -> sbyte when ^T : (static member op_Explicit : unit -> sbyte)
    /// Converts the argument to int16. This is a direct, checked conversion for all primitive numeric types. For strings, the input is converted using System.Int16.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
    /// value: The input value.
    val inline int16 : value: ^T -> int16 when ^T : (static member op_Explicit : unit -> int16)
    /// Converts the argument to uint16. This is a direct, checked conversion for all primitive numeric types. For strings, the input is converted using System.UInt16.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
    /// value: The input value.
    val inline uint16 : value: ^T -> uint16 when ^T : (static member op_Explicit : unit -> uint16)
    /// Converts the argument to int. This is a direct, checked conversion for all primitive numeric types. For strings, the input is converted using System.Int32.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
    /// value: The input value.
    val inline int : value: ^T -> int when ^T : (static member op_Explicit : unit -> int)
    /// Converts the argument to int32. This is a direct, checked conversion for all primitive numeric types. For strings, the input is converted using System.Int32.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
    /// value: The input value.
    val inline int32 : value: ^T -> int32 when ^T : (static member op_Explicit : unit -> int32)
    /// Converts the argument to uint32. This is a direct, checked conversion for all primitive numeric types. For strings, the input is converted using System.UInt32.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
    /// value: The input value.
    val inline uint32 : value: ^T -> uint32 when ^T : (static member op_Explicit : unit -> uint32)
    /// Converts the argument to int64. This is a direct, checked conversion for all primitive numeric types. For strings, the input is converted using System.Int64.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
    /// value: The input value.
    val inline int64 : value: ^T -> int64 when ^T : (static member op_Explicit : unit -> int64)
    /// Converts the argument to uint64. This is a direct, checked conversion for all primitive numeric types. For strings, the input is converted using System.UInt64.Parse() with InvariantCulture settings. Otherwise the operation requires an appropriate static conversion method on the input type.
    /// value: The input value.
    val inline uint64 : value: ^T -> uint64 when ^T : (static member op_Explicit : unit -> uint64)
    /// Converts the argument to nativeint. This is a direct, checked conversion for all primitive numeric types. Otherwise the operation requires an appropriate static conversion method on the input type.
    /// value: The input value.
    val inline nativeint : value: ^T -> nativeint when ^T : (static member op_Explicit : unit -> nativeint)
    /// Converts the argument to unativeint. This is a direct, checked conversion for all primitive numeric types. Otherwise the operation requires an appropriate static conversion method on the input type.
    /// value: The input value.
    val inline unativeint : value: ^T -> unativeint when ^T : (static member op_Explicit : unit -> unativeint)
    /// Converts the argument to char. Numeric inputs are converted using a checked conversion according to the UTF-16 encoding for characters. String inputs must be exactly one character long. For other input types the operation requires an appropriate static conversion method on the input type.
    /// value: The input value.
    val inline char : value: ^T -> char when ^T : (static member op_Explicit : unit -> char)

