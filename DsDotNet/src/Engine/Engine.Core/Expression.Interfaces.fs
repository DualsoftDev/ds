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

    // Interface for tags
    type ITag =
        inherit IStorage

    type ITag<'T> =
        inherit ITag
        inherit IStorage<'T>

    type IVariable = inherit IStorage

    type IVariable<'T> =
        inherit IVariable
        inherit IStorage<'T>

    // Interface for PLC generation module
    type IFlatExpression = interface end

    // Interface for objects that can be terminal expressions with names (excluding Literals)
    type INamedExpressionizableTerminal =
        inherit IExpressionizableTerminal
        abstract StorageName: string

    // Interface to access Expression<'T> in a boxed manner
    type IExpression =
        abstract DataType : System.Type
        abstract BoxedEvaluatedValue : obj
        abstract GetBoxedRawObject: unit -> obj
        abstract ToText : withParenthesys: bool -> string
        /// Function expression 인 경우 function name 반환.  terminal 이면 none
        abstract FunctionName: string option
        /// Function expression 인 경우 function args 반환.  terminal 이거나 argument 없으면 empty list 반환
        abstract FunctionArguments: IExpression list
        abstract WithNewFunctionArguments: IExpression list -> IExpression
        abstract Terminal: ITerminal option
        abstract CollectStorages: unit -> IStorage list
        abstract Flatten: unit -> IFlatExpression
        abstract IsEqual: IExpression -> bool

    type IExpression<'T when 'T: equality> =
        inherit IExpression
        abstract EvaluatedValue : 'T
