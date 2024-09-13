namespace Engine.Import.Office

open System.Collections.Concurrent
open System
open Engine.Core
open System.Collections.Generic

[<AutoOpen>]
module Util =

    /// ConcurrentDictionary 를 이용한 hash
    type ConcurrentHash<'T>() =
        inherit ConcurrentDictionary<'T, 'T>()
        member x.TryAdd(item: 'T) = x.TryAdd(item, item)

    let trimSpace (text: string) = text.Trim()
    let trimNewLine (text: string) = text.Trim('\n').Trim('\r')

    let mutable activeSys:DsSystem option= None
    let mutable activeSysDir = ""
    let mutable currentFileName = ""

    let Copylibrary = HashSet<string>() //Copylibrary 저장


    let GetTailNumber (name: string) =
        let pattern = "\d+$" // 글자 마지막 숫자를 찾음
        let matches = System.Text.RegularExpressions.Regex.Matches(name, pattern)

        if matches.Count > 0 then
            let name = System.Text.RegularExpressions.Regex.Replace(name, pattern, "")
            let number = matches.[matches.Count - 1].Value |> trimSpace |> Convert.ToInt32
            name, number
        else
            name, 0


    let getBtnType (key: string) =
        match key.Trim().ToUpper() with
        | "A" -> BtnType.DuAutoBTN
        | "M" -> BtnType.DuManualBTN
        | "D" -> BtnType.DuDriveBTN
        | "P" -> BtnType.DuPauseBTN
        | "E" -> BtnType.DuEmergencyBTN
        | "T" -> BtnType.DuTestBTN
        | "R" -> BtnType.DuReadyBTN
        | "H" -> BtnType.DuHomeBTN
        | "C" -> BtnType.DuClearBTN
        | _ -> if key = ""
                then failwith $"버튼은 [타입]이름 형식으로 작성해야 합니다.\nEx)[P]PauseStation1"
                else failwith $"{key}은 버튼 타입이 아님니다. 가능타입 리스트
                        \n[A]AutoBTN
                        \n[M]ManualBTN
                        \n[D]DriveBTN
                        \n[P]PauseBTN
                        \n[E]EmergencyBTN
                        \n[T]TestBTN
                        \n[R]ReadyBTN
                        \n[H]HomeBTN
                        \n[C]ClearBTN"
        
                

    let getLampType (key: string) =
        match key.Trim().ToUpper() with
        | "A" -> LampType.DuAutoModeLamp
        | "M" -> LampType.DuManualModeLamp
        | "D" -> LampType.DuDriveStateLamp
        | "E" -> LampType.DuErrorStateLamp
        | "T" -> LampType.DuTestDriveStateLamp
        | "R" -> LampType.DuReadyStateLamp
        | "I" -> LampType.DuIdleModeLamp
        | "O" -> LampType.DuOriginStateLamp
        | _ -> if key = ""
                then failwith $"램프는 '[타입]이름' 형식으로 작성해야 합니다.\nEx)[S]stopErrStation1~3"
                else failwith $"{key}은 램프 타입이 아님니다. 가능타입 리스트
                        \n[A]AutoLamp
                        \n[M]ManualLamp
                        \n[D]DriveLamp
                        \n[E]ErrorLamp
                        \n[T]TestDriveLamp
                        \n[R]ReadyLamp
                        \n[O]OriginLamp
                        \n[I]IdleLamp"

    let tryGetConditionType (key: string) =
        match key.Trim().ToUpper() with
        | "R" -> Some ConditionType.DuReadyState 
        | "D" -> Some ConditionType.DuDriveState
        | _ -> None

    let tryGetActionType (key: string) =
        match key.Trim().ToUpper() with
        | "E" ->Some ActionType.DuEmergencyAction
        | "P" ->Some ActionType.DuPauseAction
        | _ -> None


