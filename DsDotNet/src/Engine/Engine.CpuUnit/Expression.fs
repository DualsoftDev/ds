[<AutoOpen>]
module rec Engine.Cpu.FunctionImpl

open System
open System.Linq
open System.Runtime.CompilerServices
open System.Diagnostics
open System.Collections
open Engine.Cpu
open System.Text.Json
 
type Arguments = obj seq
type Args      = Arguments

type Expression<'T> = 
        | ConstValue of 'T
        | Variable   of Tag<'T>
        | Function   of name:string * args:Arguments
        member x.ToText()   = (x:> IExpression).ToText()
        member x.ToJson()   = (x:> IExpression).ToJson()

        interface IExpression  with
            member x.ToText() = expressionToText(x)
            member x.ToJson() = expressionToJson(x)
            member x.Evaluate()  = 
                match x with
                | ConstValue v -> v |> box
                | Variable t -> t.GetValue() |> CheckVaildValue
                | Function (name, args)-> excuteFunc(name, args) 
        

let createDataExpr (v:'T)       = Expression.ConstValue v
let createTagExpr  (t:Tag<'T>)  = Expression.Variable t

//ds totext
let expressionToText(x:Expression<'T>) = 
                match x with
                | ConstValue v  -> v.ToString()
                | Variable t    -> t.ToText()
                | Function (name, args) -> 
                    let argsText =  args.Select(fun f-> 
                                    match f with
                                    | :? ITag                 as tag  -> tag.ToText()
                                    | :? IExpression          as exp  -> exp.ToText()
                                    | _ ->  //나머지 value는 data 변환 확인 후 ToString 출력
                                            CheckVaildValue(f).ToString() 
                                    )
                    let text = String.Join("; ", argsText)
                    $"{name}[{text}]"

let ToText(x:Expression<'T>) = x.ToText()



///Expression -> ExpressionJson
let expressionToJson(x:Expression<'T>) = 
        match x with
        | ConstValue v  -> constJson(v)
        | Variable   t  -> variableJson(t)
        | Function (funcName, args) -> 
                { 
                    Case ="Function";Type = funcName    ;Terminal = terminalEmpty()
                    Items = args|> Seq.map(fun f-> 
                            match f with
                            | :? ITag           as tag  -> variableJson(tag)
                            | :? IExpression    as exp  -> exp.ToJson()
                            | _ ->  constJson(f)
                        )
                }

let private evalArg (x:obj) =
    //Expression DS Class 타입은 전부 GenericType 
    if(x.GetType().IsGenericType|>not) then x  //int, bool, single, double, string 처리
    else  match x with
            | :? ITag         as tag  -> tag.Value 
            | :? IExpression  as exp  -> exp.Evaluate()
            | _ ->  
                    failwith "error"    

///[arguments] 값 평가후 value로 리턴
let evaluate      (x:obj) = evalArg(x) 

[<Extension>] 
type ArgumentExt = 
    [<Extension>] static member EvalArg(xs:Args)    = xs |> Seq.map(fun f-> f |> evalArg)    
    [<Extension>] static member CheckN(xs:Args, n)      = 
                        if xs.Count() <> n then failwith  $"Wrong number of arguments: expect {n}"
                        xs
    [<Extension>] static member ToPairValue (xs:obj seq)    = 
                        if xs.Count() <> 2 then failwith  $"Wrong number of arguments: expect {2}"
                        xs.First() , xs.Last() 

    [<Extension>] static member ToBools    (args :Args)  = args.EvalArg().Cast<bool>()  
    [<Extension>] static member ToInts     (args :Args)  = args.EvalArg().Cast<int>()   
    [<Extension>] static member ToStrings  (args :Args)  = args.EvalArg().Cast<string>()
    [<Extension>] static member ToSingles  (args :Args)  = args.EvalArg().Cast<single>()
    [<Extension>] static member ToDoubles  (args :Args)  = args.EvalArg().Cast<double>()
    
    [<Extension>] static member ConvertBools    (args :Args)  = args.EvalArg().Select(fun f-> Convert.ToBoolean(CheckVaildValue(f)|>box))
    [<Extension>] static member ConvertInts     (args :Args)  = args.EvalArg().Select(fun f-> Convert.ToInt32  (CheckVaildValue(f)|>box))
    [<Extension>] static member ConvertStrings  (args :Args)  = args.EvalArg().Select(fun f-> Convert.ToString (CheckVaildValue(f)|>box)) 
    [<Extension>] static member ConvertSingles  (args :Args)  = args.EvalArg().Select(fun f-> Convert.ToSingle (CheckVaildValue(f)|>box)) 
    [<Extension>] static member ConvertDoubles  (args :Args)  = args.EvalArg().Select(fun f-> Convert.ToDouble (CheckVaildValue(f)|>box)) 
                
    [<Extension>] static member ToDoublePair(args :Args) = args.EvalArg().ToPairValue()|> fun (left:obj, right:obj) -> Convert.ToDouble(left), Convert.ToDouble(right) 
    [<Extension>] static member ToStringPair(args :Args) = args.EvalArg().ToPairValue()|> fun (left:obj, right:obj) -> left.ToString(), right.ToString()


let excuteFunc(functionName:string, args:Arguments) = 
    if args.Any()|>not then failwith  $"Empty Arguments of function : {functionName}"

    let result = 
        match functionName with
        | Func._add             -> args.ToInts()    |> Seq.reduce (+) |> box
        | Func._addDouble       -> args.ToDoubles() |> Seq.reduce (+) |> box
        | Func._addString       -> args.ToStrings() |> Seq.reduce (+) |> box
        | Func._sub             -> args.ToInts()    |> Seq.reduce (-) |> box
        | Func._subDouble       -> args.ToDoubles() |> Seq.reduce (-) |> box
        | Func._mul             -> args.ToInts()    |> Seq.reduce (*) |> box
        | Func._mulDouble       -> args.ToDoubles() |> Seq.reduce (*) |> box
        | Func._div             -> args.ToInts()    |> Seq.reduce (/) |> box
        | Func._divDouble       -> args.ToDoubles() |> Seq.reduce (/) |> box
        | Func._modu            -> args.ToInts()    |> Seq.reduce (%) |> box
        | Func._moduDouble      -> args.ToDoubles() |> Seq.reduce (%) |> box

        | Func._equal           -> args.ToDoublePair() |> fun (x, y) -> x = y       |> box
        | Func._equalString     -> args.ToStringPair() |> fun (x, y) -> x = y       |> box
        | Func._notEqual        -> args.ToDoublePair() |> fun (x, y) -> x <> y      |> box
        | Func._notEqualString  -> args.ToStringPair() |> fun (x, y) -> x <> y      |> box
        | Func._gt              -> args.ToDoublePair() |> fun (x, y) -> x > y       |> box
        | Func._lt              -> args.ToDoublePair() |> fun (x, y) -> x < y       |> box
        | Func._gte             -> args.ToDoublePair() |> fun (x, y) -> x >= y      |> box
        | Func._lte             -> args.ToDoublePair() |> fun (x, y) -> x <= y      |> box
        | Func._logicalAnd      -> args.ToBools()             |> Seq.reduce (&&)    |> box
        | Func._logicalOr       -> args.ToBools()             |> Seq.reduce (||)    |> box
        | Func._logicalNot      -> args.CheckN(1).ToBools()   |> Seq.head |> not    |> box

        | Func._xorBit          -> args.ToInts()              |> Seq.reduce (^^^)   |> box //비트 배타적 OR 연산자.
        | Func._orBit           -> args.ToInts()              |> Seq.reduce (|||)   |> box //비트 OR 연산자. 
        | Func._andBit          -> args.ToInts()              |> Seq.reduce (&&&)   |> box //비트 AND 연산자. 
        | Func._shiftLeft       -> args.CheckN(2).ToInts()    |> Seq.reduce (<<<)   |> box //비트 왼쪽 시프트 연산자.
        | Func._shiftRight      -> args.CheckN(2).ToInts()    |> Seq.reduce (>>>)   |> box //비트 오른쪽 시프트 연산자.
        | Func._notBit          -> args.CheckN(1).ToInts()    |> Seq.head |>(~~~)   |> box //비트 부정 연산자.

        | Func._convertBool     -> args.CheckN(1).ConvertBools()    |> Seq.head  |> box
        | Func._convertInt      -> args.CheckN(1).ConvertInts()     |> Seq.head  |> box
        | Func._convertString   -> args.CheckN(1).ConvertStrings()  |> Seq.head  |> box
        | Func._convertSingle   -> args.CheckN(1).ConvertSingles()  |> Seq.head  |> box
        | Func._convertDouble   -> args.CheckN(1).ConvertDoubles()  |> Seq.head  |> box

        | Func._abs             -> args.CheckN(1).ToInts()    |> Seq.head |> Math.Abs |> box
        | Func._absDouble       -> args.CheckN(1).ToDoubles() |> Seq.head |> Math.Abs |> box
        | Func._sin             -> args.CheckN(1).ToDoubles() |> Seq.head |> Math.Sin |> box
        | Func._cos             -> args.CheckN(1).ToDoubles() |> Seq.head |> Math.Cos |> box
        | Func._tan             -> args.CheckN(1).ToDoubles() |> Seq.head |> Math.Tan |> box
        |  _ -> failwith $"error : can't find function name {functionName}"
        
    result 
 