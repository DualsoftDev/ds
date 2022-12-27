[<AutoOpen>]
module Engine.CodeGenCPU.ConvertStatus

open Engine.CodeGenCPU
open Engine.Core

type VertexManager with

    /// vertex 의 RGFH status 를 update 하는 rungs/statements 만들기                   
    member v.S1_Ready_Going_Finish_Homing(): CommentedStatement list =            //  Status   SP  RP  ET
                                                                                  //----------------------
        let r = v.Ready  <== (      ( (!!) v.SP                  <&&> (!!) v.EP ) //    R      x   -   x  
                               <||> (      v.SP <&&>       v.RP  <&&> (!!) v.EP ))//           o   o   x                                                    
        let g = v.Going  <==        (      v.SP <&&>  (!!) v.RP  <&&> (!!) v.EP ) //    G      o   x   x                                                  
        let f = v.Finish <==        (                 (!!) v.RP  <&&>      v.EP ) //    F      -   x   o                                                 
        let h = v.Homing <==        (                      v.RP  <&&>      v.EP ) //    H      -   o   o                                                                               
                                                                                        
        [ r; g; f; h ]
        |> List.map(fun statement -> statement |> withExpressionComment "S1")