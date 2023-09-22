namespace IOMapForModeler

open Engine.Core
open System.ComponentModel

module HwTagModule = 

 
    type HwTag<'T>(baseTag: ExpressionForwardDeclModule.ITag<'T>, ioType, memoryName, deviceType, dataType:DataType, index) as this=
        interface IHwTag with
            member _.IOType = ioType
            member _.DeviceType = deviceType
            member _.DataType = dataType
            member _.Index = index
            member _.Name = baseTag.Name
            member _.Address = baseTag.Address
            member _.MemoryName = memoryName
            member _.Value 
                with get() = box baseTag.BoxedValue 
                and set v = 
                    baseTag.BoxedValue <- v
                    HwTagWriteModule.WriteAction(this:> IHwTag);

            member _.GetTarget() = baseTag.Target.Value
            member _.GetDeviceAddress() = @$"{memoryName}{baseTag.BoxedValue.GetType().Name}{index}"  //ex DW123 , IB6235, IX123  디바이스 타입은 hw maker별로 자유롭게
            member _.GetTagAddress() = baseTag.Address
        member x.IOType with get() = ioType 
        