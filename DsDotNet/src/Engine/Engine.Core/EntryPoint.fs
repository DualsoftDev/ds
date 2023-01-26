namespace Engine.Core

module ModuleInitializer =
    let Initialize() =
        printfn "Module is being initialized..."
        fwdSerializeFunctionNameAndBoxedArguments <- serializeFunctionNameAndBoxedArguments

        fwdCreateBoolEndoTag <-
            let createBoolTag name value =
                let param = {defaultStorageCreationParams(value) with Name=name; }
                EndoTag<bool>(param) :> TagBase<bool>
            createBoolTag


        fwdCreateUShortEndoTag <-
            let createUShortTag name value =
                let param = {defaultStorageCreationParams(value) with Name=name; }
                EndoTag<uint16>(param) :> TagBase<uint16>
            createUShortTag
