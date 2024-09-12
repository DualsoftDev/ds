namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System.Linq

[<AutoOpen>]
module ConvertAddressModule =
  
    let updateDuplicateAddress (sys: DsSystem) = 
              // Aggregate all addresses to check for duplicates along with their API names
        let allAddresses = 
            [
                yield!
                    sys.GetTaskDevsSkipEmptyAddress()
                        .Select(fst)
                        .Distinct()
                        .Collect(fun d -> [($"{d.ApiPureName}_IN", d.InTag); ($"{d.ApiPureName}_OUT", d.OutTag)])
                       
                yield!
                    sys.HwSystemDefs
                        .Collect(fun h ->  [($"{h.Name}_IN", h.InTag); ($"{h.Name}_OUT", h.OutTag)])
            ] 
            |> Seq.distinctBy fst
            |> Seq.filter (fun (_, tag) -> tag.IsNonNull())
            |> Seq.toList

        // Helper to find duplicate elements and group them by API names
        let findDuplicates (list:(string*ITag) list) =
            list 
            |> Seq.groupBy (fun (_name, tag) -> tag.Address)
            |> Seq.filter (fun (_, items) -> Seq.length items > 1)
            //|> Seq.map (fun (addr, items) -> addr, items |> Seq.map fst |> Seq.distinct |> Seq.toList)

          // Find and handle duplicates
        let duplicates = findDuplicates allAddresses

        duplicates 
        |> Seq.iter (fun (_addr, dupliTagSet) -> 
            let tags = dupliTagSet.Select(snd)
            let aliasSet = tags.Select(fun f->f.Name)
            tags.Iter(fun f->f.AliasNames.AddRange(aliasSet))
            )
             
    let updateSourceTokenOrder(sys: DsSystem) = 
              // Aggregate all addresses to check for duplicates along with their API names
        sys.GetVerticesCallOperator().OrderBy(fun f->f.QualifiedName) |> Seq.iteri(fun i v -> v.TokenSourceOrder <- Some (i+1))
             
    let setSimulationEmptyAddress(sys:DsSystem) = 
        for j in sys.Jobs do
            for d in j.TaskDefs do
                if d.InAddress.IsNullOrEmpty() then  d.InAddress <- (TextAddrEmpty)
                if d.OutAddress.IsNullOrEmpty() then d.OutAddress <- (TextAddrEmpty)
                if d.MaunualAddress.IsNullOrEmpty() then d.MaunualAddress <- (TextAddrEmpty)

        for l in sys.HWLamps do
            if l.OutAddress.IsNullOrEmpty() then  l.OutAddress <-TextAddrEmpty

        for b in sys.HWButtons do
            if b.InAddress.IsNullOrEmpty() then   b.InAddress <-TextAddrEmpty
            if b.OutAddress.IsNullOrEmpty() then  b.OutAddress <- TextAddrEmpty

        for c in sys.HWConditions do
            if c.InAddress.IsNullOrEmpty() then   c.InAddress <-TextAddrEmpty
            if c.OutAddress.IsNullOrEmpty() then  c.OutAddress <- TextAddrEmpty

        for a in sys.HWActions do
            if a.InAddress.IsNullOrEmpty() then   a.InAddress <-TextAddrEmpty
            if a.OutAddress.IsNullOrEmpty() then  a.OutAddress <- TextAddrEmpty
