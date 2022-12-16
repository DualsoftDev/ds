namespace Engine.Core

module ModuleInitializer =
    let Initialize() =
        printfn "Module is being initialized..."
        fwdSerializeFunctionNameAndBoxedArguments <- serializeFunctionNameAndBoxedArguments

        let createBoolTag name value = DsTag<bool>(name, value) :> Tag<bool>
        fwdCreateBoolTag <- createBoolTag


        let createUShortTag name value = DsTag<uint16>(name, value) :> Tag<uint16>
        fwdCreateUShortTag <- createUShortTag
