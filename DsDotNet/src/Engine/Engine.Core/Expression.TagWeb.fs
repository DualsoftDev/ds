namespace Engine.Core

open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Collections
open System.Collections.Generic
open System

[<AutoOpen>]
module TagWebModule =

    // C# interop 을 위해서 record type 대신 class type 으로..
    [<AllowNullLiteral>]
    type TagWeb(name:Name, value:obj, kind:int, kindDescription:string, message:string) =
        let serializedObject = ObjectHolder.Create(value).Serialize()

        new() = TagWeb("", "", 0, "", "")
        new(name, object, kind, kindDescription) = TagWeb(name, object, kind, kindDescription, "")

        member val Name = name with get, set    //FQDN 고유이름
        /// serializedObject 예: "{\"RawValue\":false,\"Type\":1}"
        member val _SerializedObject = serializedObject with get, set
        member val Kind = kind with get, set //Tag 종류 ex) going = 11007
        member val KindDescription = kindDescription with get, set
        member val Message = message with get, set //에러 내용 및 기타 전달 Message 
        member val WritableValue = value with get, set //Cpu로 부터 쓰여진 값 

    type TagWeb with
        member x.Value:obj = ObjectHolder.Deserialize(x._SerializedObject).GetValue()

    type HMIPush   = TagWeb
    type HMISelect = TagWeb*TagWeb //selectA/selectB  ex)Auto/Manu   
    type HMILamp   = TagWeb

    [<Obsolete("HMIFlickerLamp TagWeb 없음 직접 Lamp반응")>]
    type HMIFlickerLamp = TagWeb
    [<Obsolete("HMIButton => HMIPush, HMISelect, HMIPushMultiLamp 로 구분")>]
    type HMIButton = HMIPush*HMIFlickerLamp
    [<Obsolete("HMIPushMultiLamp 로 사용")>]
    type HMIDevice = HMIPush*HMILamp  //input, output


    type HMIPushMultiLamp = HMIPush*(HMILamp seq) // output inputs

    [<Obsolete("type HmiPackage 대신 사용 (Engine.CodeGenHMI에서 생성함)")>]
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


    //HMITagPackage 사용안함 대신 HMIPackage 사용
    //HMIPackage에서 Core 구조체 넘겨서 이제  obj.QualifiedName 필요 없음 
    //[<Extension>]
    //static member GetWebTag(x:IStorage, kindDescription:Dictionary<int, string>) : TagWeb =
    //    let createTagWeb (tag:IStorage) (qualifiedName:string) =
    //        TagWeb(qualifiedName, tag.BoxedValue, tag.TagKind, kindDescription[tag.TagKind], "")
        
    //    match x.GetTagInfo() with
    //    | Some dsTag ->
    //        match dsTag with
    //        | EventSystem (tag, obj, _) -> createTagWeb tag obj.QualifiedName
    //        | EventFlow   (tag, obj, _) -> createTagWeb tag obj.QualifiedName
    //        | EventVertex (tag, obj, _) -> createTagWeb tag obj.QualifiedName
    //        | EventApiItem(tag, obj, _) -> createTagWeb tag obj.QualifiedName
    //        | EventAction (tag, obj, _) -> createTagWeb tag obj.QualifiedName

    //    | None ->  createTagWeb x x.Name


    [<Extension>]
    static member GetWebTag(x:IStorage, kindDescription:Dictionary<int, string>) : TagWeb =
            TagWeb(x.Name, x.BoxedValue, x.TagKind, kindDescription[x.TagKind], "")
        
    [<Extension>]
    static member SetValue(x:TagWeb, value:obj) = x._SerializedObject <- ObjectHolder.Create(value).Serialize()
