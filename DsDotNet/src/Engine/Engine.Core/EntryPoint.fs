namespace Engine.Core

module ModuleInitializer =
    let Initialize() =
        printfn "Module is being initialized..."
        fwdSerializeFunctionNameAndBoxedArguments <- serializeFunctionNameAndBoxedArguments

        fwdCreateBoolTag <-
            let createBoolTag name value = DsTag<bool>(name, value) :> TagBase<bool>
            createBoolTag


        fwdCreateUShortTag <-
            let createUShortTag name value = DsTag<uint16>(name, value) :> TagBase<uint16>
            createUShortTag
