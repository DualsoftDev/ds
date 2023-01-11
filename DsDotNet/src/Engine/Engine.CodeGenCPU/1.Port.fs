[<AutoOpen>]
module Engine.CodeGenCPU.ConvertPort

open Engine.CodeGenCPU
open Engine.Core
open Engine.Common.FS
open System.Linq

//Port 처리 Set 공용 함수
let private getPortSetBits(v:VertexManager) (rse:SREType) =
    match v.Vertex with
    | :? Real as r ->  
        let shareds = v.GetSharedReal()
        match rse with
        |Reset -> (shareds.RTs() @ [v.RT;v.RF]).ToOr()
        |Start -> (shareds.STs() @ [v.ST;v.SF]).ToOr() <||> v.System.GetTXs(r).EmptyOffElseToOr(v.System)
        |End   -> (shareds.ETs() @ [v.ST;v.SF]).ToOr() 

    | _ -> failwith "Error getPortSetBits : Real Only"
 

type VertexManager with
    
    member v.P1_RealStartPort(): CommentedStatement =
        let v = v :?> VertexReal
        let sets = getPortSetBits v SREType.Start 
        let rsts = v.System._off.Expr
         
        (sets, rsts) --| (v.SP, "P1")

    member v.P2_RealResetPort(): CommentedStatement =
        let v = v :?> VertexReal
        let sets = getPortSetBits v SREType.Reset 
        let rsts = v.System._off.Expr
         
        (sets, rsts) --| (v.RP, "P2")

    member v.P3_RealEndPort(): CommentedStatement =
        let v = v :?> VertexReal
        let sets = getPortSetBits v SREType.End 
        let rsts = v.System._off.Expr
         
        (sets, rsts) --| (v.EP, "P3")
