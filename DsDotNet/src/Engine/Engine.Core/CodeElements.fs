namespace Engine.Core

open System
open System.Linq
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
open Dual.Common.Core.FS
open System.Collections.Generic

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

    type OperatorFunctionTypes =
        | DuOPUnDefined
        | DuOPCode
        | DuOPNot
        | DuOPTimer
        member x.ToText() =
            match x  with
            | DuOPUnDefined|DuOPCode -> ""
            | DuOPNot -> "$not"
            | DuOPTimer -> "$time"
     


    let updateOperator (op:OperatorFunction) (funcBodyText:string) = 
        if funcBodyText <> "" then 
            op.OperatorType <- DuOPCode 
            op.OperatorCode <- funcBodyText

    type CommandFunctionTypes =
        | DuCMDUnDefined
        | DuCMDCode


    [<AbstractClass>]
    type Func(name:string) =
        member x.Name = name
        member val Statements = StatementContainer()
        member x.ToDsText() =
            match x with
            | :? OperatorFunction as op -> op.ToDsText()
            | :? CommandFunction as cmd -> cmd.ToDsText()
            | _ -> failwith "Not Supported"

    ///Comparison, Logical, ... Operators  (비교, 논리 연산자)
    and OperatorFunction(name:string) =
        inherit Func(name)
        member val OperatorType = DuOPUnDefined with get, set
        member val OperatorCode = "" with get, set

        member x.ToDsText() = if x.OperatorCode = "" then TextSkip else x.OperatorCode

    ///Copy, Assign, ... Commands (복사, 대입 명령)
    and CommandFunction(name:string) =
        inherit Func(name)
        member val CommandType = DuCMDUnDefined with get, set
        member val CommandCode = "" with get, set

        member x.ToDsText() = if x.CommandCode = "" then TextSkip else x.CommandCode
    
    type DevAddress = string
    type DevParam = {
        DevAddress: DevAddress
        DevName : string option  //In or Out Tag 이름
        DevValueNType: (obj*DataType) option
        DevTime: int option  //기본 ms 단위 parsing을 위해 끝에 ms 필수
    } with
        member x.DevValue = match x.DevValueNType with 
                                 |Some (v, _)->  v
                                 |None -> null
        member x.DevType = match x.DevValueNType with 
                                 |Some (_, t)-> t
                                 |None -> DuBOOL     //기본 타입 bool   
        member x.DevSymbolName = match x.DevName with 
                                 |Some (n)-> n
                                 |None -> "" 
       
        

    let defaultDevParam (address) = 
        {   
            DevAddress = address
            DevName = None
            DevValueNType = None
            DevTime = None
        }

    let changeDevParam (x:DevParam) (address:string) (symbol:string option) = 
        { 
          DevAddress = address
          DevName =  symbol
          DevValueNType = x.DevValueNType 
          DevTime = x.DevTime
    }
    
    let addOrUpdateParam(jobName:string, paramDic:Dictionary<string, DevParam>, newParam :DevParam) = 
            paramDic.Remove jobName |> ignore
            paramDic.Add (jobName, newParam)
            
        
    let changeParam(jobName:string, paramDic:Dictionary<string, DevParam>, address:string, symbol:string option) = 
            let changedDevParam = changeDevParam paramDic[jobName] address symbol
            paramDic.Remove(jobName) |> ignore
            paramDic.Add (jobName, changedDevParam)

    let createDevParam (address:string) (name:string option) (vNt:(obj*DataType) option) (t:int option) = 
        { 
          DevAddress = address
          DevName = name
          DevValueNType =vNt
          DevTime =t
    }

    let addressPrint (addr:string) = if addr.IsNullOrEmpty() then TextAddrEmpty else addr

    let toTextDevParam (x:DevParam) = 
        match x.DevAddress, x.DevName , x.DevValueNType, x.DevTime  with 
        | address, Some(n) , Some(v,ty), Some(t) -> $"{addressPrint address}:{n}:{ty.ToStringValue(v)}:{t}ms"
        | address, Some(n) , Some(v,ty), None    -> $"{addressPrint address}:{n}:{ty.ToStringValue(v)}"
        | address, Some(n) , None, None          -> $"{addressPrint address}:{n}"
        | address, Some(n) , None,Some(t)        -> $"{addressPrint address}:{n}:{t}ms"
        | address, None, Some(v,ty), None        -> $"{addressPrint address}:{ty.ToStringValue(v)}"
        | address, None, Some(v,ty), Some(t)     -> $"{addressPrint address}:{ty.ToStringValue(v)}:{t}ms"
        | address, None, None, Some(t)           -> $"{addressPrint address}:{t}ms"
        | address, None, None, None              -> $"{addressPrint address}"



    let toTextInOutDev (inp:DevParam) (outp:DevParam) = 
        let inText = toTextDevParam inp
        let outText = toTextDevParam outp
        $"{inText}, {outText}"