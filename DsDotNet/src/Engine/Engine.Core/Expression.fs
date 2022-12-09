namespace rec Engine.Core
open System.Diagnostics
open Engine.Common.FS.Prelude

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
        FunctionBody: Arguments -> 'T
        Name        : string
        Arguments   : Arguments
    }


    type Expression<'T> =
        | Terminal of Terminal<'T>
        | Function of FunctionSpec<'T>  //FunctionBody:(Arguments -> 'T) * Name * Arguments
        interface IExpression with
            member x.DataType = x.DataType
            member x.BoxedEvaluatedValue = x.Evaluate() |> box
            member x.GetBoxedRawObject() = x.GetBoxedRawObject()
            member x.ToText(withParenthesys) = x.ToText(withParenthesys)

        member x.DataType = typedefof<'T>

    /// literal 'T 로부터 Expression<'T> 생성
    let literal (x:'T) =
        let t = x.GetType()
        if t.IsValueType || t = typedefof<string> then
            Terminal (Literal x)
        else
            failwith "ERROR: Value Type Error.  only allowed for primitive type"

    /// Tag<'T> 로부터 Expression<'T> 생성
    let tag (t: Tag<'T>) = Terminal (Tag t)

    /// Variable<'T> 로부터 Expression<'T> 생성
    let var (t: StorageVariable<'T>) = Terminal (Variable t)

    type Statement =
        | Assign of expression:IExpression * target:IStorage
        | VarDecl of expression:IExpression * variable:IStorage


    type Statement with
        member x.Do() =
            match x with
            | Assign (expr, target) ->
                assert(target.DataType = expr.DataType)
                target.Value <- expr.BoxedEvaluatedValue

            | VarDecl (expr, target) ->
                assert(target.DataType = expr.DataType)
                target.Value <- expr.BoxedEvaluatedValue

        member x.ToText() =
            match x with
            | Assign (expr, target) -> $"{target.ToText()} := {expr.ToText(false)}"
            | VarDecl (expr, var) -> $"{var.DataType.Name} {var.Name} = {expr.ToText(false)}"

    type Terminal<'T> with
        member x.GetBoxedRawObject(): obj =
            match x with
            | Tag t -> t |> box
            | Variable v -> v
            | Literal v -> v |> box

        member x.Evaluate(): 'T =
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
        member x.GetBoxedRawObject() =  // return type:obj    return type 명시할 경우, 다음 compile error 발생:  error FS1198: 제네릭 멤버 'ToText'이(가) 이 프로그램 지점 전의 비균일 인스턴스화에 사용되었습니다. 이 멤버가 처음에 오도록 멤버들을 다시 정렬해 보세요. 또는, 인수 형식, 반환 형식 및 추가 제네릭 매개 변수와 제약 조건을 포함한 멤버의 전체 형식을 명시적으로 지정하세요.
            match x with
            | Terminal b -> b.GetBoxedRawObject()
            | Function fs -> fs |> box

        member x.Evaluate(): 'T =
            match x with
            | Terminal b -> b.Evaluate()
            | Function fs -> fs.FunctionBody fs.Arguments

        member x.ToText(withParenthesys:bool) =
            match x with
            | Terminal b -> b.ToText()
            | Function fs ->
                let text = fwdSerializeFunctionNameAndBoxedArguments fs.Name fs.Arguments withParenthesys
                text


