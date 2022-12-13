namespace Engine.CodeGenHMI

open System.Diagnostics
open System.Collections.Generic
open Engine.Core

[<AutoOpen>]
module CpuUnit =
  
      //----------------------
      //  Status   SP  RP  EP
      //----------------------
      //    R      x   -   x
      //           o   o   x
      //    G      o   x   x
      //    F      -   x   o
      //    H      -   o   o
      //----------------------
      //- 'o' : ON, 'x' : Off, '-' 는 don't care
      //- 내부에서 Reset First 로만 해석

      //- 실행/Resume 은 Child call status 보고 G 이거나 R 인 것부터 수행
    
    [<DebuggerDisplay("{name}")>]
    type Cpu(name:string, system:DsSystem)  =
        member x.Name = name
        member x.System = system
        member val Running = false with get, set
