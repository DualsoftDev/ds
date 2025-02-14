open System.Collections.Generic


// "F:\Git\dual\soft\Delta\Dual.Common.FS\Scripts" 에서의 상대 경로
//                         < .. >          <.>


//#I @"Z:\ds\Submodules\nuget\bin\net8.0"
//#r "Dual.Common.Core.dll"
//#r "Dual.Common.Core.FS.dll"

#r @"F:\Git\ds\Submodules\nuget\bin\net8.0\Dual.Common.Core.dll"
#r @"F:\Git\ds\Submodules\nuget\bin\net8.0\Dual.Common.Core.FS.dll"

//#r "nuget: Newtonsoft.Json"

open System
open System.IO
open Dual.Common
open Dual.Common.Core.FS


let none() = None
let some() = Some ()
let opt1 =
    option {
        //let! x = None
        do! some()
        //do! none()
        return 1
    }


