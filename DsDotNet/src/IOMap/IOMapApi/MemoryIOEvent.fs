namespace IOMapApi

open System
open System.Collections.Generic
open System.Threading
open MemoryIOApi

module MemoryIOEventImpl =
    
    type MemoryChangedEventArgs<'T>(device: string, index: uint64, value: 'T) =
        inherit EventArgs()
        member this.Value = value
        member _.GetDeviceAddress() = $"{device}{value.GetType().Name}{index}" 
        override _.ToString() = $"Changed Device: {device}, Value: {value}, Index: {index}"

    let MemoryChanged = new Event<MemoryChangedEventArgs<_>>()

    type MemoryIOEvent(deviceName: string) =
        let memoryIO = MemoryIO(deviceName)
        let mutable currentData = Array.zeroCreate<byte>(memoryIO.MemorySize |> int)

        let hasBitChanged oldVal newVal pos = 
            (oldVal &&& (1uy <<< pos)) <> (newVal &&& (1uy <<< pos))

        let compareAndTrigger (newData: byte[]) i =
            let oldByte = currentData[i]
            let newByte = newData[i]

            if oldByte <> newByte then
                [0..7]
                |> List.iter (fun bit ->
                    if hasBitChanged oldByte newByte bit then
                        let v = (newByte &&& (1uy <<< bit)) <> 0uy
                        if v then
                            MemoryChanged.Trigger(MemoryChangedEventArgs<obj>(deviceName, uint64(i * 8 + bit), v)))

            if i % 8 = 0 && i < currentData.Length - 7 
               && BitConverter.ToUInt64(currentData, i) <> BitConverter.ToUInt64(newData, i) then 
                   MemoryChanged.Trigger(MemoryChangedEventArgs<obj>(deviceName, uint64(i/8), BitConverter.ToUInt64(newData, i)))
            if oldByte <> newByte then
                currentData[i] <-  newData[i]
        member val DeviceName = deviceName with get
        member this.MemoryIO = memoryIO 

        member this.UpdateMemory(newData: byte[]) =
            newData |> Array.iteri (fun i _ -> compareAndTrigger newData i)

    let monitorDevice(me: MemoryIOEvent, cancellationToken: CancellationToken) =
        async {
            while not cancellationToken.IsCancellationRequested do
                do! Async.Sleep(1) // Adjust based on needs
                let newData = me.MemoryIO.Read(0, me.MemoryIO.MemorySize |> int)
                me.UpdateMemory(newData)
        }
        
    let mutable cts = new CancellationTokenSource()
    let memorySet = HashSet<MemoryIOEvent>()
    
    let stop() = cts.Cancel()
    let create(deviceNames: string seq) =
        stop()
        memorySet.Clear()
        deviceNames |> Seq.iter(fun d -> memorySet.Add(MemoryIOEvent(d)) |> ignore)

    let run() =
        if cts.IsCancellationRequested then
            cts <- new CancellationTokenSource()
            memorySet
            |> Seq.map (fun device -> monitorDevice(device, cts.Token))
            |> Async.Parallel
            |> Async.Ignore
            |> Async.RunSynchronously
