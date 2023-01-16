[<AutoOpen>]
module Engine.CodeGenCPU.ConvertPort

open Engine.CodeGenCPU
open Engine.Core
open Engine.Common.FS
open System.Linq

//Port 처리 Set 공용 함수
let private getPortSetExpression(v:VertexManager) (rse:SREType) : Expression<bool> =
    let real = v.Vertex :?> Real
    let shareds = v.GetSharedReal().Select(getVM)
    (* S/R/E 조건에 맞는 expression 추출:  공유된 real 에서의 명령 + HMI 에서의 명령 + 자신의 명령 *)
    match rse with                                      //real 자신을 외부 시스템에서 Plan Send 경우
    | Start -> (shareds.STs() @ [v.ST; v.SF]).ToOr() <||> v.System.GetPSs(real).ToOrElseOff(v.System)
    | Reset -> (shareds.RTs() @ [v.RT; v.RF]).ToOr()
    | End   -> (shareds.ETs() @ [v.ET; v.EF]).ToOr()

type VertexManager with

    member v.P1_RealStartPort(): CommentedStatement =
        let v = v :?> VertexMReal
        let set = getPortSetExpression v SREType.Start
        let rst = v.System._off.Expr

        (set, rst) --| (v.SP, "P1")

    member v.P2_RealResetPort(): CommentedStatement =
        let v = v :?> VertexMReal
        let set = getPortSetExpression v SREType.Reset
        let rst = v.System._off.Expr

        (set, rst) --| (v.RP, "P2")

    member v.P3_RealEndPort(): CommentedStatement =
        let v = v :?> VertexMReal
        let set = getPortSetExpression v SREType.End
        let rst = v.System._off.Expr

        (set, rst) --| (v.EP, "P3")
