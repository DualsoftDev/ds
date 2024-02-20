[<AutoOpen>]
module Engine.CodeGenCPU.ConvertReal

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type VertexMReal with

    member v.R1_RealInitialStart(): CommentedStatement  =
        let set = v.G.Expr <&&> v.OG.Expr
        let rst = v.RT.Expr

        (set, rst) ==| (v.RR, getFuncName())

    member v.R2_RealJobComplete(): CommentedStatement seq=
        let real = v.Vertex :?> Real
        let setCoins = real.CoinRelays.ToAndElseOn()
        [   
            (v.G.Expr, v._off.Expr) --| (v.GG, getFuncName())  //finish 전에 GR 한번 연결 
            (v.GG.Expr <&&> setCoins <||> v.ON.Expr , v.H.Expr <||> v.OFF.Expr) ==| (v.ET, getFuncName())
        ]

    member v.R3_RealStartPoint(): CommentedStatement  =
        let set = (v.G.Expr <&&> !!v.RR.Expr<&&> v.SYNC.Expr)
        let rst = v._off.Expr

        (set, rst) --| (v.RO, getFuncName())   


    member v.R4_RealSync(): CommentedStatement  =
        let real = v.Vertex :?> Real
        let set = real.Graph.Vertices.OfType<Call>()
                      .SelectMany(fun call -> call.TargetJob.ApiDefs)
                      .Select(fun api-> api.AL).ToAndElseOn()

        let rst = v._off.Expr
        (set, rst) --| (v.SYNC, getFuncName())
      
        //test ahn 인과 시작조건으로 변경
    member v.R5_RealDataMove(): CommentedStatement  =
        let set = v.RD.ToExpression() 
        (set) --* (v.RD, getFuncName())

type VertexManager with
    member v.R1_RealInitialStart(): CommentedStatement  = (v :?> VertexMReal).R1_RealInitialStart()
    member v.R2_RealJobComplete() : CommentedStatement seq = (v :?> VertexMReal).R2_RealJobComplete()
    member v.R3_RealStartPoint()  : CommentedStatement  = (v :?> VertexMReal).R3_RealStartPoint()
    member v.R4_RealSync()  : CommentedStatement  = (v :?> VertexMReal).R4_RealSync()
    member v.R5_RealDataMove()  : CommentedStatement  = (v :?> VertexMReal).R5_RealDataMove()
