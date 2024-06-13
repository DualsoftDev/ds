[<AutoOpen>]
module Engine.CodeGenCPU.ConvertMonitor

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type VertexManager with
    ///Status
    member v.S1_RGFH() =
            let r = v.R <==  (( (!!) v.ST.Expr                       <&&> (!!) v.ET.Expr) //    R      x   -   x
                              <||> ( v.ST.Expr <&&>       v.RT.Expr  <&&> (!!) v.ET.Expr))//           o   o   x
            let g = v.G <==        ( v.ST.Expr <&&>  (!!) v.RT.Expr  <&&> (!!) v.ET.Expr) //    G      o   x   x
            let f = v.F <==        (                 (!!) v.RT.Expr  <&&>      v.ET.Expr) //    F      -   x   o
            let h = v.H <==        (                      v.RT.Expr  <&&>      v.ET.Expr) //    H      -   o   o

            [
               withExpressionComment $"{getFuncName()}{v.Name}(Ready)"        r
               withExpressionComment $"{getFuncName()}{v.Name}(Going)"        g
               withExpressionComment $"{getFuncName()}{v.Name}(Finish)"       f
               withExpressionComment $"{getFuncName()}{v.Name}(Homming)"      h
            ]

    ///Monitor
    member v.M1_OriginMonitor() =
        let v = v :?> VertexMReal

        let ons       = getOriginIOExprs     (v, InitialType.On)

        let offs      = getOriginIOExprs     (v, InitialType.Off)

        let onExpr    = if ons.any() then ons.ToAndElseOff() else v._on.Expr
        let offExpr   = if offs.any() then offs.ToOrElseOn() else v._off.Expr


        let set =   onExpr <&&> (!!offExpr) <&&> v.Link.Expr
              

        (set, v._off.Expr) --| (v.OG, getFuncName())


    member v.M2_PauseMonitor() =
        let set = v.Flow.pause.Expr
        let rst = v._off.Expr

        (set, rst) --| (v.PA, getFuncName())