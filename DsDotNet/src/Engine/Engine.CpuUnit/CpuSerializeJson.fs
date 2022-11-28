[<AutoOpen>]
module Engine.Cpu.CpuSerializeJson

open System.Runtime.CompilerServices
open System.Text.Json


//json union 타입이 지원 안되서 레코드 규격 추가
//f#.Json nuget이 있지만 System.Text.Json 사용
let jsonOptions =  JsonSerializerOptions ( WriteIndented = true, IncludeFields  = false)
type TerminalJson = 
  { 
    TagType : string   //None, PcTag, PlcTag, DsTag (None:bool, int,..)
    Type    : string   //bool, int, string 
    Name    : string   //None or TagName
    Value   : string   //value
  }

type ExpressionJson = 
  { 
    Case        : string              //ConstValue  | Variable             | Function
    Type        : string              //bool, int,..| bool, int,..         | +, +D, *, <
    Terminal    : TerminalJson        //TerminalJson| TerminalJson         | None
    Items : ExpressionJson seq       // [ ExpressionJsons ]
  }

type StatementJson = 
  { 
    Expresstion : ExpressionJson
    Target      : TerminalJson
  }

let toTerminalJson(x:'T) = { 
        TagType = "None"
        Type = x.GetType().Name
        Name = "None"
        Value = x.ToString() 
    } 
let toTerminalTagJson(x:ITag) = { 
        TagType = $"{x.GetType().Name.Split('`')[0]}" 
        Type =  (x.Data |> ToValue).GetType().Name  
        Name = x.Name
        Value = x.Data.ToString() 
    } 
let terminalEmpty() = { 
        TagType = "None"
        Type   =  "None"
        Name   =  "None"
        Value   = "None"
    } 
let constJson(x:'T) =  {
        Case  = "ConstValue"
        Type  = x.GetType().Name 
        Terminal = toTerminalJson(x)
        Items = []
    }
let variableJson(tag:ITag)  = {
        Case ="Variable"  
        Type = $"{tag.GetType().Name.Split('`')[0]}" 
        Terminal = toTerminalTagJson(tag)
        Items = []
    }

let toConstExpr(x:ExpressionJson) = Expression.ConstValue(getData(x.Type, x.Terminal.Value))
let toTag(x:TerminalJson) = 
    let typeName = x.TagType
    let dataType = x.Type
    let tagName  = x.Name
    let tagValue = x.Value
    let tag =   match typeName with
                |"DsTag"    -> SegmentTag<byte>.Create(tagName , getData(dataType, tagValue)|> ToValue)  :> ITag
                |"DsDotBit" -> DsDotBit.Create(tagName ,Memory(getData(dataType, tagValue)|> ToValue))  :> ITag  //todo :Memory를 DsTag로 ref 처리필요
                |"PlcTag"   -> PlcTag.Create(tagName, getData(dataType, tagValue)|> ToValue)  :> ITag
                |"PcTag"    -> PcTag.Create(tagName, getData(dataType, tagValue) |> ToValue)  :> ITag
                |_ -> failwith "error"

    tag :?> Tag<'T> 


///Expression -> ExpressionJson
let toJson(x:Expression<'T>) = 
    let rec getJson(x:Expression<'T>) = 
        match x with
        | ConstValue v  -> constJson(v|> ToValue)
        | Variable   t  -> variableJson(t)
        | Function (funcName, args) -> 
                { 
                    Case ="Function";Type = funcName    ;Terminal = terminalEmpty()
                    Items = args|> Seq.map(fun f-> 
                            match f with
                            | :? IData          as data -> constJson(data)
                            | :? ITag           as tag  -> variableJson(tag)
                            | :? IExpression    as exp  -> getJson(exp :?> Expression<'T>)
                            | _ ->  constJson(f)
                        )
                }
    getJson(x)

///ExpressionJson -> Expression
let toExpr(x:ExpressionJson) = 
    let rec getExpr(x:ExpressionJson) = 
                    match x.Case with
                    |"ConstValue" -> toConstExpr(x)
                    |"Variable" ->   toTag(x.Terminal) |> createTagExpr
                    |"Function" ->   Function(x.Type, x.Items |> Seq.map(fun f-> getExpr f) )
                    | _ -> failwith "error"
    getExpr(x)

[<AutoOpen>]
[<Extension>]
type SerializeModule =
    ///Expression -> ToJsonText
    [<Extension>] static member ToJsonText (expr:Expression<'T>) = 
                    let expressionJson = toJson(expr)
                    let json = JsonSerializer.Serialize<ExpressionJson>(expressionJson, jsonOptions)
                    json.ToString()

    ///Statement -> ToJsonText
    [<Extension>] static member ToJsonText (x:Statement<'T>) = 
                    let statementJson = match x with
                                        | Assign     (expr, target) ->  { 
                                            Expresstion = expr    |> toJson
                                            Target      = target  |> toTerminalTagJson }

                    let json = JsonSerializer.Serialize<StatementJson>(statementJson, jsonOptions)
                    json.ToString()

    ///JsonText -> Expression
    [<Extension>] static member ToExpression (json:string) =
                    let exprJson = JsonSerializer.Deserialize<ExpressionJson> json
                    toExpr(exprJson)

     ///JsonText -> Statement
    [<Extension>] static member ToStatement (json:string) =
                    let sJson = JsonSerializer.Deserialize<StatementJson> json
                    Assign(sJson.Expresstion|>toExpr , sJson.Target |> toTag)
