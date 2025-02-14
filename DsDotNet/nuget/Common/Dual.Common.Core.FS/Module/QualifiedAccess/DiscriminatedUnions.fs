namespace Dual.Common.Core.FS
open Microsoft.FSharp.Reflection
open Microsoft.FSharp.Quotations.Patterns
open System
open Parse
open Dual.Common.Core.FS

/// F# Discriminated Unions
[<RequireQualifiedAccess>]
module DU =
    // http://www.fssnip.net/9l/title/toString-and-fromString-for-discriminated-unions
    let toString (x:'a) =
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, _ -> case.Name

    let fromString<'a> (s:string) =
        match FSharpType.GetUnionCases typeof<'a> |> Array.filter (fun case -> case.Name = s) with
        | [|case|] -> Some(FSharpValue.MakeUnion(case,[||]) :?> 'a)
        | _ -> None

    /// Memoized DU.fromString
    let fromStringMemoized<'a> =
        memoize fromString<'a>

    let tryParseEnum<'T when 'T: (new: unit -> 'T) and 'T: struct and 'T :> ValueType> (s:string) = Enum.TryParse<'T>(s) |> tryParseToOption

    let tryGetEnumValue<'T when 'T: (new: unit -> 'T) and 'T: struct and 'T :> ValueType> (value:int) =
        try
            if typeof<'T>.GetEnumValues() |> Seq.cast<int> |> Seq.contains(value) then
                Enum.ToObject(typeof<'T>, value) :?> 'T |> Some
            else
                None
        with ex ->
            None
    let getEnumValue<'T when 'T: (new: unit -> 'T) and 'T: struct and 'T :> ValueType> (value:int) = tryGetEnumValue<'T> value |> Option.get

    //https://stackoverflow.com/questions/62195995/enumerate-names-and-values-of-an-f-discriminated-union-type-like-enum-getvalues?noredirect=1&lq=1
    /// Return all values for an enumeration type
    ///
    /// e.g
    ///
    /// type Num = | One | Two | Three
    ///
    /// enumValues typeof<Num> => [One; Two; Three]
    let enumValues (t:'T)  = [
        for x in FSharpType.GetUnionCases t do
           yield FSharpValue.MakeUnion(x, [||]) :?> 'T
    ]

    /// [<Flag>] 도 포함가능한 Discriminated Union type 의 case 값들을 반환
    let getEnumValues (t:Type) : obj list =
        [
            if t.IsEnum then
                for v in Enum.GetValues(t) do
                    yield v
            else
                for x in FSharpType.GetUnionCases t do
                   yield FSharpValue.MakeUnion(x, [||])
        ]

    let enumValuesT<'T> = getEnumValues typedefof<'T>

    //https://stackoverflow.com/questions/3363184/f-how-to-elegantly-select-and-group-discriminated-unions/3365084#3365084
    /// UnionCase 판정
    ///
    /// e.g
    ///
    /// type Num = | One | Two | Three
    ///
    /// let n = One
    ///
    /// - isUnionCase<@ One @> n => n 이 One 인지 판정.  true
    ///
    /// - isUnionCase<@ Two @> : false
    ///
    /// - isUnionCase<@ One, Two @> n => n 이 One 이거나 Two 인지 판정
    let rec isUnionCase = function
        | Lambda (_, expr) | Let (_, _, expr) -> isUnionCase expr
        | NewTuple exprs ->
            let iucs = List.map isUnionCase exprs
            fun value -> List.exists ((|>) value) iucs
        | NewUnionCase (uci, _) ->
            let utr = FSharpValue.PreComputeUnionTagReader uci.DeclaringType
            box >> utr >> (=) uci.Tag
        | _ -> failwithlog "Expression is no union case."

    module private TestMe =
        type [<RequireQualifiedAccess>] Requirements =
            A | B | C

        [<RequireQualifiedAccess>]
        type Requirements2 =
            None | Single | All

        let a = Requirements.A
        let b = Requirements2.None
        let isUnionANone = isUnionCase <@ Requirements.A @> a
        let isUnionASingle = isUnionCase <@ Requirements.B @> a
        verify isUnionANone
        verify (not isUnionASingle)

        let isANoneOrSingle = isUnionCase <@ Requirements.A, Requirements.B @> a
        let isASingleOrAll = isUnionCase <@ Requirements.B, Requirements.C @> a
        verify isANoneOrSingle
        verify (not isASingleOrAll)

        (* int 을 F# flag enum type 으로 변환 *)
        type [<Flags>] Num =
            | BOOL          = 0x00000001
            | BYTE          = 0x00000002
            | WORD          = 0x00000004
            | DWORD         = 0x00000008
        let boolByte = 3 |> enum<Num>
        verify(boolByte = (Num.BOOL ||| Num.BYTE))
        verify (boolByte.ToString() = "BOOL, BYTE")


type DU() =
    static member Cases (t:'T) = DU.enumValues t
    static member Cases<'T>() = DU.enumValuesT<'T>
