namespace Engine.CodeGenCPU

open Engine.Core

[<AutoOpen>]
module VariableManagerModule =

     type VariableManager(v:VariableData, sys:DsSystem)  =
        let stg = sys.TagManager.Storages
        let variTag =  createVariableByType v.Name v.Type

        do
            stg.Add(variTag.Name, variTag)

        interface ITagManager with
            member _.Target = v
            member _.Storages = stg

        member _.VariableTag = variTag
        member _.InitValue = v.Type.ToValue(v.InitValue)
        member _.VariableData = v