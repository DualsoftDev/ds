[<AutoOpen>]
module Engine.CodeGenCPU.ConvertFunctions

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS
open Engine.Parser.FS


type VertexMCall with

  
    member v.C1_DoOperator() =
        let call = v.Vertex :?> Call
        let comment = getFuncName()
        let sts = call.TargetFunc.Statements
        if sts.Count = 1
        then 
            [
            match sts.Head() with
            | DuAssign (_, cmdExpr, _) ->
                yield withExpressionComment comment (DuAssign (None, cmdExpr, v.CallOperatorValue))
            |_ -> failWithLog $"err {comment}"
            ]
        elif sts.Count > 1
        then failwithlog $"Operator({call.Name})에는 하나의 수식이 필요합니다. \r\n테이블 정의 수식 Count:({sts.Count})"
        else []
         

    member v.C2_DoCommand() =
        let call = v.Vertex :?> Call
        let comment = getFuncName()
        [
            if call.TargetFunc.Statements.any() 
            then
                let sets = 
                    if RuntimeDS.Package.IsPLCorPLCSIM() 
                    then
                        fbRising [v.MM.Expr]:> IExpression<bool>
                    elif RuntimeDS.Package.IsPCorPCSIM() then
                        v.CallCommandPulse.Expr   
                    else   
                        failWithLog $"Not supported {RuntimeDS.Package} package"

                yield! (v.MM.Expr, v.CallCommandPulseRelay) --^ (v.CallCommandPulse, getFuncName()) 

                    ////test ahn
                yield! call.TargetFunc.Statements |> Seq.collect(fun s->
                        [
                            match s with
                            | DuAssign (_, cmdExpr, target) ->
                                yield withExpressionComment comment (DuAssign (sets|> Some, cmdExpr, target))
                            |_ -> failWithLog $"err {comment}"
                        ]
                    )

                yield (v.CallCommandPulse.Expr, v._off.Expr) --| (v.CallCommandEnd, getFuncName())
        ]

    member v.C3_DoOperatorDevice() =
        let call = v.Vertex :?> Call
        let inOps = 
            call.TargetJob.DeviceDefs
                .Select(fun d->
                    if d.InAddress = TextAddrEmpty || d.InAddress = TextSkip
                    then //주소가 없으면 Plan 으로 처리
                        d.PE.Expr
                    else    
                        d.GetInExpr(call.TargetJob)
                ) 

        if inOps.IsEmpty()
        then failwithlog $"Device({call.Name})에는 입력이 필요합니다."
        else
            let sets = inOps.ToAndElseOff()    
            (sets, call._off.Expr) --| (v.CallOperatorValue, getFuncName()) //그대로 복사
