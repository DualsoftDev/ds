namespace Engine.Core

open Dual.Common.Base.FS
open Dual.Common.Core.FS
open Engine.Common

[<AutoOpen>]
module rec CodeElements =

    type VariableType =
        | Mutable
        | Immutable


    type VariableData(name:string, varType:DataType, variableType:VariableType)  =
        inherit FqdnObject(name, createFqdnObject([||]))
        member x.Name = name
        member x.Type = varType
        member x.VariableType = variableType
        member x.ToDsText() =
            match variableType with
            | Mutable ->  $"{varType.ToText()} {name}"
            | Immutable -> $"const {varType.ToText()} {name} = {x.InitValue}"
        member val InitValue = getNull<string>() with get, set

    //action 주소를 가지는 변수
    type ActionVariable(name:string, address:string, targetName:string, varType:DataType)  =
        inherit FqdnObject(name, createFqdnObject([||]))
        member x.Name = name
        member x.Address = address
        member x.TargetName = targetName
        member x.Type = varType


    let updateOperator (op:OperatorFunction) (funcBodyText:string) =
        if funcBodyText <> "" then
            op.OperatorCode <- funcBodyText



    [<AbstractClass>]
    type DsFunc(name:string) =
        interface INamed with
            member x.Name with get() = x.Name and set(_v) = failwithlog "ERROR: not supported"
        member x.Name = name
        member val Statements = StatementContainer()
        member x.ToDsText() =
            match x with
            | :? OperatorFunction as op -> op.ToDsText()
            | :? CommandFunction as cmd -> cmd.ToDsText()
            | _ -> failwith "Not Supported"

    ///Comparison, Logical, ... Operators  (비교, 논리 연산자)
    and OperatorFunction(name:string) =
        inherit DsFunc(name)
        member val OperatorCode = "" with get, set

        member x.ToDsText() = if x.OperatorCode = "" then TextSkip else x.OperatorCode

    ///Copy, Assign, ... Commands (복사, 대입 명령)
    and CommandFunction(name:string) =
        inherit DsFunc(name)
        member val CommandCode = "" with get, set

        member x.ToDsText() = if x.CommandCode = "" then TextSkip else x.CommandCode
