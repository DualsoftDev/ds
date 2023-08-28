namespace Engine.Core

open System.Collections.Generic


[<AutoOpen>]
module ExpressionForwardDeclModule =
    type IValue<'T> =
        inherit IValue
        abstract Value: 'T with get, set

    type IStorage<'T> =
        inherit IStorage
        inherit IValue<'T>

    /// tag 의 interface
    type ITag =
        inherit IStorage
    type ITag<'T> =
        inherit ITag
        inherit IStorage<'T>

    type IVariable = inherit IStorage
    type IVariable<'T> =
        inherit IVariable
        inherit IStorage<'T>

    (* PLC generation module 용 *)
    type IFlatExpression = interface end


    /// 이름을 갖는 terminal expression 이 될 수 있는 객체.  Tag, Variable (Literal 은 제외)
    type INamedExpressionizableTerminal =
        inherit IExpressionizableTerminal
        abstract StorageName:string


    // <kwak> IExpression<'T> vs IExpression : 강제 변환
    /// Expression<'T> 을 boxed 에서 접근하기 위한 최소의 interface
    type IExpression =
        abstract DataType : System.Type
        //abstract ExpressionType : ExpressionType
        abstract BoxedEvaluatedValue : obj
        /// Tag<'T> 나 Variable<'T> 객체 boxed 로 반환
        abstract GetBoxedRawObject: unit -> obj
        /// withParenthesys: terminal 일 경우는 무시되고, Function 일 경우에만 적용됨
        abstract ToText : withParenthesys:bool -> string
        /// Function expression 인 경우 function name 반환.  terminal 이면 none
        abstract FunctionName: string option
        /// Function expression 인 경우 function args 반환.  terminal 이거나 argument 없으면 empty list 반환
        abstract FunctionArguments: IExpression list
        /// Function arguments 목록만 치환된 새로운 expression 반환
        abstract WithNewFunctionArguments: IExpression list -> IExpression

        abstract Terminal: ITerminal option

        /// Function expression 에 사용된 IStorage 항목들을 반환
        abstract CollectStorages: unit -> IStorage list
        abstract Flatten: unit -> IFlatExpression
        abstract IsEqual: IExpression -> bool

    type IExpression<'T when 'T:equality> =
        inherit IExpression
        abstract EvaluatedValue : 'T
