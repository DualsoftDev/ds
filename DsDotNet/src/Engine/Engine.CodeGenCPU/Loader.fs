namespace Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Engine.Common.FS

[<AutoOpen>]
module CpuLoader =

    ///CPU에 DsSystem 규격으로 code 불러 로딩하기
    let LoadStatements(system:DsSystem) = 
        let statements, dicMemory =  ConvertSystem(system)

        statements, dicMemory

    ///CPU에 DsSystem 규격으로 code 불러 Text으로 리턴
    let LoadStatementsForText(system:DsSystem) = 
        let statements, dicMemory =  ConvertSystem(system)

        statements
            .Select(fun (desc, statement) -> statement.ToText())
            .JoinWith("\r\n")

