namespace Engine.Common.FS
open Microsoft.FSharp.Reflection
open Microsoft.FSharp.Quotations.Patterns

/// F# Discriminated Unions
[<RequireQualifiedAccess>]
module DU =
    // http://www.fssnip.net/9l/title/toString-and-fromString-for-discriminated-unions
    let toString (x:'a) = 
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, _ -> case.Name

    let fromString<'a> (s:string) =
        match FSharpType.GetUnionCases typeof<'a> |> Array.filter (fun case -> case.Name = s) with
        |[|case|] -> Some(FSharpValue.MakeUnion(case,[||]) :?> 'a)
        |_ -> None



    //https://stackoverflow.com/questions/3363184/f-how-to-elegantly-select-and-group-discriminated-unions/3365084#3365084
    /// UnionCase 판정
    /// e.g isUnionCase<@ OnOffAction @> action => action  이 OnOffAction 인지 판정 
    /// e.g isUnionCase<@ OnOffAction, PLCAction @> action => action  이 OnOffAction 이거나 PLCAction 인지 판정 
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
            None | Single | All

        [<RequireQualifiedAccess>]
        type Requirements2 =
            None | Single | All

        let a = Requirements.None
        let b = Requirements2.None
        ()
