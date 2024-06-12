namespace PLC.CodeGen.Common
open Dual.Common.Core.FS

/// Konstants (Constants) collection

module K =
    /// "ActionType"
    [<Literal>]
    let ActionType = "ActionType"

    /// "FullName Rule"
    [<Literal>]
    let FullNameRule = "FullName Rule"

    /// "SelfHold"
    [<Literal>]
    let SelfHold = "SelfHold"

    /// "Address"
    [<Literal>]
    let Address = "Address"

    /// "Address Prefix"
    [<Literal>]
    let AddrPrefix = "Address Prefix"

    /// "Tag Type"
    [<Literal>]
    let TagType = "Tag Type"

    /// "Work Type"
    [<Literal>]
    let ProcessType = "Work Type"

    /// "Name"
    [<Literal>]
    let Name = "Name"

    /// "InstanceId"
    [<Literal>]
    let InstanceId = "Instance Id"

    /// "LibraryId"
    [<Literal>]
    let LibraryId = "Library Id"

    /// "Device count"
    [<Literal>]
    let DeviceCount = "Device Count"

    /// "Device Type"
    [<Literal>]
    let DeviceType = "Device Type"

    /// "Job Bound Type"
    [<Literal>]
    let BoundType = "Bound Type"

    /// "Job Memory Type"
    [<Literal>]
    let MemoryType = "Memory Type"

    /// "Delay"
    [<Literal>]
    let Delay = "Delay"

    /// "FBInstance"
    [<Literal>]
    let FBInstance = "FBInstance"

    /// "Size"
    [<Literal>]
    let Size = "Size"



    /// "_MANUAL"
    let _Manual = "_MANUAL"
    /// "_ADV"
    let _Advance = "_ADV"
    /// "_RET"
    let _Return = "_RET"
    /// "MANUAL"
    let Manual = "MANUAL"
    /// "ADV"
    let Advance = "ADV"
    /// "RET"
    let Return = "RET"
    let On = "ON"
    let Off = "OFF"
    let Up = "UP"
    let Down = "DOWN"



    /// "I"
    let I = "I"
    /// "Q"
    let Q = "Q"
    /// "M"
    let M = "M"

    let X = "X"
    let B = "B"
    let W = "W"
    let D = "D"
    let L = "L"


    let ErrNoAddressAssigned = "No address assigned."
    let ErrNameIsNullOrEmpty = "Name is null or empty."
    let ErrAddressIsNullOrEmpty = "Address is null or empty."


    /// "+"|"-"|"*"|"/"
    let arithmaticOperators = [|"+"; "-"; "*"; "/"|]
    /// "&"|"&&&"| "|"|"|||"| "^"|"^^^"|  "~"|"~~~"| ">>"|">>>"| "<<"|"<<<"
    let bitwiseOperators = [| "&";"&&&"; "|";"|||"; "^";"^^^";  "~";"~~~"; ">>";">>>"; "<<";"<<<" |]
    /// ">"|">="|"<"|"<="|"=="|"!="|"<>"
    let comparisonOperators = [|">";">=";"<";"<=";"==";"!=";"<>";|]
    /// ">"|">="|"<"|"<="|"=="|"!="|"<>"  |  "+"|"-"|"*"|"/"
    let arithmaticOrComparisionOperators = comparisonOperators @ arithmaticOperators

    let arithmaticOrBitwiseOrComparisionOperators = comparisonOperators @ arithmaticOperators @ bitwiseOperators

[<AutoOpen>]
module OperatorActivePatterns =
    /// IsArithmeticOperator: "+"|"-"|"*"|"/"
    let (|IsOpA|_|) (op:string) = if K.arithmaticOperators |> Array.contains op then Some op else None
    /// IsBitwiseOperator: "&";"&&&"; "|";"|||"; "^";"^^^";  "~";"~~~"; ">>";">>>"; "<<";"<<<"
    let (|IsOpB|_|) (op:string) = if K.bitwiseOperators    |> Array.contains op then Some op else None
    /// IsComparisonOperator: ">"|">="|"<"|"<="|"=="|"!="|"<>"
    let (|IsOpC|_|) (op:string) = if K.comparisonOperators |> Array.contains op then Some op else None
    /// IsArithmeticOrBitwiseOperator: ">"|">="|"<"|"<="|"=="|"!="|"<>"  |  "+"|"-"|"*"|"/"
    let (|IsOpAC|_|) (op:string) = if K.arithmaticOrComparisionOperators |> Array.contains op then Some op else None
    /// IsArithmeticOrBitwiseOrComparisonOperator
    let (|IsOpABC|_|) (op:string) = if K.arithmaticOrBitwiseOrComparisionOperators |> Array.contains op then Some op else None

    let isOpC op = (|IsOpC|_|) op |> Option.isSome
    let isOpA op = (|IsOpA|_|) op |> Option.isSome
    let isOpB op = (|IsOpB|_|) op |> Option.isSome
    let isOpAC op = (|IsOpAC|_|) op |> Option.isSome
