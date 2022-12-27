[<AutoOpen>]
module Engine.CodeGenCPU.ConvertStatus

open Engine.CodeGenCPU
open Engine.Core

type VertexMemoryManager with
    //----------------------
        //  Status   SP  RP  ET
        //----------------------
        //    R      x   -   x
        //           o   o   x
        //    G      o   x   x
        //    F      -   x   o
        //    H      -   o   o
        //----------------------
    /// vertex 의 RGFH status 를 update 하는 rungs/statements 만들기
    member v.CreateRGFHRungs(): CommentedStatement list =
    
        let rungR = v.Ready   <== (      ( (!!) v.SP                  <&&> (!!) v.EP )
                                    <||> (      v.SP <&&>       v.RP  <&&> (!!) v.EP ))
        let rungG = v.Going   <==        (      v.SP <&&>  (!!) v.RP  <&&> (!!) v.EP )
        let rungF = v.Finish  <==        (                 (!!) v.RP  <&&>      v.EP )
        let rungH = v.Homing  <==        (                      v.RP  <&&>      v.EP )

        [ rungR; rungG; rungF; rungH ]
        |> List.map(fun statement -> CommentedStatement("", statement))

