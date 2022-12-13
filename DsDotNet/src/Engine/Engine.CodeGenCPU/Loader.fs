namespace Engine.CodeGenCPU

open System.Collections.Concurrent
open System.IO
open System.Linq
open Engine.Core
open System
open Engine.Common.FS

[<AutoOpen>]
module CpuLoader =

    ///CPU에 text 규격으로 code 불러 로딩하기
    let LoadStatements(system:DsSystem) =
        let statements =  ConvertSystem(system)

        statements

