namespace Engine.Cpu

[<AutoOpen>]
module Memory =

    type Bit(name) = 
        member x.Name = name
