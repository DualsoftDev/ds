[<AutoOpen>]
module Engine.CodeGenCPU.ConvertPort

open Engine.CodeGenCPU
open Engine.Core
open Engine.Common.FS
open System.Linq

//Port 처리 Set 공용 함수
let private getPortSetBits(v:VertexManager) (rse:SREType) =
    let real = v.Vertex :?> Real
    let shareds = v.GetSharedReal().Select(getVM)
    match rse with                                      //real 자신을 외부 시스템에서 Plan Send 경우
    |Start -> (shareds.STs() @ [v.ST;v.SF]).ToOr() <||> v.System.GetPSs(real).EmptyOffElseToOr(v.System)
    |Reset -> (shareds.RTs() @ [v.RT;v.RF]).ToOr() <||> v.System.GetPRs(real).EmptyOffElseToOr(v.System)
    |End   -> (shareds.ETs() @ [v.ET;v.EF]).ToOr() 

type VertexManager with
    
    member v.P1_RealStartPort(): CommentedStatement =
        let v = v :?> VertexMReal
        let sets = getPortSetBits v SREType.Start 
        let rsts = v.System._off.Expr
         
        (sets, rsts) --| (v.SP, "P1")

    member v.P2_RealResetPort(): CommentedStatement =
        let v = v :?> VertexMReal
        let sets = getPortSetBits v SREType.Reset 
        let rsts = v.System._off.Expr
         
        (sets, rsts) --| (v.RP, "P2")

    member v.P3_RealEndPort(): CommentedStatement =
        let v = v :?> VertexMReal
        let sets = getPortSetBits v SREType.End 
        let rsts = v.System._off.Expr
         
        (sets, rsts) --| (v.EP, "P3")
