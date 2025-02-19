namespace DsMxComm

open System.Collections.Generic

[<AutoOpen>]
module MelsecReadBatchModule =


    type WordBatch(tags: MxTag seq) =
        // 태그를 WordTag 기준으로 그룹화하여 딕셔너리 생성
        let dicTag = 
            tags 
            |> Seq.groupBy (fun tag -> tag.WordTag)
            |> dict

        // WordTag를 키로 하여 초기값이 16비트 (0)인 버퍼 딕셔너리 생성
        let buffer = 
            dicTag.Keys 
            |> Seq.map (fun wordTag -> wordTag, 0s) // 16비트 short 사용
            |> dict

        // 태그 목록과 버퍼 딕셔너리 속성으로 노출
        member _.Tags = tags
        member _.Buffer = buffer

        /// 특정 WordTag 키에 대해 값을 업데이트하는 함수
        member _.UpdateBuffer(wordTag: string, value: int16) =
            if buffer.ContainsKey(wordTag) then
                buffer.[wordTag] <- value
            else
                invalidArg "wordTag" $"'{wordTag}' 키를 찾을 수 없습니다."

        /// 버퍼를 512개의 키 단위로 청크하여 반환
        member _.GetBatches() =
            buffer.Keys
            |> Seq.chunkBySize 512
            |> Seq.map (fun batch -> batch, batch |> Array.map (fun key -> buffer.[key]))


    let prepareReadBatches(tags: string seq) : WordBatch[] =
      // MxTag를 생성하고 WordBatch에 그룹화
        let groupTags = 
            tags 
            |> Seq.map (fun tag -> 
                    match tryParseMxTag(tag) with
                    | Some tagInfo -> MxTag(tag, tagInfo)
                    | None -> failwith $"Invalid tag: {tag}"
                )
            |> Seq.groupBy(fun tag -> tag.WordTag)

            // 512개 단위로 WordBatch 배열을 생성
        let batches = 
            groupTags
            |> Seq.chunkBySize 512
            |> Seq.map (fun chunk -> 
                let tags = chunk |> Seq.collect(fun(_, tags) -> tags)
                WordBatch(tags))
            |> Seq.toArray

        batches
