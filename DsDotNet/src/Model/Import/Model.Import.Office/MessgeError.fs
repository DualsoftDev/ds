// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System
open System.Linq
open System.Runtime.CompilerServices

open DocumentFormat.OpenXml
open System.Collections.Concurrent
open Engine.Common.FS

module WarnID =
    let _1 = 1, "Warnning message1"
    let _2 = 1, "Warnning message2"

module ErrID =
    let _1 = "Call에 점선은 지정된 모양이 아닙니다."
    let _2 = "이름 마지막에 [#;#] 형식은 하나 이상 양의 정수이어야 합니다."
    let _3 = "기능이 없는 연결입니다."
    let _4 = "edge not connected"
    let _5 = "start not connected"
    let _6 = "end not connected"
    let _7 = "there is no edge direction "
    let _8 = "SReset edge는 한쪽이 둥근화살표 입니다."
    let _9 = "양방향 edge 끝 화살표는 하나 이상 입니다"
    let _10 = "Interlock edge는  점선만 가능합니다"
    let _11 = "인터페이스 인과는 약 리셋 불가"
    let _12 = "children은 call or exReal만 가능합니다."
    let _13 = "도형의 이름이 없거나 Dummy 그룹은 점선 원형입니다."
    let _14 = "edge 연결가능도형 아님"
    let _15 = "edge not connected 시작 화살표 연결필요"
    let _16 = "edge not connected 끝   화살표 연결필요"
    let _17 = "Real의 내부자식은 한번만 정의 되어야 합니다."
    let _18 = "Dummy 그룹은 사각형 내부에서만 사용가능합니다."
    let _19 = "Dummy 그룹은 중복된 자식을 가질 수 없습니다."
    let _20 = "중복된 화살표 연결이 존재합니다."
    let _21 = "동일한 이름의 페이지 타이틀이 존재합니다."
    let _22 = "이름 마지막에 [#,#] 형식으로 입력 ex) name[TX개수, RX개수] "
    let _23 = "1 개의 myReal 그룹지정이 되어야 합니다."
    let _24 = "1 개의 dummy 그룹지정이 되어야 합니다."
    let _25 = "그룹내 dummy 타입 또는 real 타입 하나는 존재 해야합니다."
    let _26 = "이름에 MFlow경로 설정 '.' 기호는 1개 이여야 합니다. ex) MFlow.real"
    let _27 = "해당이름의 MFlow가 없거나 이름에 ';' 사용은 안됩니다. (이름에 ';' 제거 혹은 다른 페이지 타이틀 이름 확인필요)"
    let _28 = "Safety 이름이 시스템 내부에 존재하지 않습니다."
    let _29 = "이름에 '.' or ';' 사용은 안됩니다."
    let _30 = "버튼 타입은 출력값은 입력 불가입니다. [0, N] 수량을 사용하세요"
    let _31 = "시스템 이름과 Flow이름이 같으면 안됩니다."
    let _32 = "외부 시스템이 정의되지 않았습니다."
    let _33 = "외부 시스템에 해당 인터페이스가 없습니다."
    let _34 = "외부 시스템에 동일한 Copy 시스템을 만들려 했습니다."
    let _35 = "외부 시스템 인터페이스 이름만 존재 합니다. '인터페이스이름[tx1 ~ rx1,rx2]' 와 같은형식으로 입력 해야합니다."
    let _36 = "행위이름이 EXCEL 이름 열에 없습니다."
    let _37 = "인터페이스는 인터페이스끼리 인과가능."
    let _38 = "도형에 윤곽선이 없습니다."
    let _39 = "모델링에 사용불가 도형입니다."
    let _40 = "연결선은 반드시 색상이 있어야 합니다."

    

    //todo
       // "리얼 행위의 자식은 한곳에만 정의가능합니다.(alias, exFlow 정의불가)"
    //
        
[<AutoOpen>]
module MessgeError = 

    type ErrorCase  = Shape | Conn | Page | Group | Name 
        with
        member x.ToText() =
            match x with
            |Shape -> "도형오류"
            |Conn  -> "연결오류"
            |Page  -> "장표오류"
            |Group -> "그룹오류"
            |Name  -> "이름오류"



    [<Extension>]
    type Office =
            
        [<Extension>]
        static member ErrorPPT(case:ErrorCase, msg:string,  objName:string, page:int, ?userMsg:string) = 
            let itemName =  if(userMsg.IsSome && (userMsg.Value = ""|>not))
                            then $"[Page{page}:{objName}({userMsg.Value})" 
                            else $"[Page{page}:{objName}" 
            failwithf  $"[{case.ToText()}] {msg} \t\t\t{itemName}]"

        