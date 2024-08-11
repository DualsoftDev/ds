namespace Engine.Core

open System.Linq

[<AutoOpen>]
module rec ExpressionForwardDeclModule =

    type IValue<'T> =
        inherit IValue
        abstract Value: 'T with get, set

    type IStorage<'T> =
        inherit IStorage
        inherit IValue<'T>

    // Interface for tags
    type ITag =
        inherit IStorage
        abstract AliasNames: ResizeArray<string>

    type ITag<'T> =
        inherit ITag
        inherit IStorage<'T>

    type IVariable = inherit IStorage

    type IVariable<'T> =
        inherit IVariable
        inherit IStorage<'T>

    // 직접변수일 경우 주소 출력
    let getStorageText (stg:IStorage) =
        match stg with
        | :? ITag as t ->  if t.AliasNames.Any() then t.Address else t.Name
        |_ -> stg.Name

    type ITerminal with
        member x.GetContact(): string =
            match x.Variable, x.Literal with
            | Some v, None -> getStorageText v
            | None, Some literal -> literal.ToText()
            | _ -> failwith "Invalid terminal"


    // Interface for PLC generation module
    type IFlatExpression = interface end

    /// e.g 가령 Person UDT 에서 "int age", 혹은 Lambda function 의 arg list;
    type TypeDecl = {
        Type:System.Type
        Name:string
    }

    type LambdaDecl = {
        Prototype:TypeDecl
        Arguments:TypeDecl list
        Body:IExpression }

    type LambdaApplication = {
        LambdaDecl:LambdaDecl
        Arguments:IExpression list
        Storages:Storages }

    /// e.g "int sum(int a,int b) = ..." 에서 a 에 해당하는 formal parameter name 은 "_local_sum_a" 이다.
    let getFormalParameterName (funName:string) (varName:string) = $"_local_{funName}_{varName}"

    type Arg       = IExpression
    type Arguments = IExpression list
    type Args      = Arguments

    type IFunctionSpec =
        abstract LambdaDecl: LambdaDecl option with get, set
        abstract LambdaApplication: LambdaApplication option with get, set
        abstract Duplicate: unit -> IFunctionSpec

    type FunctionSpec<'T> =
        {
            FunctionBody: Arguments -> 'T
            Name        : string
            Arguments   : Arguments
            mutable LambdaDecl  : LambdaDecl option
            mutable LambdaApplication  : LambdaApplication option
        }
        interface IFunctionSpec with
            member x.LambdaDecl with get() = x.LambdaDecl and set v = x.LambdaDecl <- v
            member x.LambdaApplication with get() = x.LambdaApplication and set v = x.LambdaApplication <- v
            member x.Duplicate() = { x with Name = x.Name } :> IFunctionSpec


    // Interface for objects that can be terminal expressions with names (excluding Literals)
    type INamedExpressionizableTerminal =
        inherit IExpressionizableTerminal
        abstract StorageName: string

    // Interface to access Expression<'T> in a boxed manner
    type IExpression =
        inherit IType
        abstract BoxedEvaluatedValue : obj
        abstract GetBoxedRawObject: unit -> obj
        abstract ToText : unit -> string
        abstract ToText : withParenthesis: bool -> string

        /// Function expression 인 경우 function name 반환.  terminal 이면 none
        ///
        /// e.g "+", "-", "*", "/", ">", ">=", "<", "<=", "==", "!=", "&&", "||", "!", "createTON", "createTOF", "createCounter", "createTimer"
        abstract FunctionName: string option

        /// Function expression 인 경우 function args 반환.  terminal 이거나 argument 없으면 empty list 반환
        abstract FunctionArguments: IExpression list

        abstract FunctionSpec:IFunctionSpec option
        //abstract LambdaBody: LambdaDecl option  with get, set
        //abstract LambdaApplication: LambdaApplication option with get, set
        abstract WithNewFunctionArguments: IExpression list -> IExpression
        abstract Terminal: ITerminal option
        abstract CollectStorages: unit -> IStorage list
        /// 실제 구현에서 PLC.CodeGen.Common.FlatExpression 을 반환하지만, FlatExpression 이 현 시점에서 visible 하지 않기 때문에 IFlatExpression 을 반환하는 것으로 처리
        abstract Flatten: unit -> IFlatExpression
        abstract IsEqual: IExpression -> bool

    type IExpression<'T when 'T: equality> =
        inherit IExpression
        abstract EvaluatedValue : 'T
