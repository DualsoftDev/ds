// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office

open System
open System.Linq
open System.Runtime.CompilerServices

open DocumentFormat.OpenXml
open System.Collections.Concurrent
open Dual.Common.Core.FS

module WarnID =
    let _1 = 1, "Warnning message1"
    let _2 = 1, "Warnning message2"

module ErrID =
    // PPT Error (1 ~ 999)
    let _1 = "사용불가 도형입니다."
    let _2 = "중복된 Page 이름이 존재합니다."
    let _3 = "사용불가 연결 방식 입니다."
    let _4 = "선이 도형에 연결이 안되었습니다."
    let _5 = "선이 시작쪽 도형에 연결이 안되었습니다."
    let _6 = "선이 종료쪽 도형에 연결이 안되었습니다."
    let _7 = "Loading 시스템 정의는 시스템 새 이름이 필요합니다. ex) path[loadedName]"
    let _8 = "StartReset edge는 한쪽이 둥근화살표 입니다."
    let _9 = "양방향 edge 끝 화살표는 하나 이상 입니다"
    let _10 = "Interlock edge는  점선만 가능합니다"
    let _11 = "인터페이스 인과는 약 리셋이 불가능 합니다."
    let _12 = "제목 슬라이드가 없습니다. 제목 슬라이드를 생성하여 시스템 이름을 정하세요"
    let _13 = "도형의 이름이 없습니다."
    let _14 = "edge 연결가능도형 아님"
    let _15 = "edge not connected 시작 화살표 연결필요"
    let _16 = "edge not connected 끝   화살표 연결필요"
    let _17 = "Real의 이름이 중복입니다."
    let _18 = "이름에 ';' 사용은 안됩니다."
    let _19 = "Flow.Real 표기외에 이름에 '.' 사용은 안됩니다."
    let _20 = "Flow 이름 (페이지 제목)에 '.' 사용은 안됩니다."
    let _21 = "동일한 이름의 페이지 타이틀이 존재합니다."
    let _22 = "중복된 화살표 연결이 존재합니다."
    let _23 = "1 개의 Real만 그룹지정이 되어야 합니다."
    let _24 = "그룹내 Real 타입 하나는 존재 해야합니다."
    let _25 = "중복된 인터페이스 이름이 있습니다."
    let _26 = "해당이름의 Flow가 없습니다."
    let _27 = "해당이름의 Real이 없습니다."
    let _28 = "Safety 이름이 시스템 내부에 존재하지 않습니다."
    let _29 = "Loading 파일경로에 파일이 없습니다."
    let _30 = "버튼 타입은 출력값은 입력 불가입니다. [0, N] 수량을 사용하세요"
    let _31 = "시스템 이름과 Flow이름이 같으면 안됩니다."
    let _32 = "인터페이스 Link는 인과정보가 필요없습니다."
    let _33 = "페이지 내에 동일한 내용을 Copy하였습니다."
    let _34 = "외부 시스템에 동일한 Copy 시스템을 만들려 했습니다."
    let _35 = "외부 시스템 인터페이스 이름만 존재 합니다. '인터페이스이름[tx1 ~ rx1,rx2]' 와 같은 형식으로 입력 해야합니다."
    let _36 = "행위이름이 EXCEL 이름 열에 없습니다."
    let _37 = "인터페이스는 인터페이스끼리 인과가능."
    let _38 = "도형에 윤곽선이 없습니다."
    let _39 = "모델링에 사용불가 도형입니다."
    let _40 = "연결선은 반드시 색상이 있어야 합니다."
    let _41 = "Api TXs~Rxs 이름의 Real행위가 없습니다."
    let _42 = "Api TXs~Rxs 이름이 소속된 Flow가 없습니다."

    let _43 = "Api 지시/관찰 구성은 '~' 으로 구분합니다. ex) ApiName[TXs ~ RXs]"
    let _44 = "파일 경로는 '/' 사용만 가능합니다. \\ 사용불가"
    let _45 = "System 인터페이스 순환관계가 존재합니다."
    let _46 = "CallDev 이름은 '.' 으로 구분되어야 합니다.(ex: systemA.Inferface3)"
    let _47 = "호출 Interface에 해당하는 대상 시스템이 없습니다."
    let _48 = "Loading 시스템이름과 Call 호출 이름이 다름니다."
    let _49 = "외부 시스템(Device)에 해당 Interface가 없습니다."
    let _50 = "다른 CPU는 Copy아닌 Open으로 로딩해야합니다."
    let _51 = "외부 시스템(CPU) 호출은 Link Task만 호출가능 합니다."
    let _52 = "외부 시스템(Device) 호출은 Device Task만 호출가능 합니다."
    let _53 = "Api TXs~Rxs 이름을 정의 해야합니다. ex) ApiName[tx1;tx2~rx1]"
    let _54 = "다른 Flow의 Work 정의는 '.' 기호로 나타냅니다. ex) Flow2.Work3"
    let _55 = "외부 시스템(CPU)은  호출은 '$' 기호로 나타냅니다. ex) System(CPU)$SystemApiName"
    let _56 = "외부 시스템(Device) 호출은 '$'  또는 '.' 기호로 나타냅니다. ex) System(Device).DeviceApiName"
    let _57 = "PPT 파일이름 공백발견, 다른이름저장이 필요합니다."
    let _58 = "System 이름 시작은 특수문자 및 숫자는 불가능합니다."
    let _59 = "페이지에 제목 이름이 없습니다."

    // IO Mapping Error (1001 ~ )
    let _1001 = "시스템에 버튼 이름이 없습니다."
    let _1002 = "시스템에 램프 이름이 없습니다."
    let _1003 = "해당 시스템 이름이 엑셀 Sheet에 없습니다."
    let _1004 = "Job 이 해당 시스템에 없습니다."
    let _1005 = "시스템 램프 or 버튼 or 조건은 Not($n) 함수만 사용가능합니다."
    let _1006 = "Device가 해당 시스템에 없습니다."

    //todo
       // "리얼 행위의 자식은 한곳에만 정의가능합니다.(alias, exFlow 정의불가)"
    //

[<AutoOpen>]
module MessgePPTError =

    type ErrorCase  = Shape | Conn | Page | Group | Name | Path
        with
        member x.ToText() =
            match x with
            |Shape -> "도형오류"
            |Conn  -> "연결오류"
            |Page  -> "장표오류"
            |Group -> "그룹오류"
            |Name  -> "이름오류"
            |Path  -> "경로오류"

  
    [<Extension>]
    type Office =

        [<Extension>]
        static member ErrorPPT(case:ErrorCase, msg:string,  objName:string, page:int, ?userMsg:string) =
              
           
            let itemName =  if(userMsg.IsSome && (userMsg.Value = ""|>not))
                            then $"[Page{page}:{objName} ({userMsg.Value})"
                            else $"[Page{page}:{objName}"
            failwithf  $"[{case.ToText()}] {msg} \t{itemName}"

