namespace Engine.Runner


open Engine.Common.FS
open Engine.Core
open System.Collections.Concurrent
open Engine

[<AutoOpen>]
module DataModule =
    
    type DataTag (tag:Tag) =
        inherit Bit(tag.Name, tag.Value) 
        member x.OriginalTag = tag
        member this.SetValue(newValue) = this._value <- newValue;

    type DataBroker() =
        let _tagDic = ConcurrentDictionary<string, DataTag>()
        
        //TAG 등록
        member x.AddTag(tag:Tag) = 
            _tagDic.TryAdd(tag.Name, DataTag(tag)) |>ignore
        member x.AddTags(tags:Tag seq) = 
            tags.foreach(fun tag -> x.AddTag tag)
                
        //TAG 가져오기
        member x.GetTag(tagName:string ) = 
            if(_tagDic.ContainsKey(tagName))
            then _tagDic.[tagName]
            else failwithlog $"등록된 {tagName} TAG 없습니다. AddTag를 수행하세요"

        //TAG 읽기
        member x.ReadTags(tagNames:string seq) = 
            tagNames.where(fun name -> _tagDic.ContainsKey(name))
                    .select(fun name -> name, _tagDic.[name].Value)
        
        //TAG 쓰기
        member x.Write(tagName:string, value) = 
            let tag = x.GetTag(tagName)
            //assert(tag.Value = value|>not)
            tag.SetValue(value)
            Core.Global.TagChangeFromOpcServerSubject.OnNext(new OpcTagChange(tagName, value));

        //통신연결
        member x.CommunicationPLC() = () //todo : 통신 연결
        //통신수신
        member x.StreamData() = () //todo : 데이터 수신
           

           