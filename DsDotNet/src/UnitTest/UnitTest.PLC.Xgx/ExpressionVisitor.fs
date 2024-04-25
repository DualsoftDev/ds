namespace T
open Dual.UnitTest.Common.FS


open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open System
open PLC.CodeGen.LS

type ExpressionVisitorTest() =
    inherit XgxTestBaseClass(XGI)
    let code = generateInt16VariableDeclarations 1 8 + """
        bool cond1 = false;
        bool result = false;

        $result := $cond1 && $nn1 + $nn2 * $nn3 > 2s || $nn4 + $nn5 * $nn6 / $nn7 - $nn8 > 5s;
    """

    let getExpression (statment:Statement) =
        match statment with
        | DuAssign(expr, var) -> expr
        | _ -> failwith "Not supported"

    [<Test>]
    member x.``Expression Visitor: Identity transform test`` () =
        let expr =
            let storages = Storages()
            let statements = parseCodeForWindows storages code
            statements |> last |> getExpression
        let oldText = expr.ToTextFormat()
        oldText |> tracefn "%s\n" 

        // (int*IExpression -> IExpression) type 의 handler 함수에서 snd 인 IExpression 만 반환하는 handler
        let transformers = {TerminalHandler = snd; FunctionHandler = snd}
        let newExpression = expr.Transform(transformers)
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


        let funTransformer (level:int, fnExp:IExpression) =
            match fnExp.FunctionName with
            | Some("+" | "-" | "*" | "/" as op) ->
                let var =
                    let mnemonic = operatorToMnemonic op
                    let initValue = 0//fnExp.BoxedEvaluatedValue
                    let comment = "Temporary storage for XGK compatibility"
                    createTypedXgxAutoVariable prjParam mnemonic initValue comment :?> XgxVar<int32>
                    //createXgxAutoVariableT prjParam mnemonic  ($"{op} mnemonic") fnExp.FunctionArguments[0].BoxedEvaluatedValue
                let augStatement =
                    DuAssign(fnExp, var)
                statements.Add augStatement
                storages.Add var
                DuTerminal(DuVariable var) :> IExpression
            | _ ->
                fnExp


        let transformers = {TerminalHandler = snd; FunctionHandler = funTransformer}
        let newExpression = expr.Transform(transformers)
        let newText = newExpression.ToTextFormat()
        newText |> tracefn "%s\n" 


