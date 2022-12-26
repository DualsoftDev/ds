namespace Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Engine.Common.FS
open System.Runtime.CompilerServices

[<AutoOpen>]
module CpuLoader =

    let private convertSystem(sys:DsSystem) =
        [
            for f in sys.Flows do
            for r in f.Graph.Vertices.OfType<Real>() do
                yield! createRungsForReal(r, r.Graph)
                yield! createRungsForRoot(r, f.Graph)
        ]
    
    [<Extension>]
    type Cpu =

        [<Extension>]
        static member LoadStatements         (system:DsSystem) = convertSystem(system)
        static member LoadStatementsForText  (system:DsSystem) = 
            let statements = 
                [   
                    for (desc_, CommentAndStatement(comment_, statement)) in convertSystem(system) ->
                        statement.ToText()
                ]
            statements.JoinWith("\r\n")
