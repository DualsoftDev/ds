namespace Engine.Core

module ModuleInitializer =
    let Initialize() =
        printfn "Engine.Core Module is being initialized..."
        fwdSerializeFunctionNameAndBoxedArguments <- serializeFunctionNameAndBoxedArguments

        fwdCreateBoolMemberVariable <-
            let createBoolTag name value =
                let param = {defaultStorageCreationParams(value) with Name=name; }
                MemberVariable<bool>(param) :> VariableBase<bool>
            createBoolTag


        fwdCreateUShortMemberVariable <-
            let createUShortTag name value =
                let param = {defaultStorageCreationParams(value) with Name=name; }
                MemberVariable<uint16>(param) :> VariableBase<uint16>
            createUShortTag
