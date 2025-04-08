namespace PLC.Mapper.FS

open System.Collections.Generic

module PrefixTrie =

    /// 접두어 검색용 Trie 노드 정의
    type TrieNode() =
        let children = Dictionary<char, TrieNode>()
        let mutable isTerminal = false
        let mutable value: string option = None

        member _.Children = children
        member _.IsTerminal with get() = isTerminal and set v = isTerminal <- v
        member _.Value with get() = value and set v = value <- v

    /// 문자열 접두어 목록을 Trie 구조로 구축
    let buildPrefixTrie (prefixes: string seq) : TrieNode =
        let root = TrieNode()
        for prefix in prefixes do
            let mutable node = root
            for ch in prefix do
                if not (node.Children.ContainsKey ch) then
                    node.Children.[ch] <- TrieNode()
                node <- node.Children.[ch]
            node.IsTerminal <- true
            node.Value <- Some prefix
        root

    /// 주어진 문자열에서 가장 긴 일치 접두어를 Trie에서 검색
    let tryFindLongestPrefix (root: TrieNode) (input: string) : string option =
        let mutable node = root
        let mutable result = None

        input
        |> Seq.tryPick (fun ch ->
            match node.Children.TryGetValue ch with
            | true, next ->
                node <- next
                if node.IsTerminal then result <- node.Value
                None
            | _ -> Some() // 조기 탈출
        )
        |> ignore

        result

