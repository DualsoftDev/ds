namespace Dsu.PLCConverter.FS

open Dsu.PLCConverter.FS.XgiSpecs.Config.POU.Program.LDRoutine
open System.Linq
open XgiBaseXML
open Dsu.PLCConverter.FS.XgiSpecs
open Dsu.PLCConverter.FS.ActivePattern
[<AutoOpen>]
module XgiFBInfo =

    // FB 정보에 대한 타입 정의
    type FBInfo = {
        FindName: string
        FbName: string
        Args: List<string>
        MelsecArgs: List<string>
        DataType: CheckType
    }

    // FB 정보를 반환하는 함수
    let getFBInfo (func: string, args: seq<string>, oriArg:string) : FBInfo = 
        let func, org = func.Split(';')[0], func.Split(':')[0]
        let dataType =
                if args.any(isZDevice)
                then 
                    XGI.ZDeviceCheckType
                elif func.Split(':').Length > 1 
                then parseCheckType (func.Split(':')[1]) 
                else CheckType.NONE

        let doubleArgs (dataType: CheckType) (args: seq<string>) = 
            newDoubleArgs dataType args org

        let dataTypeText = dataType.ToString()
        let printNomal newCmp = (sprintf "%s_%s" newCmp dataTypeText), newCmp, if args.length() = 2 then doubleArgs dataType (getAddLast args) else doubleArgs dataType args
        let printNewType newCmp = (sprintf "%s2_%s" newCmp dataTypeText), newCmp, if args.length() = 2 then doubleArgs dataType (getAddLast args) else doubleArgs dataType args

        let findName, fbName, newArgs =
            match org with
            | RegexPattern @"(^BCD_TO_\*\*\*$)" [newCmp] -> sprintf "%s_BCD_TO_INT" dataTypeText, newCmp, doubleArgs dataType args
            | RegexPattern @"(^\*\*\*_TO_BCD$)" [newCmp] -> 
                if func.Split(':').[1] = "DINT" 
                then "DINT_TO_BCD_DWORD", newCmp, doubleArgs dataType args
                else "INT_TO_BCD_WORD", newCmp, doubleArgs dataType args
            | RegexPattern @"(^LIFO_\*\*\*$)" [newCmp] -> sprintf "LIFO_%s" dataTypeText, newCmp, doubleArgs dataType args
            | RegexPattern @"(^FIFO_\*\*\*$)" [newCmp] -> sprintf "FIFO_%s" dataTypeText, newCmp, doubleArgs dataType args
            | RegexPattern @"(^MOVE$)" [newCmp]
            | RegexPattern @"(^NOT$)" [newCmp]
            | RegexPattern @"(^INC$)" [newCmp]
            | RegexPattern @"(^DEC$)" [newCmp]  -> sprintf "%s_%s" newCmp dataTypeText, newCmp, doubleArgs dataType args
            | RegexPattern @"(^DECO$)" [newCmp] -> printNomal newCmp
            | RegexPattern @"(^ENCO$)" [newCmp] -> printNomal newCmp
            | RegexPattern @"(^GET$)" [newCmp]  -> printNomal newCmp
            | RegexPattern @"(^PUT$)" [newCmp]  -> printNomal newCmp
            | RegexPattern @"(^DIV$)" [newCmp]  -> printNomal newCmp
            | RegexPattern @"(^SUB$)" [newCmp]  -> printNomal newCmp
            | RegexPattern @"(^AND$)" [newCmp]  -> printNewType newCmp
            | RegexPattern @"(^OR$)" [newCmp]   -> printNewType newCmp
            | RegexPattern @"(^XOR$)" [newCmp]  -> printNewType newCmp
            | RegexPattern @"(^XNR$)" [newCmp]  -> printNewType newCmp
            | RegexPattern @"(^MUL$)" [newCmp]  -> printNewType newCmp
            | RegexPattern @"(^ADD$)" [newCmp]  -> printNewType newCmp
            | _ -> org, org, getNewArgs args org

        // FBInfo 타입으로 반환
        { FindName = findName; FbName = fbName; Args = newArgs|>Seq.toList; MelsecArgs = oriArg.Split(';') |>Seq.toList; DataType = dataType }
