namespace IO.Dualsoft

open System
open IO.Spec
open System.Collections.Generic


type AddressInfo = string*int*int //memoryName, offset, bitSize

module AddressMapModule = 
    let AddressMap = Dictionary<string, AddressInfo>()
