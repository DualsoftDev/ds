[<AutoOpen>]
module Engine.CodeGenCPU.ConvertPort

open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS
open System.Linq
  
/// 삭제 대기중 test ahn   SP-> ST   RP -> RT  EP -> ET 로 대체중



////Port 처리 Set 공용 함수
//let private getPortSetBits(v:VertexManager) (rse:SREType) =
//    assert(v :? VertexMReal)
//    let shareds = v.GetSharedReal().Select(getVM)

//    match rse with  //test link real reset 구현 대기
//    |Start -> (shareds.STs() @ [v.ST]).ToOr() //real 자신을 외부 시스템에서 Plan SET Send 경우
//    |Reset -> (shareds.RTs() @ [v.RT]).ToOr() //real 자신을 외부 시스템에서 Plan RST Send 경우 
//    |End   -> (shareds.ETs() @ [v.ET]).ToOr()

//type VertexManager with

//    member v.P1_RealStartPort(): CommentedStatement =
//        let v = v :?> VertexMReal
//        let sets = getPortSetBits v SREType.Start
//        let rsts = v.Vertex._off.Expr

//        (sets, rsts) --| (v.SP, getFuncName())

//    member v.P2_RealResetPort(): CommentedStatement =
//        let v = v :?> VertexMReal
//        let sets = getPortSetBits v SREType.Reset
//        let rsts = v.Vertex._off.Expr

//        (sets, rsts) --| (v.RP, getFuncName())

//    member v.P3_RealEndPort(): CommentedStatement =
//        let v = v :?> VertexMReal
//        let sets = getPortSetBits v SREType.End
//        let rsts = v.Vertex._off.Expr

//        (sets, rsts) --| (v.EP, getFuncName())
