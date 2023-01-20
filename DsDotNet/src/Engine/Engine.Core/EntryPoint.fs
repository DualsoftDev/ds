namespace Engine.Core

module ModuleInitializer =
    let Initialize() =
        printfn "Module is being initialized..."
        fwdSerializeFunctionNameAndBoxedArguments <- serializeFunctionNameAndBoxedArguments

        (* Engine.CodeGenCPU dll loading 시, PlcTag<> 를 생성하는 함수로 overriding 됨 *)
        fwdCreateBoolTag <-
            let createBoolTag name value = PlanTag<bool>(name, value) :> TagBase<bool>
            createBoolTag


        (* Engine.CodeGenCPU dll loading 시, PlcTag<> 를 생성하는 함수로 overriding 됨 *)
        fwdCreateUShortTag <-
            let createUShortTag name value = PlanTag<uint16>(name, value) :> TagBase<uint16>
            createUShortTag
