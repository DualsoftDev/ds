namespace Engine.Core

open Dual.Common.Core.FS
open System.Runtime.CompilerServices

[<AutoOpen>]
module TagWebModule =

   
    type TagWeb = {
        Name  : string //FQDN 고유이름
        _SerializedObject : string
        Kind  : int    //Tag 종류 ex) going = 11007
        Message : string //에러 내용 및 기타 전달 Message 
    }
    type TagWeb with
        member x.Value:obj = ObjectHolder.Deserialize(x._SerializedObject).GetValue()

    type HMIPush = TagWeb
    type HMILamp = TagWeb
    type HMIFlickerLamp = TagWeb
    type HMIButton = HMIPush*HMIFlickerLamp
    type HMIDevice = (HMIPush option)*(HMILamp option)  //input, output


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
    [<Extension>] static member SetMessage(x:TagWeb, messsage) : TagWeb = {x with Message=messsage}
    [<Extension>]
    static member GetWebTag(x:IStorage) : TagWeb =
        let createTagWeb (tag:IStorage) (qualifiedName:string) =
            { Name = qualifiedName; _SerializedObject = ObjectHolder.Create(tag.BoxedValue).Serialize(); Kind = tag.TagKind; Message = ""}
        
        match x.GetTagInfo() with
        |Some dsTag ->
            match dsTag with
            | EventSystem (tag, obj, _) -> createTagWeb tag obj.QualifiedName
            | EventFlow   (tag, obj, _) -> createTagWeb tag obj.QualifiedName
            | EventVertex (tag, obj, _) -> createTagWeb tag obj.QualifiedName
            | EventApiItem(tag, obj, _) -> createTagWeb tag obj.QualifiedName
            | EventAction (tag, obj, _) -> createTagWeb tag obj.QualifiedName

        |None ->  createTagWeb x x.Name
