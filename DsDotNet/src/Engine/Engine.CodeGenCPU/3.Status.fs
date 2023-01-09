[<AutoOpen>]
module Engine.CodeGenCPU.ConvertStatus

open Engine.CodeGenCPU
open Engine.Core

type VertexManager with

    /// vertex 의 Real RGFH status 를 update 하는 rungs/statements 만들기                  
    member v.S1_RealRGFH(): CommentedStatement list =                                 //  Status   SP  RP  ET
                                                                                      //----------------------
        let r = v.R  <== (( (!!) v.SP.Expr                       <&&> (!!) v.EP.Expr) //    R      x   -   x  
                          <||> ( v.SP.Expr <&&>       v.RP.Expr  <&&> (!!) v.EP.Expr))//           o   o   x                                                    
        let g = v.G <==        ( v.SP.Expr <&&>  (!!) v.RP.Expr  <&&> (!!) v.EP.Expr) //    G      o   x   x                                                  
        let f = v.F <==        (                 (!!) v.RP.Expr  <&&>      v.EP.Expr) //    F      -   x   o                                                 
        let h = v.H <==        (                      v.RP.Expr  <&&>      v.EP.Expr) //    H      -   o   o                                                                               
                                                                                        
        [ r; g; f; h ]
        |> List.map(fun statement -> statement |> withExpressionComment "S1")

    /// vertex 의 Call RGFH status 를 update 하는 rungs/statements 만들기                  
    member v.S2_CoinRGFH(): CommentedStatement list =                                 //  Status   SP  RP  CR
                                                                                      //----------------------
        let r = v.R  <== (( (!!) v.SP.Expr                       <&&> (!!) v.CR.Expr) //    R      x   -   x  
                          <||> ( v.SP.Expr <&&>       v.RP.Expr  <&&> (!!) v.CR.Expr))//           o   o   x                                                    
        let g = v.G <==        ( v.SP.Expr <&&>  (!!) v.RP.Expr  <&&> (!!) v.CR.Expr) //    G      o   x   x                                                  
        let f = v.F <==        (                 (!!) v.RP.Expr  <&&>      v.CR.Expr) //    F      -   x   o                                                 
        let h = v.H <==        (                      v.RP.Expr  <&&>      v.CR.Expr) //    H      -   o   o                                                                               
                                                                                        
        [ r; g; f; h ]
        |> List.map(fun statement -> statement |> withExpressionComment "S1")