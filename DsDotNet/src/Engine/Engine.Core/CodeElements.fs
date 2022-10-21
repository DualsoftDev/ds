namespace Engine.Core

[<AutoOpen>]
module CodeElements =
    /// Variable Declaration: name = (type, init)
    type Variable(name:string, varType:string, initValue:string) =
        member _.Name = name
        member _.Type = varType
        member _.InitValue = initValue

    type ParameterGroup = string[]
    type FunctionApplication(functionName:string, parameterGroups:ParameterGroup[]) =
        member _.FunctionName = functionName
        member _.ParameterGroups = parameterGroups

    type Command(name:string, functionApplication:FunctionApplication) =
        member _.Name = name
        member _.FunctionApplication = functionApplication

    type Observe(name:string, functionApplication:FunctionApplication) =
        member _.Name = name
        member _.FunctionApplication = functionApplication
