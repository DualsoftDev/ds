namespace Engine.Core

open System
open System.Runtime.CompilerServices
open System.Text.RegularExpressions

[<AutoOpen>]
module CodeElements =
    (*
      [variables] = { //이름 = (타입,초기값)
        R100 = (word, 0)
      }
     *)
    /// Variable Declaration: name = (type, init)  CodeBlock 사용 ? 선택 필요 
    type VariableData(name:string, varType:DataType, initValue:string) =
        member _.Name = name
        member _.Type = varType
        member _.InitValue = initValue

        member _.ToDsText() = $"{name} = ({varType.ToText()}, {initValue})"

    let getFunctions (text:string) = 
        if not <| text.StartsWith "$" 
        then failwith "command & observe text start keyword is '$' ex)$mov 100 R100"
        text.Split('$')
        |> Seq.tail
        |> Seq.map(fun line -> 
            let line = line.Split(';')[0]  //줄바꿈 제거
            //function Name
            line.Split(' ')    |> Seq.head  
            //function Parameters
            , (line.Split(' ') |> Seq.tail |> Seq.toArray )
            ) 
        
    type Parameters = string[]
    [<AbstractClass>]
    type Func(name:string, parameters:Parameters) =
        member x.Name = name.ToLower() //명령어 이름은 소문자로만 허용
        member x.Parameters = parameters
        member x.ToDsText() =  $"""${x.Name} {String.Join(" ", parameters)}"""   
  
    //Job, ButtonDef, LampDef 에서 사용중  //todo ToDsText, parsing  
    //  [jobs] = {
    //    Ap = { A1."+"(%I1, %Q1); A2."+"(%I22, %Q22); A3."+"(%I33, %Q33); }
    //    Am = { A."-"(%I2, %Q2); } (Observes?, Commands? 삽입 규칙 필요) 
    //    Bp = { B."+"(%I3, %Q3); }
    //    Bm = { B."-"(%I4, %Q4); }
    //}
    //  [emg] = {
    //    STOP(%I1, %Q1) = { F; } (Observes?, Commands? 삽입 규칙 필요) 
    //    STOP2(%I1, %Q1) = { F2; }
    //}
    //  [emglamp] = {
    //    EmgMode(%Q1) = { F3 } ( Commands? 삽입 규칙 필요) ) 
    //}
    type Command(name:string, parameters:Parameters) =
        inherit Func(name, parameters)

    //Job, ButtonDef, LampDef 에서 사용중  //todo ToDsText, parsing  
    type Observe(name:string, parameters:Parameters) =
        inherit Func(name, parameters)
      
    [<Extension>]
    type SystemExt =
        [<Extension>] static member GetDelayTime (x:Observe) = 
                        let presetTime = (x.Parameters |> Seq.head ).ToLower()
                        let timetype = Regex.Replace(presetTime, @"\d", "");//문자 추출
                        let preset   = Regex.Replace(presetTime, @"\D", "");//숫자 추출
                        
                        match timetype with
                        | "ms"| "msec"-> preset|> CountUnitType.Parse
                        | "s" | "sec" -> let presetMsec = ((preset |> Convert.ToInt32) * 1000)
                                         presetMsec.ToString() |> CountUnitType.Parse

                        | _-> failwith "timer foramt Error"
                        

        [<Extension>] static member GetRingCount (x:Observe) = 
                        x.Parameters |> Seq.head |> CountUnitType.Parse
