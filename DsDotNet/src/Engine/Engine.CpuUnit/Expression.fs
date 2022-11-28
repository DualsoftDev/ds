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
        | ConstValue of IData
        | Variable   of Tag<'T>
        | Function   of name:string * args:Arguments
        member x.ToText()   = (x:> IExpression).ToText()

        interface IExpression  with
            member x.ToText() = expressionToText(x)
            member x.Evaluate()  = 
                match x with
                | ConstValue v -> v
                | Variable t -> t.Data 
                | Function (name, args) -> excuteFunc (name, args) 
        

let createDataExpr (d:obj)      = Expression.ConstValue (ToData(d))
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
                                            ToData(f).ToString() 
                                    )
                    let text = String.Join("; ", argsText)
                    $"{name}[{text}]"

let ToText(x:Expression<'T>) = x.ToText()


let private evalArg (x:obj) =
    //Expression DS Class 타입은 전부 GenericType 
    if(x.GetType().IsGenericType|>not) then (ToData x)  //int, bool, single, double, string 처리
    else  match x with
            | :? ITag         as tag  -> tag.Data 
            | :? IExpression  as exp  -> exp.Evaluate()
            | _ ->  
                    failwith "error"    

///[arguments] 값 평가후 value로 리턴
let evaluate      (x:obj) = evalArg(x) |> ToValue

[<Extension>] 
type ArgumentExt = 
    [<Extension>] static member EvalArg(xs:Args)    = xs |> Seq.map(fun f-> f |> evalArg)    
    [<Extension>] static member CheckN(xs:Args, n)      = 
                        if xs.Count() <> n then failwith  $"Wrong number of arguments: expect {n}"
                        xs
    [<Extension>] static member ToPairValue (xs:IData seq)    = 
                        if xs.Count() <> 2 then failwith  $"Wrong number of arguments: expect {2}"
                        xs.First() |> ToValue, xs.Last() |> ToValue

    [<Extension>] static member ToBools    (args :Args)  = args.EvalArg().Cast<Data<bool>>()  |> Seq.map(fun f->f.Data) 
    [<Extension>] static member ToInts     (args :Args)  = args.EvalArg().Cast<Data<int>>()   |> Seq.map(fun f->f.Data) 
    [<Extension>] static member ToStrings  (args :Args)  = args.EvalArg().Cast<Data<string>>()|> Seq.map(fun f->f.Data) 
    [<Extension>] static member ToSingles  (args :Args)  = args.EvalArg().Cast<Data<single>>()|> Seq.map(fun f->f.Data) 
    [<Extension>] static member ToDoubles  (args :Args)  = args.EvalArg().Cast<Data<double>>()|> Seq.map(fun f->f.Data) 
    
    [<Extension>] static member ConvertBools    (args :Args)  = args.EvalArg().Select(fun f-> Convert.ToBoolean(ToValue(f)|>box))
    [<Extension>] static member ConvertInts     (args :Args)  = args.EvalArg().Select(fun f-> Convert.ToInt32  (ToValue(f)|>box))
    [<Extension>] static member ConvertStrings  (args :Args)  = args.EvalArg().Select(fun f-> Convert.ToString (ToValue(f)|>box)) 
    [<Extension>] static member ConvertSingles  (args :Args)  = args.EvalArg().Select(fun f-> Convert.ToSingle (ToValue(f)|>box)) 
    [<Extension>] static member ConvertDoubles  (args :Args)  = args.EvalArg().Select(fun f-> Convert.ToDouble (ToValue(f)|>box)) 
                
    [<Extension>] static member ToDoublePair(args :Args) = args.EvalArg().ToPairValue()|> fun (left:obj, right:obj) -> Convert.ToDouble(left), Convert.ToDouble(right) 
    [<Extension>] static member ToStringPair(args :Args) = args.EvalArg().ToPairValue()|> fun (left:obj, right:obj) -> left.ToString(), right.ToString()


let excuteFunc(text:string, args:Arguments) = 
    let result = 
        match text with
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
        |  _ -> failwith $"error : can't find function name {text}"
        
    result |> ToData  
 