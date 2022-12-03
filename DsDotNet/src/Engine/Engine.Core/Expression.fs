namespace rec Engine.Core
open System.Diagnostics

(*  expression: generic type <'T> 나 <_> 으로는 <obj> match 으로 간주됨
    Expression<'T> 객체에 대한 matching
    * :? Expression<int> as x -> 형태로 type 을 지정하면 matching 이 가능하다.
    * :? Expression<_> as x ->   형태로 type 을 지정하지 않으면, Expression<obj> 로 matching 시도해서 matching 이 불가능하다.
    * :? Expression<'T> as x ->  형태로 type 을 지정하지 않으면, Expression<obj> 로 matching 시도해서 matching 이 불가능하다.
    * matching 해서 수행해야 할 필요한 기능들은 non generic interface 인 IExpression 에 담아 두고, 이를 matching 한다.
*)

[<AutoOpen>]
module ExpressionModule =

    type Terminal<'T> =
        | Tag of Tag<'T>
        | Variable of StorageVariable<'T>
        | Literal of 'T

    type FunctionSpec<'T> = {
        f: Arguments -> 'T
        name: string
        args:Arguments
    }


    type Expression<'T> =
        | Terminal of Terminal<'T>
        | Function of FunctionSpec<'T>  //f:(Arguments -> 'T) * name:string * args:Arguments
        interface IExpression with
            member x.DataType = x.DataType
            member x.ExpressionType = x.ExpressionType
            member x.BoxedEvaluatedValue = x.Evaluate() |> box
            member x.GetBoxedRawObject() = x.GetBoxedRawObject()
            member x.ToText(withParenthesys) = x.ToText(withParenthesys)

        member x.DataType = typedefof<'T>
        member x.ExpressionType =
            match x with
            | Terminal b -> b.ExpressionType
            | Function _ -> ExpTypeFunction

    /// literal 'T 로부터 Expression<'T> 생성
    let literal (x:'T) =
        let t = x.GetType()
        if t.IsValueType || t = typedefof<string> then
            Terminal (Literal x)
        else
            failwith "ERROR: Value Type Error.  only allowed for primitive type"

    /// Tag<'T> 로부터 Expression<'T> 생성
    let tag (t: Tag<'T>) = Terminal (Tag t)
    let var (t: StorageVariable<'T>) = Terminal (Variable t)

    [<AutoOpen>]
    module StatementModule =
        type Statement<'T> =
            | Assign of expr:Expression<'T> * target:Tag<'T>
            member x.Do() =
                match x with
                | Assign (expr, target) -> target.Value <- expr.Evaluate()
            member x.ToText() =
                 match x with
                 | Assign     (expr, target) -> $"{target.ToText()} := {expr.ToText(false)}"

    type Terminal<'T> with
        member x.ExpressionType =
            match x with
            | Tag _ -> ExpTypeTag
            | Variable _ -> ExpTypeVariable
            | Literal _ -> ExpTypeLiteral

        member x.GetBoxedRawObject() =
            match x with
            | Tag t -> t |> box
            | Variable v -> v
            | Literal v -> v |> box

        member x.Evaluate() =
            match x with
            | Tag t -> t.Value
            | Variable v -> v.Value
            | Literal v -> v

        member x.ToText() =
            match x with
            | Tag t -> "%" + t.Name
            | Variable t -> "$" + t.Name
            | Literal v -> sprintf "%A" v

    type Expression<'T> with
        member x.GetBoxedRawObject() =
            match x with
            | Terminal b -> b.GetBoxedRawObject()
            | Function fs -> fs |> box

        member x.Evaluate() =
            match x with
            | Terminal b -> b.Evaluate()
            | Function fs -> fs.f fs.args

        member x.ToText(withParenthesys:bool) =
            match x with
            | Terminal b -> b.ToText()
            | Function fs ->
                let text = fwdSerializeFunctionNameAndBoxedArguments fs.name fs.args withParenthesys
                text


