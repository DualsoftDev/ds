namespace Engine.CpuUnit

open Newtonsoft.Json
open System.Collections.Concurrent

[<AutoOpen>]
module CpuLoader =
    let BitMap = ConcurrentDictionary<string, Bit>()
    
    type CodeUnit() = 
         member val GeteName = "" with get, set
         member val Out   = "" with get, set
         member val In1  = "" with get, set //Sets, Ors, Ands, ...
         member val In2  = "" with get, set //Resets
         member val In3  = "" with get, set //spare

    let UpdateBitMap(codeUnit:CodeUnit) = 
        [|codeUnit.Out|] 
        |> Seq.append (codeUnit.In1.Split(';') |> Seq.toArray)
        |> Seq.append (codeUnit.In2.Split(';') |> Seq.toArray)
        |> Seq.append (codeUnit.In3.Split(';') |> Seq.toArray)
        |> Seq.iter(fun tagName ->
                    let name = tagName.TrimStart('!')
                    if name.Length>0 then BitMap.TryAdd(name, Bit(name)) |> ignore)

    let testText = 
            """
            [
                {
                "GeteName": "GateAND",
                "Out": "O1",
                "In1": "A;B;C;D"
                },
                {
                "GeteName": "GateSR",
                "Out": "O1",
                "In1": "A;!B;C;D",
                "In2": "E;F"
                }
            ]
            """
        
    ///CPU에 text 규격으로 code 불러 로딩하기
    let LoadText(code:string) = 
        BitMap.Clear()
        let codeUnits = JsonConvert.DeserializeObject<CodeUnit array>(code)
        codeUnits |> Seq.iter(fun codeUnit -> UpdateBitMap codeUnit)
        codeUnits |> Seq.map (fun codeUnit ->
        
            match codeUnit.GeteName with
            |"GateAND" -> let gateAND = GateAND(BitMap.[codeUnit.Out])
                          codeUnit.In1.Split(';') |> Seq.iter(fun bit->  gateAND.Add(BitMap.[bit.TrimStart('!')], bit.StartsWith("!"))|>ignore)
                          gateAND :> Gate

            |"GateOR"  -> let gateOR = GateOR(BitMap.[codeUnit.Out])
                          codeUnit.In1.Split(';') |> Seq.iter(fun bit->  gateOR.Add(BitMap.[bit.TrimStart('!')], bit.StartsWith("!"))|>ignore)
                          gateOR :> Gate
            
            |"GateSR"  -> let gateSR = GateSR(BitMap.[codeUnit.Out])
                          codeUnit.In1.Split(';') |> Seq.iter(fun bit->  gateSR.AddSet(BitMap.[bit.TrimStart('!')], bit.StartsWith("!"))|>ignore)
                          codeUnit.In2.Split(';') |> Seq.iter(fun bit->  gateSR.AddRst(BitMap.[bit.TrimStart('!')], bit.StartsWith("!"))|>ignore)
                          gateSR :> Gate

            |_ -> failwith $"{codeUnit.GeteName} is not gateName"
        ) |> Seq.toList

    [<EntryPoint>]        
    let main argv = 
        let a = LoadText(testText)
        0

        