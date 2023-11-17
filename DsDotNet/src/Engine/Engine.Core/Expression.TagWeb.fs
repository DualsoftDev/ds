namespace Engine.Core

open Dual.Common.Core.FS
open System.Runtime.CompilerServices

[<AutoOpen>]
module TagWebModule =

    // C# interop 을 위해서 record type 대신 class type 으로..
    [<AllowNullLiteral>]
    type TagWeb(name:string, object:obj, kind:int, message:string) =
        let serializedObject = ObjectHolder.Create(object).Serialize()

        new() = TagWeb("", "", 0, "")
        new(name, object, kind) = TagWeb(name, object, kind, "")

        member val Name = name with get, set    //FQDN 고유이름
        /// serializedObject 예: "{\"RawValue\":false,\"Type\":1}"
        member val _SerializedObject = serializedObject with get, set
        member val Kind = kind with get, set //Tag 종류 ex) going = 11007
        member val Message = message with get, set //에러 내용 및 기타 전달 Message 

    type TagWeb with
        member x.Value:obj = ObjectHolder.Deserialize(x._SerializedObject).GetValue()

    type HMIPush = TagWeb
    type HMILamp = TagWeb
    type HMIFlickerLamp = TagWeb
    type HMIButton = HMIPush*HMIFlickerLamp
    type HMIDevice = HMIPush*HMILamp  //input, output


    type HmiTagPackage = {
        AutoButtons      : HMIButton array 
        ManualButtons    : HMIButton array 
        DriveButtons     : HMIButton array 
        StopButtons      : HMIButton array 
        ClearButtons     : HMIButton array 
        EmergencyButtons : HMIButton array 
        TestButtons      : HMIButton array 
        HomeButtons      : HMIButton array 
        ReadyButtons     : HMIButton array 
        
        DriveLamps       : HMILamp array 
        AutoLamps        : HMILamp array 
        ManualLamps      : HMILamp array 
        StopLamps        : HMILamp array 
        EmergencyLamps   : HMILamp array 
        TestLamps        : HMILamp array 
        ReadyLamps       : HMILamp array 
        IdleLamps        : HMILamp array 

        RealBtns         : HMIPush array 
        DeviceBtns       : HMIDevice array 
        //JobBtns          : HMIPush array  나중에
    }


[<AutoOpen>]
[<Extension>]
type TagWebExt =
    [<Extension>] static member GetValue (x:TagWeb) : obj = x.Value
    [<Extension>]
    static member GetWebTag(x:IStorage) : TagWeb =
        let createTagWeb (tag:IStorage) (qualifiedName:string) =
            TagWeb(qualifiedName, tag.BoxedValue, tag.TagKind, "")
        
        match x.GetTagInfo() with
        | Some dsTag ->
            match dsTag with
            | EventSystem (tag, obj, _) -> createTagWeb tag obj.QualifiedName
            | EventFlow   (tag, obj, _) -> createTagWeb tag obj.QualifiedName
            | EventVertex (tag, obj, _) -> createTagWeb tag obj.QualifiedName
            | EventApiItem(tag, obj, _) -> createTagWeb tag obj.QualifiedName
            | EventAction (tag, obj, _) -> createTagWeb tag obj.QualifiedName

        | None ->  createTagWeb x x.Name
