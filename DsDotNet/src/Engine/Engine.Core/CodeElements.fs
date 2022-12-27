namespace Engine.Core

[<AutoOpen>]
module CodeElements = ()
    (*
      [variables] = { //이름 = (타입,초기값)
        R100 = (word, 0)
      }
     *)
    /// Variable Declaration: name = (type, init)
    //type VariableData(name:string, varType:string, initValue:string) =
    //    member _.Name = name
    //    member _.Type = varType
    //    member _.InitValue = initValue

    //type ParameterGroup = string[]
    //// @add = 30, 50 ~ R103
    //type FunctionApplication(functionName:string, parameterGroups:ParameterGroup[]) =
    //    member _.FunctionName = functionName
    //    member _.ParameterGroups = parameterGroups

    //// CMD3 = (@add = 30, 50 ~ R103)
    //type Command(name:string, functionApplication:FunctionApplication) =
    //    member _.Name = name
    //    member _.FunctionApplication = functionApplication

    //type Observe(name:string, functionApplication:FunctionApplication) =
    //    member _.Name = name
    //    member _.FunctionApplication = functionApplication
