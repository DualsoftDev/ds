namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Linq
open System.Collections.Generic
open System

[<AutoOpen>]
module ConvertAddressModule =
  
    let updateDuplicateAddress (sys: DsSystem) = 
              // Aggregate all addresses to check for duplicates along with their API names
        let allAddresses = 
            [
                yield! sys.GetTaskDevsSkipEmptyAddress().Select(fst).Distinct()
                          |> Seq.collect(fun d -> [($"{d.ApiPureName}_IN", d.InTag); ($"{d.ApiPureName}_OUT", d.OutTag)])
                       
                yield! sys.HwSystemDefs
                          |> Seq.collect(fun h ->  [($"{h.Name}_IN", h.InTag); ($"{h.Name}_OUT", h.OutTag)])
            ] 
            |> Seq.distinctBy fst
            |> Seq.filter (fun (_, tag) -> tag.IsNonNull()) |> Seq.toList

        // Helper to find duplicate elements and group them by API names
        let findDuplicates (list:(string*ITag) list) =
            list 
            |> Seq.groupBy (fun (_name, tag) -> tag.Address)
            |> Seq.filter (fun (_, items) -> Seq.length items > 1)
            //|> Seq.map (fun (addr, items) -> addr, items |> Seq.map fst |> Seq.distinct |> Seq.toList)

          // Find and handle duplicates
        let duplicates = findDuplicates allAddresses

        if not (Seq.isEmpty duplicates) then
            duplicates 
            |> Seq.iter (fun (_addr, dupliTagSet) -> 
                let tags = dupliTagSet.Select(snd)
                let aliasSet = tags.Select(fun f->f.Name)
                tags.Iter(fun f->f.AliasNames.AddRange(aliasSet))
                )
             

    let setSimulationEmptyAddress(sys:DsSystem) = 
        sys.Jobs.ForEach(fun j->
            j.TaskDefs.ForEach(fun d-> 
                        if d.InAddress.IsNullOrEmpty() then  d.InAddress <- (TextAddrEmpty)
                        if d.OutAddress.IsNullOrEmpty() then d.OutAddress <- (TextAddrEmpty)
                        if d.MaunualAddress.IsNullOrEmpty() then d.MaunualAddress <- (TextAddrEmpty)
                )
            )
        sys.HWLamps.ForEach(fun l -> 
                        if l.OutAddress.IsNullOrEmpty() then  l.OutAddress <-TextAddrEmpty
                        )
        sys.HWButtons.ForEach(fun b->                                         
                         if b.InAddress.IsNullOrEmpty() then   b.InAddress <-TextAddrEmpty
                         if b.OutAddress.IsNullOrEmpty() then  b.OutAddress <- TextAddrEmpty
                        )   
        sys.HWConditions.ForEach(fun c->                                         
                         if c.InAddress.IsNullOrEmpty() then   c.InAddress <-TextAddrEmpty
                         if c.OutAddress.IsNullOrEmpty() then  c.OutAddress <- TextAddrEmpty
                        )   
