namespace IOMapForModeler

open Engine.Core
open HwTagModule

module HwTagEventModule = 
    
    let CreateHwTagEvent(_, _:IHwTag seq) = ()

        //let hwTagValueChanged = new Event<IHwTag>()
        //let dicTag = hwTags |> Seq.groupBy(fun t-> t.GetDeviceAddress())
        //                    |> dict

        //let evt = MemoryIOEventImpl.MemoryChanged.Publish.Subscribe(fun args -> 
        //        let key = args.GetDeviceAddress()
        //        if dicTag.ContainsKey(key) 
        //        then 
        //            let hwTags = dicTag[key]
        //            hwTags
        //                |>Seq.where (fun t-> t.IOType = Input)
        //                |>Seq.iter (fun t->
        //                t.Value <- args.Value
        //               (* hwTagValueChanged.Trigger t*))
        //        else 
        //            System.Diagnostics.Debug.WriteLine $"{key} has been changed, but this tag has not been assigned to DS Tag."
        //        )

        //MemoryIOEventImpl.create devices
        //evt


    let RunTagEvent() = ()
        //MemoryIOEventImpl.run()

