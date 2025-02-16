namespace Dual.PLC.Common.FS

open System
open System.Runtime.CompilerServices

[<AutoOpen>]
module TagPLC =

    
    type ITagPLC = 
        abstract Address: string 
        abstract Value: obj with get, set
        abstract SetWriteValue: obj -> unit
        abstract ClearWriteValue: unit -> unit
        abstract GetWriteValue: unit -> option<obj>

    type TagPLCValueChangedEventArgs = 
        { 
            Ip: string
            Tag: ITagPLC    
        }
