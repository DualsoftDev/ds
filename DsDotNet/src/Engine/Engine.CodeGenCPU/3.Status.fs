[<AutoOpen>]
module Engine.CodeGenCPU.ConvertStatus

open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type VertexManager with

    /// vertex 의 Real RGFH status 를 update 하는 rungs/statements 만들기
    member v.S1_RGFH(): CommentedStatement list =
        //let endTag = if v :? VertexMReal
        //             then v.ET.Expr
        //             else (v :?> VertexMCoin).ET.Expr                              //  Status   ST  RT  CR
        //                                                                           //----------------------
        let r = v.R  <== (( (!!) v.ST.Expr                       <&&> (!!) v.ET.Expr) //    R      x   -   x
                          <||> ( v.ST.Expr <&&>       v.RT.Expr  <&&> (!!) v.ET.Expr))//           o   o   x
        let g = v.G <==        ( v.ST.Expr <&&>  (!!) v.RT.Expr  <&&> (!!) v.ET.Expr) //    G      o   x   x
        let f = v.F <==        (                 (!!) v.RT.Expr  <&&>      v.ET.Expr) //    F      -   x   o
        let h = v.H <==        (                      v.RT.Expr  <&&>      v.ET.Expr) //    H      -   o   o

        [
           withExpressionComment $"{getFuncName()}(Ready)"        r
           withExpressionComment $"{getFuncName()}(Going)"        g
           withExpressionComment $"{getFuncName()}(Finish)"       f
           withExpressionComment $"{getFuncName()}(Homming)"      h
        ]
