namespace Client

open IO.Core
open System
open System.Linq
open System.Threading
open ZmqTestModule
open Dual.Common.Core.FS
open IOClient.DS.ScanDSImpl
open Engine.Core
open System.IO
open Engine.Parser.FS
open Engine.Cpu

module ZmqStartDs =
  
    let zmqStartDs(dsCPU:DsCPU, serverIOHub:Server, clientDs:Client) = 
        
        let iospec = clientDs.GetMeta()
        iospec|>regulate
        
        IoEventDS(dsCPU, iospec.Vendors, clientDs, serverIOHub) |>ignore


