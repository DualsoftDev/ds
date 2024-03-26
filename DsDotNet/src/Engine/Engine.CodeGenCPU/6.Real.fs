[<AutoOpen>]
module Engine.CodeGenCPU.ConvertReal

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type VertexMReal with

    member v.R1_RealInitialStart() =
        let set = v.G.Expr <&&> v.OG.Expr
        let rst = v.RT.Expr

        (set, rst) ==| (v.RR, getFuncName())

    member v.R2_RealJobComplete(): CommentedStatement seq=
        let real = v.Vertex :?> Real
        [   
            let set = 
                if v.IsFinished && (RuntimeDS.Package.IsPackageEmulation())
                then
                    (v.GG.Expr <&&> real.CoinETContacts.ToAndElseOn()) <||> v.ON.Expr <||> !!v.SYNC.Expr
                else                          
                    (v.GG.Expr <&&> real.CoinETContacts.ToAndElseOn()) <||> v.ON.Expr  

            let rst = (v.RT.Expr <||> v.OFF.Expr)
                        <&&> real.CoinAlloffExpr
            //수식 순서 중요
            // 1.ET -> 2.GG (바뀌면 full scan Step제어 안됨)

            //1. EndTag 
            (set, rst) ==| (v.ET, getFuncName())              
            //2. 다른 Real Reset Tag Relay을 위한 1Scan 소비 (Scan에서 제어방식 바뀌면 H/S 필요)
            (v.G.Expr, v._off.Expr) --| (v.GG, getFuncName()) 
        ]

    member v.R3_RealStartPoint() =
        let set = (v.G.Expr <&&> !!v.RR.Expr<&&> v.SYNC.Expr)
        let rst = v._off.Expr

        (set, rst) --| (v.RO, getFuncName())   


    member v.R4_RealSync() =
        let real = v.Vertex :?> Real
        let set = real.Graph.Vertices.OfType<Call>()
                      .SelectMany(fun call -> call.TargetJob.ApiDefs)
                      .Select(fun api-> api.SL2).ToAndElseOn()

        let rst = v._off.Expr
        (set, rst) --| (v.SYNC, getFuncName())
      
    member v.R5_DummyDAGCoils() =
        let real = v.Vertex :?> Real
        let rst = v._off.Expr
        [
            (real.CoinSTContacts.ToOrElseOff(), rst) --| (v.CoinAnyOnST, getFuncName())
            (real.CoinRTContacts.ToOrElseOff(), rst) --| (v.CoinAnyOnRT, getFuncName())
            (real.CoinETContacts.ToOrElseOff(), rst) --| (v.CoinAnyOnET, getFuncName())
        ]

    member v.R6_RealDataMove() =
        let set = v.RD.ToExpression() 
        (set) --* (v.RD, getFuncName())



type VertexManager with
    member v.R1_RealInitialStart() = (v :?> VertexMReal).R1_RealInitialStart()
    member v.R2_RealJobComplete() : CommentedStatement seq = (v :?> VertexMReal).R2_RealJobComplete()
    member v.R3_RealStartPoint()  = (v :?> VertexMReal).R3_RealStartPoint()
    member v.R4_RealSync()  = (v :?> VertexMReal).R4_RealSync()
    member v.R5_DummyDAGCoils()  = (v :?> VertexMReal).R5_DummyDAGCoils()
    member v.R6_RealDataMove()  = (v :?> VertexMReal).R6_RealDataMove()
