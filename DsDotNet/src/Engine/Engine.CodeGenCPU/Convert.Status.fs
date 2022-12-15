[<AutoOpen>]
module Engine.CodeGenCPU.ConvertStatus

open System.Linq
open System.Runtime.CompilerServices
open Engine.CodeGenCPU
open Engine.Core

[<AutoOpen>]
[<Extension>]
type StatementStatus =

    ///vertex status bit 만들기
    [<Extension>] 
    static member CreateRGFH(vertex:VertexM) =
                 
        let exprCS = tag <| vertex.StartPort
        let exprCR = tag <| vertex.ResetPort
        let exprCE = tag <| vertex.EndTag   // End는 Port와 Tag가 같음으로
        //----------------------
        //  Status   SP  RP  ET 
        //----------------------
        //    R      x   -   x  
        //           o   o   x  
        //    G      o   x   x  
        //    F      -   x   o  
        //    H      -   o   o  
        //----------------------
        let condSR =      ( (!!) exprCS                  <&&> (!!) exprCE )  
                     <||> (      exprCS <&&>      exprCR <&&> (!!) exprCE )  
        let condSG =             exprCS <&&> (!!) exprCR <&&> (!!) exprCE    
        let condSF =                         (!!) exprCR <&&>      exprCE    
        let condSH =                              exprCR <&&>      exprCE    

        let rungR = vertex.Ready  <== condSR
        let rungG = vertex.Going  <== condSG
        let rungF = vertex.Finish <== condSF
        let rungH = vertex.Homing <== condSH

        [rungR;rungG;rungF;rungH]

