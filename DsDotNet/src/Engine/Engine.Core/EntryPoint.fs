namespace Engine.Core

module ModuleInitializer =
    let Initialize() =
        printfn "Engine.Core Module is being initialized..."
        fwdSerializeFunctionNameAndBoxedArguments <- serializeFunctionNameAndBoxedArguments

        fwdCreateBoolMemberVariable <-
            let createBoolTag name value tagKind =
                let param = {defaultStorageCreationParams(value) tagKind with Name=name; }
                MemberVariable<bool>(param) :> VariableBase<bool>
            createBoolTag


        fwdCreateUShortMemberVariable <-
            let createUShortTag name value tagKind  =
                let param = {defaultStorageCreationParams(value) tagKind  with Name=name; }
                MemberVariable<uint16>(param) :> VariableBase<uint16>
            createUShortTag


        fwdCreateUInt32MemberVariable <-
            let createUInt32Tag name value tagKind  =
                let param = {defaultStorageCreationParams(value) tagKind  with Name=name; }
                MemberVariable<uint32>(param) :> VariableBase<uint32>
            createUInt32Tag

        fwdCreateEdgeOnFlow := (fun (flow: Flow) (mei: ModelingEdgeInfo<Vertex>) -> flow.CreateEdgeImpl(mei))
        fwdCreateEdgeOnReal := (fun (real: Real) (mei: ModelingEdgeInfo<Vertex>) -> real.CreateEdgeImpl(mei))