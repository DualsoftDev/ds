[<AutoOpen>]
module Engine.Cpu.CpuSerializeJson

open System.Runtime.CompilerServices
open System.Text.Json
open System
open Engine.Core


//let toTag(x:TerminalJson) =
//    let typeName = x.TagType
//    let dataType = x.Type
//    let tagName  = x.Name
//    let tagValue = x.Value
//    let tag = // : #Tag<'T> =
//        match typeName with
//        | "DsBit"    -> DsBit(tagName, false, Memory( getData(dataType, tagValue) |> Convert.ToByte), Monitor.ErrorRx) |> box //todo :Memory를 DsMemory로 ref 처리필요
//        | "DsDotBit" -> DsDotBit(tagName , false, Memory( getData(dataType, tagValue) |> Convert.ToByte)) |> box //todo :Memory를 DsMemory로 ref 처리필요
//        | "PlcTag"   -> PlcTag.Create(tagName, getData(dataType, tagValue)|> CheckVaildValue :?> 'T) |> box
//        | _ -> failwith "error"

//    match tag with
//    | :? Tag<'T> as t -> t
//    | _ -> failwith "ERROR"

//let toConstExpr(x:ExpressionJson) =
//    let t, v = x.Type, x.Terminal.Value
//    let conv x = Terminal (Value (box x))
//    match x.Type with
//    |"Boolean"-> Convert.ToBoolean(v)  |> conv
//    |"Int32"  -> Convert.ToInt32(v)    |> conv
//    |"Byte  " -> Convert.ToByte(v)     |> conv
//    |"Double" -> Convert.ToDouble(v)   |> conv
//    |"Single" -> Convert.ToSingle(v)   |> conv
//    |"String" -> Convert.ToDouble(v)   |> conv
//    |_ -> failwith "error"

/////ExpressionJson -> Expression
//let toExpr(x:ExpressionJson) =
//    let rec getExpr(x:ExpressionJson) =
//                    match x.Case with
//                    |"ConstValue" -> toConstExpr(x)
//                    |"Variable" ->   Terminal (toTag(x.Terminal))
//                    |"Function" ->   Function(x.Type, x.Items |> Seq.map(fun f-> getExpr f) )
//                    | _ -> failwith "error"
//    getExpr(x)

//[<AutoOpen>]
//[<Extension>]
//type SerializeModule =
//    ///Expression -> ToJsonText
//    [<Extension>] static member ToJsonText (expr:Expression<'T>) =
//                    let expressionJson = expr.ToJson()
//                    let json = JsonSerializer.Serialize<ExpressionJson>(expressionJson, jsonOptions)
//                    json.ToString()

//    ///Statement -> ToJsonText
//    [<Extension>] static member ToJsonText (x:Statement<'T>) =
//                    let statementJson = match x with
//                                        | Assign     (expr, target) ->  {
//                                            Expresstion = expr.ToJson()
//                                            Target      = target  |> toTerminalTagJson }

//                    let json = JsonSerializer.Serialize<StatementJson>(statementJson, jsonOptions)
//                    json.ToString()

//    ///JsonText -> Expression
//    [<Extension>] static member ToExpression (json:string) =
//                    let exprJson = JsonSerializer.Deserialize<ExpressionJson> json
//                    toExpr(exprJson)

//     ///JsonText -> Statement
//    [<Extension>] static member ToStatement (json:string) =
//                    let sJson = JsonSerializer.Deserialize<StatementJson> json
//                    Assign(sJson.Expresstion|>toExpr , sJson.Target |> toTag)
