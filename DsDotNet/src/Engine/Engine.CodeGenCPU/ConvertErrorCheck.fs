namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Linq
open System.Collections.Generic

[<AutoOpen>]
module ConvertErrorCheck =
    
    let checkNullAddressErr(sys:DsSystem) = 
        let nullTagJobs = sys.Jobs
                             .Where(fun j-> j.DeviceDefs.Where(fun f-> 
                                            f.InTag.IsNull() && f.ApiItem.RXs.any()
                                            ||f.OutTag.IsNull() && f.ApiItem.TXs.any()
                                            ).any())
                      
        if nullTagJobs.any()
        then 
            let errJobs = StringExt.JoinWith(nullTagJobs.Select(fun j -> j.Name), "\n")
            failwithlogf $"Device 주소가 없습니다. \n{errJobs}"
        let nullBtns = sys.HWButtons.Where(fun b-> 
                                    b.InTag.IsNull() 
                                    ||b.OutTag.IsNull() && b.OutAddress <> TextSkip)
        if nullBtns.any()
        then 
            let errBtns = StringExt.JoinWith(
                            nullBtns.Select(fun b -> 
                                let inout = if b.InTag.IsNull() then "입력" else "출력"
                                $"{b.ButtonType} 해당 {inout} 주소가 없습니다. [{b.Name}]"), "\n")

            failwithlogf $"버튼 주소가 없습니다. \n{errBtns}"
                                      
        let nullLamps = sys.HWLamps.Where(fun b-> b.OutTag.IsNull())
        if nullLamps.any()
        then 
            let errLamps= StringExt.JoinWith(nullLamps.Select(fun j -> j.Name), "\n")
            failwithlogf $"램프 주소가 없습니다. \n{errLamps}"
             
    