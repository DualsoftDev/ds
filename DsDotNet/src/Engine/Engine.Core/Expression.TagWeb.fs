namespace Engine.Core

open Dual.Common.Core.FS
open System
open System.Linq
open System.Runtime.CompilerServices
open System.Reactive.Subjects

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
