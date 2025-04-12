[<AutoOpen>]
module Engine.CodeGenCPU.ConvertHmiPulse

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type VertexTagManager with

    member v.H1_HmiPulse(): CommentedStatement [] =
        let fn = getFuncName()
        [|
            yield! (v.SF.Expr, v.System)  --^ (v.SFP, fn) 
            yield! (v.ON.Expr, v.System)  --^ (v.ONP, fn) 
            if v.Vertex :? Real 
            then
                let resetUsingOA = (v.Vertex.VR.OB.Expr <&&> v.Vertex.VR.OG.Expr) //원위치 상태에서 원위치 누르면 Real 리셋
                yield! (v.RF.Expr <||> resetUsingOA, v.System)  --^ (v.RFP, fn) 
            else
                yield! (v.RF.Expr , v.System)  --^ (v.RFP, fn) 
                
        |]
