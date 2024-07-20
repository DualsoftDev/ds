// Copyright (c) Dualsoft  All Rights Reserved.
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
    let _8 = "전제조건은 시작 화살표에만 존재합니다."
    let _9 = "양방향 edge 끝 화살표는 하나 이상 입니다"
    let _10 = "전제조건 이름이 시스템 내부에 존재하지 않습니다."
    let _11 = "인터페이스 인과는 강리셋(예약기능)이 불가능 합니다."
    let _12 = "제목 슬라이드가 없습니다. 제목 슬라이드를 생성하여 시스템 이름을 정하세요"
    let _13 = "해당도형에 이름을 입력해야합니다."
    let _14 = "edge 연결가능도형 아님"
    let _15 = "edge not connected 시작 화살표 연결필요"
    let _16 = "edge not connected 끝   화살표 연결필요"
    let _17 = "Work의 이름이 중복입니다."
    let _18 = "이름에 ';' 사용은 안됩니다."
    let _19 = "복수그룹명령은 Action 이름 끝에 [숫자] 형식으로 입력합니다."
    let _20 = "Spare"
    let _21 = "동일한 이름의 페이지 타이틀이 존재합니다."
    let _22 = "중복된 화살표 연결이 존재합니다."
    let _23 = "1 개의 Work만 그룹지정이 되어야 합니다."
    let _24 = "그룹내 Work 타입 하나는 존재 해야합니다."
    let _25 = "중복된 인터페이스 이름이 있습니다."
    let _26 = "해당이름의 Flow가 없습니다."
    let _27 = "해당이름의 Work이 없습니다(공란확인)"
    let _28 = "Spare"
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
    let _41 = "Api TXs~Rxs 이름의 Work행위가 없습니다."
    let _42 = "Api TXs~Rxs 이름이 소속된 Flow가 없습니다."

    let _43 = "Api 지시/관찰 구성은 '~' 으로 구분합니다. ex) ApiName[TXs ~ RXs]"
    let _44 = "Action은 Work안에서 시작 가능합니다. Work내 그룹화 필요합니다."
    let _45 = "System 인터페이스 순환관계가 존재합니다."
    let _46 = "ActionDev 이름은 '.' 으로 구분되어야 합니다.(ex: systemA.Inferface3)"
    let _47 = "호출 Interface에 해당하는 대상 시스템이 없습니다."
    let _48 = "Action디바이스 이름이 Loading 시스템에 없습니다."
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
    let _60 = "적용시킬 Flow가 없습니다. Flow장표를 추가하세요"
    let _61 = "Layout 이름에 해당 디바이스가 없습니다."
    let _62 = "Layout 페이지에만 정의 가능, Utils 메뉴에 Add Layout 실행하세요."
    let _63 = "Layout 페이지에 [Path], [Layout] Textbox가 없습니다."
    let _64 = "중복된 Lamp 이름이 존재합니다."
    let _65 = "Layout slide에 배경 이미지가 없습니다."
    let _66 = "중복된 Layout 이름이 존재합니다."
    let _67 = "중복된 Condition 이름이 존재합니다."
    let _68 = "Safety조건으로는 Action만 가능합니다. Dev.Api 형식으로 입력하세요"
    let _69 = "처음페이지 슬라이드 노트에 Text가 없습니다."
    let _70 = "내부 함수 호출은 이름만 입력해야합니다."
    let _71 = "Flow에 존재하는 Action은 반드시 연결이 필요합니다."
    let _72 = "Multi Device 각 API호출 갯수는 같아야 합니다."
    let _73 = "Action 이름에는 영역 구분자 속성 '.'은 두개 이하 입니다."
    let _74 = "Safety 정의는 Device.Api 또는 Flow.Device.Api 형식으로 입력해야 합니다."
    let _75 = "Table Device 이름규격은 dev.Api 입니다."
    let _76 = "Work 설정시간이 중복 정의 되었습니다."
 
    let _78 = "전제조건은 자신을 조건으로 사용불가 입니다."
    let _79 = "Safety는 Action만 조건으로 가능합니다."
    let _80 = "Safety  이름이 시스템 내부에 존재하지 않습니다."
    let _81 = "Safety  는 자신을 조건으로 사용불가 입니다."
    let _82 = "AutoPre 이름이 시스템 내부에 존재하지 않습니다."
    let _83 = "AutoPre 는 자신을 조건으로 사용불가 입니다."

    // IO Mapping Error (1001 ~ )
    let _1001 = "시스템에 버튼 이름이 없습니다."
    let _1002 = "시스템에 램프 이름이 없습니다."
    let _1003 = "해당 시스템 이름이 엑셀 Sheet에 없습니다."
    let _1004 = "테이블 Colum 갯수가 맞지 않습니다. 새로 생성이 필요합니다."
    let _1005 = "시스템 램프 or 버튼 or 조건은 Not($n) 함수만 사용가능합니다."
    let _1006 = "Device가 해당 시스템에 없습니다."
    let _1007 = "시스템에 Condition 이름이 없습니다."
    let _1008 = "Function 적용은 1개만 가능합니다."
    let _1009 = "그룹 Action Function 적용은 처음 Device 1개에만 적용해서 그룹에 전체 반영됩니다."
    let _1010 = "함수에 대한 내용이 없습니다. \r\n ex) $D100 = 55;"

//todo
// "리얼 행위의 자식은 한곳에만 정의가능합니다.(alias, exFlow 정의불가)"
//

[<AutoOpen>]
module MessgePPT =

    type ErrorCase =
        | Shape
        | Conn
        | Page
        | Group
        | Name
        | Path

        member x.ToText() =
            match x with
            | Shape -> "도형오류"
            | Conn -> "연결오류"
            | Page -> "장표오류"
            | Group -> "그룹오류"
            | Name -> "이름오류"
            | Path -> "경로오류"


    ///file(Item1), page(Item2), objID(Item3), msg(Item4)
    let ErrorPPTNotify = new Event<string * int * uint * string>()
    let LoadingPPTNotify = new Event<string>()
    let ErrorNotify = "ERROR "

    [<Extension>]
    type Office =

        [<Extension>]
        static member ErrorPPT
            (
                case: ErrorCase,
                msg: string,
                objName: string,
                page: int,
                objID: uint,
                ?userMsg: string
            ) =

            let itemName =
                if (userMsg.IsSome && (userMsg.Value = "" |> not)) then
                    $"◆페이지{page} {objName}({userMsg.Value})"
                else
                    $"◆페이지{page} {objName}"

            ErrorPPTNotify.Trigger(currentFileName, page, objID, itemName)
            failwithf $"[{case.ToText()}] {msg} \t\t{itemName} {ErrorNotify}"
