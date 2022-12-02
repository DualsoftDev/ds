namespace Engine.Core

module ModuleInitializer =
    let Initialize() =
        printfn "Module is being initialized..."
        fwdSerializeFunctionNameAndBoxedArguments <- serializeFunctionNameAndBoxedArguments
