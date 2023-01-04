namespace Engine.Core

[<AutoOpen>]
module CodeElements =
    (*
      [variables] = { //이름 = (타입,초기값)
        R100 = (word, 0)
      }
     *)
    /// Variable Declaration: name = (type, init)  CodeBlock 사용 ? 선택 필요 
    type VariableData(name:string, varType:DataType, initValue:string) =
        member _.Name = name
        member _.Type = varType
        member _.InitValue = initValue

        member _.ToDsText() = $"{name} = ({varType.ToText()}, {initValue})"

    type ParameterGroup = string[]
    // $ton 500   // &mov 0 R100 // &ctr 5
    type FunctionApplication(functionName:string, parameterGroups:ParameterGroup[]) =
        member _.FunctionName = functionName
        member _.ParameterGroups = parameterGroups

    type Command(name:string, functionApplication:FunctionApplication) =
        member _.Name = name
        member _.FunctionApplication = functionApplication

    type Observe(name:string, functionApplication:FunctionApplication) =
        member _.Name = name
        member _.FunctionApplication = functionApplication
