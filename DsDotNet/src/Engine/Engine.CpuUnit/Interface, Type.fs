[<AutoOpen>]
module rec Engine.Cpu.Interface

open System.Collections.Concurrent
open System.Text.Json


    //22.11.29 일 기준 지원 Data type = { bool, int, byte, single, double, string }
    type IData      = interface end

    type ITag    =
        abstract Name   : string
        abstract Value  : obj with get,set
        abstract ToText   : unit -> string


    //json union 타입이 지원 안되서 레코드 규격 추가
    //f#.Json nuget이 있지만 System.Text.Json 사용
    let jsonOptions =  JsonSerializerOptions ( WriteIndented = true, IncludeFields  = false)
    type TerminalJson =
      {
        TagType : string   //None, PcTag, PlcTag, DsMemory (None:bool, int,..)
        Type    : string   //bool, int, string
        Name    : string   //None or TagName
        Value   : string   //value
      }

    type ExpressionJson =
      {
        Case        : string              //ConstValue  | Variable             | Function
        Type        : string              //bool, int,..| bool, int,..         | +, +D, *, <
        Terminal    : TerminalJson        //TerminalJson| TerminalJson         | None
        Items : ExpressionJson seq       // [ ExpressionJsons ]
      }

    type StatementJson =
      {
        Expresstion : ExpressionJson
        Target      : TerminalJson
      }

    let toTerminalJson(x:'T) = {
            TagType = "None"
            Type = x.GetType().Name
            Name = "None"
            Value = x.ToString()
        }

    let toTerminalTagJson(x:ITag) = {
            TagType = $"{x.GetType().Name.Split('`')[0]}"
            Type =  x.Value.GetType().Name
            Name = x.Name
            Value = x.Value.ToString()
        }

    let terminalEmpty() = {
            TagType = "None"
            Type   =  "None"
            Name   =  "None"
            Value   = "None"
        }

    let constJson(x:'T) =  {
            Case  = "ConstValue"
            Type  = x.GetType().Name
            Terminal = toTerminalJson(x)
            Items = []
        }

    let variableJson(tag:ITag)  = {
            Case ="Variable"
            Type = $"{tag.GetType().Name.Split('`')[0]}"
            Terminal = toTerminalTagJson(tag)
            Items = []
        }
