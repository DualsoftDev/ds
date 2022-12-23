namespace PLC.CodeGen.Common

open Engine.Core

module ModuleInitializer =
    let Initialize() =
        printfn "Module is being initialized..."
        fwdFlattenExpression <- flattenExpression


