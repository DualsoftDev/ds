namespace Dual.PLC.Common.FS

open System
open System.Runtime.CompilerServices

[<AutoOpen>]
module ScanPLCModule =

    type IScanPLC = 
        abstract TagValueChangedNotify: Event<TagPLCValueChangedEventArgs>
        abstract ConnectChangedNotify: Event<ConnectChangedEventArgs>
     