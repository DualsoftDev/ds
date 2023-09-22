namespace IOMap.LS

open System.IO
open System
open System.Collections.Generic


type DeviceCPUInfo = {
    nID: int
    nPLCID: int
    strDevice: string
    nSizeWord: int
}

type PLCType = {
    nPLCID: int
    nAPPID: int
    strPLCType: string
    strCPUType: string
    nShowPLC: bool
}

[<AutoOpen>]
module DeviceSizeImpl = 
    
    let pathDevice = Path.Combine(__SOURCE_DIRECTORY__, "DeviceSizeInfoForComm.csv")
    let pathCpu    = Path.Combine(__SOURCE_DIRECTORY__, "PLCTypeList.csv")
    let folderNfile m d = @$"LS Electric\{m.strPLCType.Split('-')[0]}\{d.strDevice}"
    
    let parseCSV filePath =
        File.ReadAllLines(filePath)
        |> Array.skip 1
        |> Array.map (fun line -> line.Split(','))
        |> Array.toSeq

//nPLCID,nAPPID,strPLCType,strCPUType,nShowPLC
//0,1,XGK-CPUH,0xA001,1
//1,1,XGK-CPUS,0xA002,1
//2,2,XGB-XBMS,0xB001,1
    let modelPLCs = parseCSV pathCpu |> Seq.map (fun f ->
            {
                nPLCID= int f.[0]
                nAPPID= int f.[1]
                strPLCType=  f.[2]
                strCPUType=  f.[3]
                nShowPLC=  if f.[4] = "1" then true else false
            })
//nID,nPLCID,strDevice,nSize
//1,0,P,2048
//3,0,K,2048
//5,0,T,128
    let dataDevice = parseCSV pathDevice |> Seq.map (fun f ->
            {
                nID = int f.[0]
                nPLCID = int f.[1]
                strDevice = f.[2]
                nSizeWord = int f.[3]
            })

    let getModel nPLCID = 
        modelPLCs |> Seq.filter(fun m -> m.nPLCID = nPLCID) 
    let getModelByName cpuName = 
        modelPLCs |> Seq.filter(fun m -> m.strPLCType = cpuName) |> Seq.head
          
    let private cpuDeviceMap =

        let dicModelDevice = 
            modelPLCs |> Seq.map (fun m -> m, HashSet<DeviceCPUInfo>()) |> dict
        
        dataDevice
        |> Seq.iter (fun f -> 
            let m = getModel f.nPLCID |> Seq.toList
            assert (m.Length <= 1)
            if m.Length = 0
            then 
                Console.WriteLine $"Device {f.strDevice} nPLCID: {f.nPLCID} cannot be found in the PLC list."
            else 
                dicModelDevice.[m[0]].Add f |> ignore
            )

        dicModelDevice

    type DeviceSize() =
           
        static member GetCPUInfos(cpu: string) =  cpuDeviceMap.[getModelByName cpu]
        static member GetMemoryInfos(cpu: string) =
            modelPLCs
            |> Seq.where(fun m -> m.strPLCType = cpu)
            |> Seq.collect(fun m -> 
                cpuDeviceMap.[getModelByName cpu]
                |> Seq.map(fun d-> d, (folderNfile m d))
            )
            
    