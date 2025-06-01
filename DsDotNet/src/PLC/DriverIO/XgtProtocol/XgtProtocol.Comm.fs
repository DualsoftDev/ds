namespace XgtProtocol

open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading
open System.Text.RegularExpressions
open Dual.PLC.Common.FS
open NetworkUtils
open ReadWriteBlockFactory
open DataConverter
open FrameBuilder

/// XGT Ethernet communication class for PLC interaction
type XgtEthernet(ip: string, port: int, timeoutMs: int) =
    inherit PlcEthernetBase(ip, port, timeoutMs)
    
    let frameID = getFrameIDBytesFromIP ip
    
    /// Read data from multiple addresses
    member this.Reads(addresses: string[], localEthernet: bool, dataTypes: PlcDataSizeType[], readBuffer: byte[]) =
        if Array.isEmpty addresses then 
            failwith "No addresses provided"
        

        let localEthernet = true
        let frame =
            if localEthernet then
                createMultiReadFrame frameID addresses dataTypes
            else
                createMultiReadFrameEFMTB frameID addresses dataTypes
        
        this.SendFrame frame
        let buffer = this.ReceiveFrame (50 + addresses.Length * 16)
        
        if localEthernet then
            parseMultiReadResponse buffer  addresses.Length  dataTypes  readBuffer 
        else
            parseEFMTBMultiReadResponse buffer  addresses.Length  dataTypes  readBuffer

    /// Write data to multiple addresses
    member this.Writes(addresses: string[], localEthernet: bool, dataTypes: PlcDataSizeType[], values: obj[]) : bool =
        try
            if addresses.Length <> dataTypes.Length || addresses.Length <> values.Length then
                failwith "Array lengths must match"

            let blocks = 
                addresses |> Array.mapi (fun i addr -> 
                    getReadWriteBlock addr dataTypes.[i] values.[i]
                ) 
            
            let frame =
                if localEthernet then
                    createMultiWriteFrameFromBlock frameID blocks
                else
                    createMultiWriteFrameEFMTBFromBlock frameID blocks
            
            this.SendFrame frame
            let _ = this.ReceiveFrame (50 + addresses.Length * 16)
            true
        with
        | _ -> false

    /// Read data from a single address
    member this.Read(address: string, localEthernet: bool, dataType: PlcDataSizeType, readBuffer: byte[]) =
        this.Reads([| address |], localEthernet, [| dataType |], readBuffer)
    
    /// Write data to a single address
    member this.Write(address: string, localEthernet: bool, dataType: PlcDataSizeType, value: obj) : bool =
        this.Writes([| address |], localEthernet, [| dataType |], [| value |])
    