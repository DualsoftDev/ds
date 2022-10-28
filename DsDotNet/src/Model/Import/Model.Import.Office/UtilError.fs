// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System
open System.Linq
open System.Runtime.CompilerServices

open DocumentFormat.OpenXml
open System.Collections.Concurrent

[<AutoOpen>]
module UtilError = 

    type ErrorCase  = Shape | Conn | Page | Group | Name 
        with
        member x.ToText() =
            match x with
            |Shape -> "도형오류"
            |Conn  -> "연결오류"
            |Page  -> "장표오류"
            |Group -> "그룹오류"
            |Name  -> "이름오류"

    let DicErr = 
        let msgs = ConcurrentDictionary<int, string>()
        msgs.TryAdd(1,"Call에 점선은 지정된 모양이 아닙니다.") |>ignore
        msgs.TryAdd(2,"이름 마지막에 [#;#] 형식은 하나 이상 양의 정수이어야 합니다.") |>ignore
        msgs.TryAdd(3,"기능이 없는 연결입니다.") |>ignore
        msgs.TryAdd(4,"edge not connected") |>ignore
        msgs.TryAdd(5,"start not connected") |>ignore
        msgs.TryAdd(6,"end not connected") |>ignore
        msgs.TryAdd(7,"there is no edge direction ") |>ignore
        msgs.TryAdd(8,"SReset edge는 한쪽이 둥근화살표 입니다.") |>ignore
        msgs.TryAdd(9,"양방향 edge 끝 화살표는 하나 이상 입니다") |>ignore
        msgs.TryAdd(10,"Interlock edge는  점선만 가능합니다") |>ignore
        msgs.TryAdd(11,"sp") |>ignore
        msgs.TryAdd(12,"children은 call or exReal만 가능합니다.") |>ignore
        msgs.TryAdd(13,"도형의 이름이 없거나 Dummy 그룹은 점선 원형입니다.") |>ignore
        msgs.TryAdd(14,"edge 연결가능도형 아님") |>ignore
        msgs.TryAdd(15,"edge not connected 시작 화살표 연결필요") |>ignore
        msgs.TryAdd(16,"edge not connected 끝   화살표 연결필요") |>ignore
        msgs.TryAdd(17,"Real의 내부자식은 한번만 정의 되어야 합니다.") |>ignore
        msgs.TryAdd(18,"Dummy 그룹은 사각형 내부에서만 사용가능합니다.") |>ignore
        msgs.TryAdd(19,"Dummy 그룹은 중복된 자식을 가질 수 없습니다.") |>ignore
        msgs.TryAdd(20,"중복된 화살표 연결이 존재합니다.") |>ignore
        msgs.TryAdd(21,"동일한 이름의 페이지 타이틀이 존재합니다.") |>ignore
        msgs.TryAdd(22,"이름 마지막에 [#,#] 형식으로 입력 ex) name[TX개수, RX개수] ") |>ignore
        msgs.TryAdd(23,"1 개의 myReal 그룹지정이 되어야 합니다.") |>ignore
        msgs.TryAdd(24,"1 개의 dummy 그룹지정이 되어야 합니다.") |>ignore
        msgs.TryAdd(25,"그룹내 dummy 타입 또는 real 타입 하나는 존재 해야합니다.") |>ignore
        msgs.TryAdd(26,"이름에 MFlow경로 설정 '.' 기호는 1개 이여야 합니다. ex) MFlow.real") |>ignore
        msgs.TryAdd(27,"해당이름의 MFlow가 없거나 이름에 ';' 사용은 안됩니다. (이름에 ';' 제거 혹은 다른 페이지 타이틀 이름 확인필요)") |>ignore
        msgs.TryAdd(28,"Safety 이름이 시스템 내부에 존재하지 않습니다.") |>ignore
        msgs.TryAdd(29,"이름에 '.' or ';' 사용은 안됩니다.") |>ignore
        msgs.TryAdd(30,"버튼 타입은 출력값은 입력 불가입니다. [0, N] 수량을 사용하세요") |>ignore
        msgs.TryAdd(31,"시스템 이름과 Flow이름이 같으면 안됩니다.") |>ignore
        msgs.TryAdd(32,"외부 시스템이 정의되지 않았습니다.") |>ignore
        msgs.TryAdd(33,"외부 시스템에 해당 인터페이스가 없습니다.") |>ignore
        msgs.TryAdd(34,"외부 시스템에 동일한 Copy 시스템을 만들려 했습니다.") |>ignore
        msgs.TryAdd(35,"외부 시스템 인터페이스 이름만 존재 합니다. '인터페이스이름[tx1 ~ rx1,rx2]' 와 같은형식으로 입력 해야합니다.") |>ignore
        msgs.TryAdd(36,"행위이름이 EXCEL 이름 열에 없습니다.") |>ignore
        msgs.TryAdd(37,"인터페이스는 인터페이스끼리 인과가능.") |>ignore
        msgs.TryAdd(38,"도형에 윤곽선이 없습니다.") |>ignore
        msgs.TryAdd(39,"") |>ignore
       
        msgs
        
    [<Extension>]
    type Office =
            
        [<Extension>]
        static member ErrorPPT(case:ErrorCase, id:int,  objName:string, page:int, ?userName:string) = 
            let itemName =  if(userName.IsSome && (userName.Value = ""|>not))
                            then $"[Page{page}:{objName}({userName.Value})" 
                            else $"[Page{page}:{objName}" 
            failwithf  $"[{case.ToText()}] {DicErr.[id]} \t\t\t{itemName}]"

        