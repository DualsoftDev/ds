namespace OPC.DSClient

open System
open System.ComponentModel
open System.Diagnostics
open System.Drawing
open Opc.Ua
open Engine.Core

[<DebuggerDisplay("{Name}")>]
type OPCClientTag(iStorage: IStorage) =
    let mutable value: obj = "N/A"
    let mutable dateTime: DateTime = DateTime.MinValue
    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
    let mutable handler: PropertyChangedEventHandler option = None

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member _.PropertyChanged = propertyChanged.Publish
    [<CLIEvent>]
    member x.PropertyChanged = (x :> INotifyPropertyChanged).PropertyChanged
    
    member private this.OnOPCValueChanged(propertyName: string) =
        propertyChanged.Trigger(this, PropertyChangedEventArgs(propertyName))
        
    /// 이벤트 핸들러 추가
    member x.AddHandler(h: PropertyChangedEventHandler) =
        if handler.IsNone then
            handler <- Some h
            propertyChanged.Publish.AddHandler h

    /// 이벤트 핸들러 제거
    member x.RemoveHandler() =
        match handler with
        | Some h -> 
            propertyChanged.Publish.RemoveHandler h
            handler <- None
        | None -> ()

    member x.DsStorage = iStorage
    member x.Name = iStorage.Name   
    member x.Value
        with get() = value
        and set(newValue: obj) =
            if not (obj.Equals(value, newValue)) then
                value <- newValue
                x.OnOPCValueChanged("Value")

    member x.Timestamp
        with get() = dateTime
        and set(newValue: DateTime) =
            if not (obj.Equals(dateTime, newValue)) then
                dateTime <- newValue

    member val NodeId: NodeId = null with get, set
