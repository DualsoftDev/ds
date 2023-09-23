namespace IOMapForModeler

open Engine.Core
open HwTagModule
open IOMapApi

module HwTagEventModule = 
    
    let CreateHwTagEvent(devices, hwTags:IHwTag seq) =

        //let hwTagValueChanged = new Event<IHwTag>()
        let dicTag = hwTags |> Seq.groupBy(fun t-> t.GetDeviceAddress())
                            |> dict

        MemoryIOEventImpl.MemoryChanged.Publish.Subscribe(fun args -> 
                let key = args.GetDeviceAddress()
                if dicTag.ContainsKey(key) 
                then 
                    let hwTags = dicTag[key]
                    hwTags |>Seq.iter (fun t->
                        t.Value <- args.Value
                       (* hwTagValueChanged.Trigger t*))
                else 
                    System.Diagnostics.Debug.WriteLine $"{key} has been changed, but this tag has not been assigned to DS Tag."
                ) |>ignore

        MemoryIOEventImpl.create devices
        //hwTagValueChanged.Publish


    let RunTagEvent() =
        MemoryIOEventImpl.run()

