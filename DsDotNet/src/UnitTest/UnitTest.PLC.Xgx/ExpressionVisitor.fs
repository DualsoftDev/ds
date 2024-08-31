namespace T

open NUnit.Framework

open Dual.Common.UnitTest.FS
open Dual.Common.Core.FS
open Engine.Parser.FS
open Engine.Core
open PLC.CodeGen.LS
open System.Globalization

type ExpressionVisitorTest() =
    inherit XgxTestBaseClass(XGI)
    let code = generateInt16VariableDeclarations 1 8 + """
        bool cond1 = false;
        bool result = false;

        $result = $cond1 && $nn1 + $nn2 * $nn3 > 2s || $nn4 + $nn5 * $nn6 / $nn7 - $nn8 > 5s;
    """

    let getExpression (statment:Statement) =
        match statment with
        | DuAssign(_, expr, var) -> expr
        | _ -> failwith "Not supported"

    let stringEquals (str1:string) (str2:string) =
        System.String.Compare(str1, str2, CultureInfo.CurrentCulture, (*CompareOptions.IgnoreCase |||*) CompareOptions.IgnoreSymbols) = 0

    [<Test>]
    member x.``Expression Visitor: Identity transform test`` () =
        let expr =
            let storages = Storages()
            let statements = parseCodeForWindows storages code
            statements |> last |> getExpression
        let oldText = expr.ToTextFormat()
        oldText |> tracefn "%s\n" 

        // (int*IExpression -> IExpression) type 의 handler 함수에서 snd 인 IExpression 만 반환하는 handler
        let transformers = {TerminalHandler = snd; FunctionHandler = Tuple.second}
        let newExpression = expr.Transform(transformers, None)
        let newText = newExpression.ToTextFormat()
        newText |> tracefn "%s\n" 

        oldText === newText

    [<Test>]
    member x.``Expression Visitor: XGK pre-transform test`` () =
        let statements = StatementContainer()   // 새로이 생성될 statements
        let storages = XgxStorage()     // 새로이 생성될 storages
        let expr =
            let storages = Storages()
            let statements = parseCodeForWindows storages code
            statements |> last |> getExpression

        let prjParam = getXgxProjectParams XGI "UnitTestProject"


        let functionTransformer (level:int, functionExpression:IExpression, expStore:IStorage option) =
            match functionExpression.FunctionName with
            | Some("+"|"-"|"*"|"/" as op) when level <> 0 ->
                let var =
                    let args = functionExpression.FunctionArguments
                    let mnemonic = operatorToMnemonic op
                    //let initValue = typeDefaultValue functionExpression.DataType
                    let initValue = typeDefaultValue args[0].DataType
                    let comment = args |> map (fun a -> a.ToText()) |> String.concat $" {op} "
                    prjParam.CreateAutoVariable(mnemonic, initValue, comment)
                let augStatement = DuAssign(None, functionExpression, var)
                statements.Add augStatement
                storages.Add var
                var.ToExpression()
            | _ ->
                functionExpression


        let oldExprText = expr.ToTextFormat()
        tracefn "ORIGINAL:\r\n%s" oldExprText


        // Terminal Handler: (int*IExpression -> IExpression) type 의 handler 함수에서 snd 인 IExpression 만 반환하는 handler.  즉 기존 것 그대로 사용
        // Function Handler: (int*IExpression -> IExpression) type 의 handler 함수에서 +, -, *, / 연산자를 만나면 새로운 변수를 생성하여 대입하는 handler
        let transformers = {TerminalHandler = snd; FunctionHandler = functionTransformer}
        let newExpression = expr.Transform(transformers, None)
        let newExprText = newExpression.ToTextFormat()
        tracefn "NEW:\r\n%s" newExprText

        let statementsText = statements |> map (fun s -> s.ToText()) |> String.concat "\r\n"
        tracefn "STATEMENTS:\r\n%s" statementsText

        let storagesText = storages |> map (fun s -> $"{s.Name} = {s.Comment}") |> String.concat "\r\n"
        tracefn "STORAGES:\r\n%s" storagesText

        let oldExprAnswer = """Function: ||
    Function: &&
        Storage: $cond1
        Function: >
            Function: +
                Storage: $nn1
                Function: *
                    Storage: $nn2
                    Storage: $nn3
            Literal: 2s
    Function: >
        Function: -
            Function: +
                Storage: $nn4
                Function: /
                    Function: *
                        Storage: $nn5
                        Storage: $nn6
                    Storage: $nn7
            Storage: $nn8
        Literal: 5s
"""

        let newExprAnswer = """Function: ||
    Function: &&
        Storage: $cond1
        Function: >
            Storage: _t2_ADD
            Literal: 2s
    Function: >
        Storage: _t6_SUB
        Literal: 5s"""
        
        let statementsAnswer = """_t1_MUL = $nn2 * $nn3
_t2_ADD = $nn1 + $_t1_MUL
_t3_MUL = $nn5 * $nn6
_t4_DIV = $_t3_MUL / $nn7
_t5_ADD = $nn4 + $_t4_DIV
_t6_SUB = $_t5_ADD - $nn8"""

        let storagesAnswer = """_t1_MUL = $nn2 * $nn3
_t2_ADD = $nn1 + $_t1_MUL
_t3_MUL = $nn5 * $nn6
_t4_DIV = $_t3_MUL / $nn7
_t5_ADD = $nn4 + $_t4_DIV
_t6_SUB = $_t5_ADD - $nn8"""

        stringEquals oldExprText oldExprAnswer === true
        stringEquals newExprText newExprAnswer === true
        stringEquals statementsText statementsAnswer === true
        stringEquals storagesText storagesAnswer === true



