[<AutoOpen>]
module Engine.CodeGenCPU.ConvertPort

open Engine.CodeGenCPU
open Engine.Core
open Engine.Common.FS
open System.Linq

//Port 처리 Set 공용 함수
let private getPortSetBits(v:VertexManager) (rse:SREType) =
    let real = v.Vertex :?> Real
    let planSets = v.System.GetPSs(real).ToOrElseOff(v.System)
    let shareds = v.GetSharedReal().Select(getVM)
    match rse with
    |Start -> (shareds.STs() @ [v.ST;v.SF]).ToOr() <||> planSets//real 자신을 외부 시스템에서 Plan SET Send 경우
    |Reset -> (shareds.RTs() @ [v.RT;v.RF]).ToOr() //real 자신을 외부 시스템에서 Plan RST Send 경우  //test link real reset 구현 대기
    |End   -> (shareds.ETs() @ [v.ET;v.EF]).ToOr()
type VertexManager with

    member v.P1_RealStartPort(): CommentedStatement =
        let v = v :?> VertexMReal
        let sets = getPortSetBits v SREType.Start
        let rsts = v.Vertex._off.Expr

        (sets, rsts) --| (v.SP, getFuncName())

    member v.P2_RealResetPort(): CommentedStatement =
        let v = v :?> VertexMReal
        let sets = getPortSetBits v SREType.Reset
        let rsts = v.Vertex._off.Expr

        (sets, rsts) --| (v.RP, getFuncName())

    member v.P3_RealEndPort(): CommentedStatement =
        let v = v :?> VertexMReal
        let sets = getPortSetBits v SREType.End
        let rsts = v.Vertex._off.Expr

        (sets, rsts) --| (v.EP, getFuncName())
