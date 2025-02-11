namespace Engine.Core

open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Collections.Generic
open System.Diagnostics

[<AutoOpen>]
module TagWebModule =

    // C# interop 을 위해서 record type 대신 class type 으로..
    [<AllowNullLiteral>]
    [<DebuggerDisplay("{Name}")>]
    type TagWeb(name:Name, value:obj, kind:int, kindDescription:string, message:string) =
        let serializedObject = ObjectHolder.CreateFromObject(value).Serialize()

        new() = TagWeb("", "", 0, "", "")
        new(name, object, kind, kindDescription) = TagWeb(name, object, kind, kindDescription, "")

        member val Name = name with get, set    //FQDN 고유이름

        // !!! Value set 은 SetValue() extension 함수를 사용해야 함
        /// serializedObject 예: "{\"RawValue\":false,\"Type\":1}"
        member val _SerializedObject = serializedObject with get, set

        member val Kind = kind with get, set //Tag 종류 ex) going = 11007
        member val KindDescription = kindDescription with get, set
        member val Message = message with get, set //에러 내용 및 기타 전달 Message
        member val WritableValue = value with get, set //Cpu로 부터 쓰여진 값

    type TagWeb with
        member x.Value:obj = ObjectHolder.Deserialize(x._SerializedObject).GetValue()

    type HMIPush              = TagWeb
    type HMILamp              = TagWeb
    ///btn/lamp            ex)drive_btn/drive_lamp
    type HMIPushLamp          = TagWeb*TagWeb
    ///HMIPushLamp/mode    ex)drive_btn/drive_lamp/drive_state
    type HMIPushLampMode      = HMIPushLamp*TagWeb
    ///HMIPushLampA/HMIPushLampB  ex)Sys_Auto/Sys_Manu
    type HMISelectLamp        = HMIPushLamp*HMIPushLamp
    ///HMIPushLampModeA/HMIPushLampModeB  ex)Flow_Auto/Flow_Manu
    type HMISelectLampMode    = HMIPushLampMode*HMIPushLampMode

    type HMIPushMultiLamp = HMIPush*(HMILamp seq) // output inputs

[<AutoOpen>]
[<Extension>]
type TagWebExt =
    [<Extension>] static member GetValue (x:TagWeb) : obj = x.Value

    [<Extension>]
    static member GetWebTag(x:IStorage, kindDescription:Dictionary<int, string>) : TagWeb =
        TagWeb(x.Name, x.BoxedValue, x.TagKind, kindDescription[x.TagKind], "")

    [<Extension>]
    static member SetValue(x:TagWeb, value:obj) =
        if value = true then    // obj 이므로 '= true' 생략 못함
            debugfn $"Found true set value for {x.Name}"
        x._SerializedObject <- ObjectHolder.CreateFromObject(value).Serialize()
    [<Extension>]
    static member IsEqual(x:TagWeb, y:TagWeb) =
        x <> null && y <> null &&
        x.Name = y.Name && x.Kind = y.Kind
