namespace Engine.Parser.FS

open Antlr4.Runtime

open Dual.Common.Core.FS
open Engine.Parser
open Engine.Core
open type Engine.Parser.dsParser

module Program =
    let CylinderText =
        """
[sys] Cylinder = {
    [flow] F = {
        Vp > Pp > Sp;
        Vm > Pm > Sm;

        Vp |> Pm |> Sp;
        Vm |> Pp |> Sm;
        Vp <|> Vm;
    }
    [interfaces] = {
        "+" = { F.Vp ~ F.Sp }
        "-" = { F.Vm ~ F.Sm }
        "+" <|> "-";
    }
}
"""

    let EveryScenarioText =
        """
[sys] DS_Units_V6 = {
    [flow] "시스템 모델링" = {
        "System B", "System A", "System B", "System A"; 
    }
    [flow] "모델링 기본 구성" = {
    }
    [flow] "모델링 확장 구성1" = {
        "System A"; 
    }
    [flow] "모델링 확장 구성2" = {
        "System A", Flow2, Flow1; 
    }
    [flow] "모델링 구조 Unit" = {
    }
    [flow] "기본 도형 Unit" = {
    }
    [flow] "1 작업 및 행위" = {
        공급작업 = {
            "1 작업 및 행위".RBT.투입, "1 작업 및 행위".RBT.홈; 
        }
        드릴작업; 
    }
    [flow] "1 작업 및 행위 유닛" = {
        "1 작업 및 행위 유닛".Device.Action1.INTrue > Work1;
        공급작업 = {
            "1 작업 및 행위 유닛".RBT.투입, "1 작업 및 행위 유닛".RBT.홈; 
        }
        드릴작업; 
    }
    [flow] "2 행위 (Action) 배치" = {
        #"2 행위 (Action) 배치_전원" > 드릴작업;
        공급작업 = {
            "2 행위 (Action) 배치".RBT.투입, "2 행위 (Action) 배치".RBT.홈; 
        }
    }
    [flow] "2 행위 (Action) 배치 유닛" = {
        "2 행위 (Action) 배치 유닛".Device.Action1.INTrue > Work1_1;
        #"2 행위 (Action) 배치 유닛_전원" > 드릴작업;
        Work1 = {
            "2 행위 (Action) 배치 유닛".Device.Action1, "2 행위 (Action) 배치 유닛".Device.Action2; 
        }
        공급작업 = {
            "2 행위 (Action) 배치 유닛".RBT.투입, "2 행위 (Action) 배치 유닛".RBT.홈; 
        }
        [aliases] = {
            Work1 = { Work1_1; }
        }
    }
    [flow] "3 작업 (Work) 타입" = {
        #"3 작업 (Work) 타입_전원" > 드릴작업;
        공급작업 = {
            "3 작업 (Work) 타입".RBT.투입, "3 작업 (Work) 타입".RBT.홈; 
        }
        Flow2, Flow1; 
    }
    [flow] "3 작업 (Work) 타입 유닛" = {
        #"3 작업 (Work) 타입 유닛_전원" > 드릴작업;
        공급작업 = {
            "3 작업 (Work) 타입 유닛".RBT.투입, "3 작업 (Work) 타입 유닛".RBT.홈; 
        }
        Work1, Flow2, Flow1, "3 작업 (Work) 타입.드릴작업"; 
    }
    [flow] "4 행위 (Action) 타입" = {
        #"4 행위 (Action) 타입_전원" > 드릴작업;
        공급작업 = {
            "4 행위 (Action) 타입".RBT.투입, "4 행위 (Action) 타입".RBT.홈; 
        }
    }
    [flow] "4 행위 (Action) 타입 유닛" = {
        #"4 행위 (Action) 타입 유닛_전원" > 드릴작업;
        #"4 행위 (Action) 타입 유닛_Action1" > 드릴작업1;
        공급작업 = {
            "4 행위 (Action) 타입 유닛".RBT.투입, "4 행위 (Action) 타입 유닛".RBT.홈; 
        }
        드릴작업1 = {
            "4 행위 (Action) 타입 유닛".System1.Api1; 
        }
    }
    [flow] "5 시스템 인터페이스" = {
        드릴작업 = {
            "5 시스템 인터페이스".드릴장치.드릴링A위치, "5 시스템 인터페이스".드릴장치.드릴링B위치; 
        }
        이동A, 드릴, 이동B; 
    }
    [flow] "5 시스템 인터페이스 유닛" = {
        "5 시스템 인터페이스 유닛".Device1.Api1.INTrue > Work2;
        드릴작업 = {
            "5 시스템 인터페이스 유닛".드릴장치.드릴링A위치, "5 시스템 인터페이스 유닛".드릴장치.드릴링B위치; 
        }
        Work1, 이동A, 드릴, 이동B; 
    }
    [flow] "기본 연결 Unit" = {
    }
    [flow] "1 기본 연결 Unit" = {
        Work1_1 |> Work2_1;
        Work1 > Work2;
        드릴작업 |> 공급작업 > 드릴작업;
        #"1 기본 연결 Unit_전원" > 드릴작업;
        드릴작업 = {
            "1 기본 연결 Unit".드릴장치.드릴링A위치, "1 기본 연결 Unit".드릴장치.드릴링B위치; 
        }
        공급작업 = {
            "1 기본 연결 Unit".RBT.투입, "1 기본 연결 Unit".RBT.홈; 
        }
        [aliases] = {
            Work1 = { Work1_1; }
            Work2 = { Work2_1; }
        }
    }
    [flow] "2 StartReset 연결 Unit" = {
        Work1 => Work2;
        Work2_1 |> Work1_1 > Work2_1;
        드릴작업 |> 공급작업 > 드릴작업;
        #"2 StartReset 연결 Unit_전원" > 드릴작업;
        드릴작업 = {
            "2 StartReset 연결 Unit".드릴장치.드릴링A위치, "2 StartReset 연결 Unit".드릴장치.드릴링B위치; 
        }
        공급작업 = {
            "2 StartReset 연결 Unit".RBT.투입, "2 StartReset 연결 Unit".RBT.홈; 
        }
        [aliases] = {
            Work1 = { Work1_1; }
            Work2 = { Work2_1; }
        }
    }
    [flow] "3 Interlock 연결 Unit" = {
        Work2 |> Work1 |> Work2;
        Work1_1 <|> Work2_1;
        공급작업 => 드릴작업;
        #"3 Interlock 연결 Unit_전원" > 드릴작업;
        드릴작업 = {
            "3 Interlock 연결 Unit".드릴장치.드릴링A위치, "3 Interlock 연결 Unit".드릴장치.드릴링B위치; 
        }
        공급작업 = {
            "3 Interlock 연결 Unit".RBT.투입, "3 Interlock 연결 Unit".RBT.홈; 
        }
        이동A, 드릴, 이동B; 
        [aliases] = {
            Work1 = { Work1_1; }
            Work2 = { Work2_1; }
        }
    }
    [flow] "4 SelfReset 연결 Unit" = {
        Work1 =|> Work2;
        Work1_1 <|> Work2_1;
        Work1_1 > Work2_1;
        공급작업 => 드릴작업 =|> 드릴작업클리어;
        #"4 SelfReset 연결 Unit_전원" > 드릴작업;
        드릴작업 = {
            "4 SelfReset 연결 Unit".드릴장치.드릴링A위치, "4 SelfReset 연결 Unit".드릴장치.드릴링B위치; 
        }
        공급작업 = {
            "4 SelfReset 연결 Unit".RBT.투입, "4 SelfReset 연결 Unit".RBT.홈; 
        }
        [aliases] = {
            Work1 = { Work1_1; }
            Work2 = { Work2_1; }
        }
    }
    [flow] "5 Group 연결 Unit" = {
        Work1 > Work4;
        Work2 > Work4;
        Work3 > Work4;
        Work1_1, Work2_1, Work3_1 > Work4_1;
        공급작업 => 드릴작업;
        #"5 Group 연결 Unit_전원" > 드릴작업;
        드릴작업 = {
            "5 Group 연결 Unit".드릴장치.드릴링A위치, "5 Group 연결 Unit".드릴장치.드릴링B위치; 
        }
        공급작업 = {
            "5 Group 연결 Unit".RBT.투입, "5 Group 연결 Unit".RBT.홈; 
        }
        [aliases] = {
            Work1 = { Work1_1; }
            Work4 = { Work4_1; }
            Work2 = { Work2_1; }
            Work3 = { Work3_1; }
        }
    }
    [flow] "확장 도형 Unit" = {
    }
    [flow] "1 외부 시스템 로딩" = {
        "System A", "System B"; 
    }
    [flow] "2 시스템 버튼 램프" = {
        "System A"; 
    }
    [flow] "2 시스템 버튼 램프 유닛" = {
        "System A"; 
    }
    [flow] "3 시스템 외부조건" = {
        "System A"; 
    }
    [flow] "3 시스템 외부조건 유닛" = {
        "System A"; 
    }
    [flow] "4 Safety 조건" = {
        Work1 = {
            "4 Safety 조건".System1.Api1, "4 Safety 조건".System1.Api2; 
        }
        Work2 = {
            "4 Safety 조건".System1.Api1; 
        }
    }
    [flow] "5 Work 초기조건" = {
        Work1, Work1_1; 
        [aliases] = {
            Work1 = { Work1_1; }
        }
    }
    [flow] "6 멀티 Action" = {
        Work1 = {
            "6 멀티 Action".System1.Api1; 
        }
        Work2 = {
            "6 멀티 Action".System.Api; 
        }
    }
    [flow] "7 멀티 Action Skip I/O" = {
        Work1 = {
            "7 멀티 Action Skip I/O".SystemA.Api; 
        }
        Work2 = {
            "7 멀티 Action Skip I/O".SystemB.Api2; 
        }
    }
    [flow] "8 Action 인터페이스 옵션" = {
        Work1 = {
            "8 Action 인터페이스 옵션".System1.Api1, "8 Action 인터페이스 옵션".System1.Api2; 
        }
        Work2 = {
            "8 Action 인터페이스 옵션".System1.Api3, "8 Action 인터페이스 옵션".System1.Api4; 
        }
    }
    [flow] "9 Action 출력 옵션" = {
        Work1 = {
            "9 Action 출력 옵션".System1.Api1; 
        }
        Work2 = {
            "9 Action 출력 옵션".System1.Api1; 
        }
    }
    [flow] "10 Action 설정 값" = {
        Work1 = {
            "10 Action 설정 값".System1.Api1._INTrue_OUTTrue; 
        }
        Work2 = {
            "10 Action 설정 값".System1.Api2._IN100_OUT500; 
        }
    }
    [flow] "11 외부 행위 (Action) 배치" = {
        "11 외부 행위 (Action) 배치".System1.Api3.INTrue > Work1_1;
        Work1 = {
            "11 외부 행위 (Action) 배치".System1.Api1, "11 외부 행위 (Action) 배치".System1.Api2; 
        }
        [aliases] = {
            Work1 = { Work1_1; }
        }
    }
    [flow] "12 내부 행위 (Action) 배치" = {
        #"12 내부 행위 (Action) 배치_Action3" > Work1_1;
        Work1 = {
            "12 내부 행위 (Action) 배치_Action1"(), "12 내부 행위 (Action) 배치_Action2"(); 
        }
        [aliases] = {
            Work1 = { Work1_1; }
        }
    }
    [flow] "13 행위 사용 안함" = {
        Work1 = {
            "13 행위 사용 안함_Action1"(), "13 행위 사용 안함_Action2"(); 
        }
        Work2 = {
            "13 행위 사용 안함_Action1"(); 
        }
    }
    [flow] "14 Work 설정시간" = {
        Work1, Work2, Work3; 
    }
    [flow] "15 Work 데이터전송" = {
        Work1 => Work2 => Work4;
        Work1 > Work3 => Work4;
        Work1_1 => Work2_1 => Work4_1;
        Work1_1 > Work3_1 => Work4_1;
        [aliases] = {
            Work1 = { Work1_1; }
            Work3 = { Work3_1; }
            Work2 = { Work2_1; }
            Work4 = { Work4_1; }
        }
    }
    [flow] "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)" = {
        Work1 = {
            "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)".System1.Api1 > "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)".System1.Api2;
        }
    }
    [flow] "IO Table" = {
    }
    [flow] "1 외부 주소" = {
        Work1 = {
            "1 외부 주소".Device1.ADV > "1 외부 주소".Device1.RET;
        }
    }
    [flow] "2 내부 변수/상수" = {
        #"2 내부 변수/상수_Operator" > Work1;
        Work1 = {
            "2 내부 변수/상수_Command"(); 
        }
    }
    [flow] "3 내부 연산/명령" = {
        #"3 내부 연산/명령_Operator" > Work1;
        Work1 = {
            "3 내부 연산/명령_Command2"(); 
        }
    }
    [flow] "4 버튼 IO" = {
    }
    [flow] "5 램프 IO" = {
    }
    [flow] "6 심볼 정의" = {
        #"6 심볼 정의_Operator" > Work1;
        Work1 = {
            "6 심볼 정의_Command"(); 
        }
        Work2 = {
            "6 심볼 정의".Device1.ADV > "6 심볼 정의".Device1.RET._INTrue_OUT300;
        }
    }
    [jobs] = {
        "1 기본 연결 Unit".드릴장치.드릴링A위치 = { "1 기본 연결 Unit_드릴장치".드릴링A위치(IB0.2, OB0.2); }
        "2 StartReset 연결 Unit".드릴장치.드릴링A위치 = { "2 StartReset 연결 Unit_드릴장치".드릴링A위치(IB2.1, OB2.1); }
        "4 SelfReset 연결 Unit".드릴장치.드릴링A위치 = { "4 SelfReset 연결 Unit_드릴장치".드릴링A위치(IB4.5, OB4.5); }
        "5 Group 연결 Unit".드릴장치.드릴링A위치 = { "5 Group 연결 Unit_드릴장치".드릴링A위치(IB5.6, OB5.6); }
        "5 시스템 인터페이스 유닛".드릴장치.드릴링A위치 = { "5 시스템 인터페이스 유닛_드릴장치".드릴링A위치(IB6.1, OB6.1); }
        "3 Interlock 연결 Unit".드릴장치.드릴링A위치 = { "3 Interlock 연결 Unit_드릴장치".드릴링A위치(IB3.3, OB3.3); }
        "1 작업 및 행위 유닛".RBT.투입 = { "1 작업 및 행위 유닛_RBT".투입(IB0.5, OB0.5); }
        "2 행위 (Action) 배치 유닛".RBT.투입 = { "2 행위 (Action) 배치 유닛_RBT".투입(IB2.5, OB2.5); }
        "3 작업 (Work) 타입 유닛".RBT.투입 = { "3 작업 (Work) 타입 유닛_RBT".투입(IB3.5, OB3.5); }
        "4 행위 (Action) 타입 유닛".RBT.투입 = { "4 행위 (Action) 타입 유닛_RBT".투입(IB4.7, OB4.7); }
        "1 기본 연결 Unit".RBT.투입 = { "1 기본 연결 Unit_RBT".투입(IB0.0, OB0.0); }
        "2 StartReset 연결 Unit".RBT.투입 = { "2 StartReset 연결 Unit_RBT".투입(IB1.7, OB1.7); }
        "4 SelfReset 연결 Unit".RBT.투입 = { "4 SelfReset 연결 Unit_RBT".투입(IB4.3, OB4.3); }
        "5 Group 연결 Unit".RBT.투입 = { "5 Group 연결 Unit_RBT".투입(IB5.4, OB5.4); }
        "3 Interlock 연결 Unit".RBT.투입 = { "3 Interlock 연결 Unit_RBT".투입(IB3.1, OB3.1); }
        "6 심볼 정의".Device1.ADV = { "6 심볼 정의_Device1".ADV(P00000:Dev1ADV_I, P00040:Dev1ADV_O); }
        "1 기본 연결 Unit".드릴장치.드릴링B위치 = { "1 기본 연결 Unit_드릴장치".드릴링B위치(IB0.3, OB0.3); }
        "2 StartReset 연결 Unit".드릴장치.드릴링B위치 = { "2 StartReset 연결 Unit_드릴장치".드릴링B위치(IB2.2, OB2.2); }
        "4 SelfReset 연결 Unit".드릴장치.드릴링B위치 = { "4 SelfReset 연결 Unit_드릴장치".드릴링B위치(IB4.6, OB4.6); }
        "5 시스템 인터페이스 유닛".드릴장치.드릴링B위치 = { "5 시스템 인터페이스 유닛_드릴장치".드릴링B위치(IB6.2, OB6.2); }
        "5 Group 연결 Unit".드릴장치.드릴링B위치 = { "5 Group 연결 Unit_드릴장치".드릴링B위치(IB5.7, OB5.7); }
        "3 Interlock 연결 Unit".드릴장치.드릴링B위치 = { "3 Interlock 연결 Unit_드릴장치".드릴링B위치(IB3.4, OB3.4); }
        "6 심볼 정의".Device1.RET._INTrue_OUT300 = { "6 심볼 정의_Device1".RET(P00001:Dev1RET_I:Boolean:True, P0041:Dev1RET_O:Int32:300); }
        "1 작업 및 행위 유닛".RBT.홈 = { "1 작업 및 행위 유닛_RBT".홈(IB0.6, OB0.6); }
        "2 행위 (Action) 배치 유닛".RBT.홈 = { "2 행위 (Action) 배치 유닛_RBT".홈(IB2.6, OB2.6); }
        "3 작업 (Work) 타입 유닛".RBT.홈 = { "3 작업 (Work) 타입 유닛_RBT".홈(IB3.6, OB3.6); }
        "4 행위 (Action) 타입 유닛".RBT.홈 = { "4 행위 (Action) 타입 유닛_RBT".홈(IB5.0, OB5.0); }
        "1 기본 연결 Unit".RBT.홈 = { "1 기본 연결 Unit_RBT".홈(IB0.1, OB0.1); }
        "2 StartReset 연결 Unit".RBT.홈 = { "2 StartReset 연결 Unit_RBT".홈(IB2.0, OB2.0); }
        "4 SelfReset 연결 Unit".RBT.홈 = { "4 SelfReset 연결 Unit_RBT".홈(IB4.4, OB4.4); }
        "5 Group 연결 Unit".RBT.홈 = { "5 Group 연결 Unit_RBT".홈(IB5.5, OB5.5); }
        "3 Interlock 연결 Unit".RBT.홈 = { "3 Interlock 연결 Unit_RBT".홈(IB3.2, OB3.2); }
        "5 시스템 인터페이스".드릴장치.드릴링A위치 = { "5 시스템 인터페이스_드릴장치".드릴링A위치(IB6.3, OB6.3); }
        "2 행위 (Action) 배치".RBT.투입 = { "2 행위 (Action) 배치_RBT".투입(IB2.7, OB2.7); }
        "3 작업 (Work) 타입".RBT.투입 = { "3 작업 (Work) 타입_RBT".투입(IB3.7, OB3.7); }
        "1 작업 및 행위".RBT.투입 = { "1 작업 및 행위_RBT".투입(IB0.7, OB0.7); }
        "4 행위 (Action) 타입".RBT.투입 = { "4 행위 (Action) 타입_RBT".투입(IB5.2, OB5.2); }
        "4 Safety 조건".System1.Api1 = { "4 Safety 조건_System1".Api1(IB4.1, OB4.1); }
        "1 작업 및 행위 유닛".Device.Action1.INTrue = { "1 작업 및 행위 유닛_Device".Action1(IB0.4, -); }
        "8 Action 인터페이스 옵션".System1.Api1[N1(1, 0)] = { "8 Action 인터페이스 옵션_System1_01".Api1(IB8.2, -); }
        "8 Action 인터페이스 옵션".System1.Api3[N1(0, 0)] = { "8 Action 인터페이스 옵션_System1_01".Api3(-, -); }
        "6 멀티 Action".System1.Api1 = { "6 멀티 Action_System1".Api1(IB6.5, OB6.5); }
        "7 멀티 Action Skip I/O".SystemA.Api[N4(4, 4)] = { "7 멀티 Action Skip I/O_SystemA_01".Api(IB7.2, OB7.2); "7 멀티 Action Skip I/O_SystemA_02".Api(IB7.3, OB7.3); "7 멀티 Action Skip I/O_SystemA_03".Api(IB7.4, OB7.4); "7 멀티 Action Skip I/O_SystemA_04".Api(IB7.5, OB7.5); }
        "6 멀티 Action".System.Api[N4(4, 4)] = { "6 멀티 Action_System_01".Api(IB6.6, OB6.6); "6 멀티 Action_System_02".Api(IB6.7, OB6.7); "6 멀티 Action_System_03".Api(IB7.0, OB7.0); "6 멀티 Action_System_04".Api(IB7.1, OB7.1); }
        "7 멀티 Action Skip I/O".SystemB.Api2[N4(4, 1)] = { "7 멀티 Action Skip I/O_SystemB_01".Api2(IB7.6, OB7.6); "7 멀티 Action Skip I/O_SystemB_02".Api2(IB7.7, -); "7 멀티 Action Skip I/O_SystemB_03".Api2(IB8.0, -); "7 멀티 Action Skip I/O_SystemB_04".Api2(IB8.1, -); }
        "5 시스템 인터페이스".드릴장치.드릴링B위치 = { "5 시스템 인터페이스_드릴장치".드릴링B위치(IB6.4, OB6.4); }
        "9 Action 출력 옵션".System1.Api1 = { "9 Action 출력 옵션_System1".Api1(IB8.3, OB8.0); }
        "1 외부 주소".Device1.ADV = { "1 외부 주소_Device1".ADV(P00000, P00040); }
        "4 행위 (Action) 타입 유닛".System1.Api1 = { "4 행위 (Action) 타입 유닛_System1".Api1(IB5.1, OB5.1); }
        "11 외부 행위 (Action) 배치".System1.Api3.INTrue = { "11 외부 행위 (Action) 배치_System1".Api3(IB1.4, -); }
        "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)".System1.Api1 = { "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)_System1".Api1(IB1.5, OB1.5); }
        "10 Action 설정 값".System1.Api1._INTrue_OUTTrue = { "10 Action 설정 값_System1".Api1(IB1.1:Boolean:True, OB1.1:Boolean:True); }
        "10 Action 설정 값".System1.Api2._IN100_OUT500 = { "10 Action 설정 값_System1".Api2(IB12.0:Int32:100, OB12.0:Int32:500); }
        "2 행위 (Action) 배치 유닛".Device.Action1.INTrue = { "2 행위 (Action) 배치 유닛_Device".Action1(IB2.3, OB2.3); }
        "2 행위 (Action) 배치 유닛".Device.Action1 = { "2 행위 (Action) 배치 유닛_Device".Action1(IB2.3, OB2.3); }
        "11 외부 행위 (Action) 배치".System1.Api1 = { "11 외부 행위 (Action) 배치_System1".Api1(IB1.2, OB1.2); }
        "2 행위 (Action) 배치 유닛".Device.Action2 = { "2 행위 (Action) 배치 유닛_Device".Action2(IB2.4, OB2.4); }
        "11 외부 행위 (Action) 배치".System1.Api2 = { "11 외부 행위 (Action) 배치_System1".Api2(IB1.3, OB1.3); }
        "1 작업 및 행위".RBT.홈 = { "1 작업 및 행위_RBT".홈(IB1.0, OB1.0); }
        "4 Safety 조건".System1.Api2 = { "4 Safety 조건_System1".Api2(IB4.2, OB4.2); }
        "2 행위 (Action) 배치".RBT.홈 = { "2 행위 (Action) 배치_RBT".홈(IB3.0, OB3.0); }
        "1 외부 주소".Device1.RET = { "1 외부 주소_Device1".RET(P00001, P00041); }
        "4 행위 (Action) 타입".RBT.홈 = { "4 행위 (Action) 타입_RBT".홈(IB5.3, OB5.3); }
        "3 작업 (Work) 타입".RBT.홈 = { "3 작업 (Work) 타입_RBT".홈(IB4.0, OB4.0); }
        "8 Action 인터페이스 옵션".System1.Api2[N1(0, 1)] = { "8 Action 인터페이스 옵션_System1_01".Api2(-, OB7.7); }
        "8 Action 인터페이스 옵션".System1.Api4[N1(0, 0)] = { "8 Action 인터페이스 옵션_System1_01".Api4(-, -); }
        "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)".System1.Api2 = { "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)_System1".Api2(IB1.6, OB1.6); }
        "5 시스템 인터페이스 유닛".Device1.Api1.INTrue = { "5 시스템 인터페이스 유닛_Device1".Api1(IB6.0, -); }
    }
    [variables] = {
        Int32 VARIABLE1;
        Int32 VARIABLE2;
        Int32 VARIABLE3;
        Double VARIABLE4;
        const Double PI = 3.14;
        const Double PI_PI_PI = 3.14;
        const Double PI_PI = 3.14;
    }
    [operators] = {
        "2 행위 (Action) 배치 유닛_전원";
        "3 작업 (Work) 타입 유닛_전원";
        "4 행위 (Action) 타입 유닛_전원";
        "1 기본 연결 Unit_전원";
        "2 StartReset 연결 Unit_전원";
        "4 SelfReset 연결 Unit_전원";
        "5 Group 연결 Unit_전원";
        "3 Interlock 연결 Unit_전원";
        "2 내부 변수/상수_Operator";
        "3 내부 연산/명령_Operator";
        "6 심볼 정의_Operator";
        "4 행위 (Action) 타입 유닛_Action1";
        "12 내부 행위 (Action) 배치_Action3";
        "2 행위 (Action) 배치_전원";
        "4 행위 (Action) 타입_전원";
        "3 작업 (Work) 타입_전원";
        Operator2 = #{$VARIABLE4 !=$PI_PI_PI;}
        Operator3 = #{$Dev1ADV_I == false;}
    }
    [commands] = {
        "12 내부 행위 (Action) 배치_Action1";
        "12 내부 행위 (Action) 배치_Action2";
        "13 행위 사용 안함_Action1";
        "13 행위 사용 안함_Action2";
        "6 심볼 정의_Command";
        "2 내부 변수/상수_Command";
        "3 내부 연산/명령_Command2";
        Command2 = #{$VARIABLE1 = 7;}
        Command3 = #{$Dev1RET_O = 22;}
    }
    [interfaces] = {
        드릴링A위치3 = { "5 시스템 인터페이스".이동A ~ "5 시스템 인터페이스".드릴 }
        드릴링B위치3 = { "5 시스템 인터페이스".이동B ~ "5 시스템 인터페이스".드릴 }
        Api1 = { "5 시스템 인터페이스 유닛".Work1 ~ "5 시스템 인터페이스 유닛".Work2 }
        드릴링A위치1 = { "5 시스템 인터페이스 유닛".이동A ~ "5 시스템 인터페이스 유닛".드릴 }
        드릴링B위치1 = { "5 시스템 인터페이스 유닛".이동B ~ "5 시스템 인터페이스 유닛".드릴 }
        드릴링A위치2 = { "3 Interlock 연결 Unit".이동A ~ "3 Interlock 연결 Unit".드릴 }
        드릴링B위치2 = { "3 Interlock 연결 Unit".이동B ~ "3 Interlock 연결 Unit".드릴 }
        드릴링A위치2 <|> 드릴링B위치2;
    }
    [buttons] = {
        [a] = {
            AutoSelect(M1001, -) = { "시스템 모델링"; "모델링 기본 구성"; "모델링 확장 구성1"; "모델링 확장 구성2"; "모델링 구조 Unit"; "기본 도형 Unit"; "1 작업 및 행위"; "1 작업 및 행위 유닛"; "2 행위 (Action) 배치"; "2 행위 (Action) 배치 유닛"; "3 작업 (Work) 타입"; "3 작업 (Work) 타입 유닛"; "4 행위 (Action) 타입"; "4 행위 (Action) 타입 유닛"; "5 시스템 인터페이스"; "5 시스템 인터페이스 유닛"; "기본 연결 Unit"; "1 기본 연결 Unit"; "2 StartReset 연결 Unit"; "3 Interlock 연결 Unit"; "4 SelfReset 연결 Unit"; "5 Group 연결 Unit"; "확장 도형 Unit"; "1 외부 시스템 로딩"; "2 시스템 버튼 램프"; "2 시스템 버튼 램프 유닛"; "3 시스템 외부조건"; "3 시스템 외부조건 유닛"; "4 Safety 조건"; "5 Work 초기조건"; "6 멀티 Action"; "7 멀티 Action Skip I/O"; "8 Action 인터페이스 옵션"; "9 Action 출력 옵션"; "10 Action 설정 값"; "11 외부 행위 (Action) 배치"; "12 내부 행위 (Action) 배치"; "13 행위 사용 안함"; "14 Work 설정시간"; "15 Work 데이터전송"; "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)"; "IO Table"; "1 외부 주소"; "2 내부 변수/상수"; "3 내부 연산/명령"; "4 버튼 IO"; "5 램프 IO"; "6 심볼 정의"; }
            "2 시스템 버튼 램프 유닛.AutoBTN1"(M1002, -) = { "2 시스템 버튼 램프 유닛"; }
            "4 버튼 IO.AutoBTN2"(M00628, -) = { "4 버튼 IO"; }
        }
        [m] = {
            ManualSelect(M1003, -) = { "시스템 모델링"; "모델링 기본 구성"; "모델링 확장 구성1"; "모델링 확장 구성2"; "모델링 구조 Unit"; "기본 도형 Unit"; "1 작업 및 행위"; "1 작업 및 행위 유닛"; "2 행위 (Action) 배치"; "2 행위 (Action) 배치 유닛"; "3 작업 (Work) 타입"; "3 작업 (Work) 타입 유닛"; "4 행위 (Action) 타입"; "4 행위 (Action) 타입 유닛"; "5 시스템 인터페이스"; "5 시스템 인터페이스 유닛"; "기본 연결 Unit"; "1 기본 연결 Unit"; "2 StartReset 연결 Unit"; "3 Interlock 연결 Unit"; "4 SelfReset 연결 Unit"; "5 Group 연결 Unit"; "확장 도형 Unit"; "1 외부 시스템 로딩"; "2 시스템 버튼 램프"; "2 시스템 버튼 램프 유닛"; "3 시스템 외부조건"; "3 시스템 외부조건 유닛"; "4 Safety 조건"; "5 Work 초기조건"; "6 멀티 Action"; "7 멀티 Action Skip I/O"; "8 Action 인터페이스 옵션"; "9 Action 출력 옵션"; "10 Action 설정 값"; "11 외부 행위 (Action) 배치"; "12 내부 행위 (Action) 배치"; "13 행위 사용 안함"; "14 Work 설정시간"; "15 Work 데이터전송"; "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)"; "IO Table"; "1 외부 주소"; "2 내부 변수/상수"; "3 내부 연산/명령"; "4 버튼 IO"; "5 램프 IO"; "6 심볼 정의"; }
            "2 시스템 버튼 램프 유닛.ManualBTN1"(M1004, -) = { "2 시스템 버튼 램프 유닛"; }
            "4 버튼 IO.ManualBTN2"(M00629, -) = { "4 버튼 IO"; }
        }
        [d] = {
            DrivePushBtn(M1005, -) = { "시스템 모델링"; "모델링 기본 구성"; "모델링 확장 구성1"; "모델링 확장 구성2"; "모델링 구조 Unit"; "기본 도형 Unit"; "1 작업 및 행위"; "1 작업 및 행위 유닛"; "2 행위 (Action) 배치"; "2 행위 (Action) 배치 유닛"; "3 작업 (Work) 타입"; "3 작업 (Work) 타입 유닛"; "4 행위 (Action) 타입"; "4 행위 (Action) 타입 유닛"; "5 시스템 인터페이스"; "5 시스템 인터페이스 유닛"; "기본 연결 Unit"; "1 기본 연결 Unit"; "2 StartReset 연결 Unit"; "3 Interlock 연결 Unit"; "4 SelfReset 연결 Unit"; "5 Group 연결 Unit"; "확장 도형 Unit"; "1 외부 시스템 로딩"; "2 시스템 버튼 램프"; "2 시스템 버튼 램프 유닛"; "3 시스템 외부조건"; "3 시스템 외부조건 유닛"; "4 Safety 조건"; "5 Work 초기조건"; "6 멀티 Action"; "7 멀티 Action Skip I/O"; "8 Action 인터페이스 옵션"; "9 Action 출력 옵션"; "10 Action 설정 값"; "11 외부 행위 (Action) 배치"; "12 내부 행위 (Action) 배치"; "13 행위 사용 안함"; "14 Work 설정시간"; "15 Work 데이터전송"; "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)"; "IO Table"; "1 외부 주소"; "2 내부 변수/상수"; "3 내부 연산/명령"; "4 버튼 IO"; "5 램프 IO"; "6 심볼 정의"; }
            "2 시스템 버튼 램프 유닛.DriveBTN1"(M1006, -) = { "2 시스템 버튼 램프 유닛"; }
            "4 버튼 IO.DriveBTN2"(M0062A, -) = { "4 버튼 IO"; }
        }
        [e] = {
            EmergencyBtn(M1007, -) = { "시스템 모델링"; "모델링 기본 구성"; "모델링 확장 구성1"; "모델링 확장 구성2"; "모델링 구조 Unit"; "기본 도형 Unit"; "1 작업 및 행위"; "1 작업 및 행위 유닛"; "2 행위 (Action) 배치"; "2 행위 (Action) 배치 유닛"; "3 작업 (Work) 타입"; "3 작업 (Work) 타입 유닛"; "4 행위 (Action) 타입"; "4 행위 (Action) 타입 유닛"; "5 시스템 인터페이스"; "5 시스템 인터페이스 유닛"; "기본 연결 Unit"; "1 기본 연결 Unit"; "2 StartReset 연결 Unit"; "3 Interlock 연결 Unit"; "4 SelfReset 연결 Unit"; "5 Group 연결 Unit"; "확장 도형 Unit"; "1 외부 시스템 로딩"; "2 시스템 버튼 램프"; "2 시스템 버튼 램프 유닛"; "3 시스템 외부조건"; "3 시스템 외부조건 유닛"; "4 Safety 조건"; "5 Work 초기조건"; "6 멀티 Action"; "7 멀티 Action Skip I/O"; "8 Action 인터페이스 옵션"; "9 Action 출력 옵션"; "10 Action 설정 값"; "11 외부 행위 (Action) 배치"; "12 내부 행위 (Action) 배치"; "13 행위 사용 안함"; "14 Work 설정시간"; "15 Work 데이터전송"; "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)"; "IO Table"; "1 외부 주소"; "2 내부 변수/상수"; "3 내부 연산/명령"; "4 버튼 IO"; "5 램프 IO"; "6 심볼 정의"; }
            "2 시스템 버튼 램프 유닛.EmergencyBTN1"(M1008, -) = { "2 시스템 버튼 램프 유닛"; }
            "4 버튼 IO.EmergencyBTN2"(M0062D, -) = { "4 버튼 IO"; }
        }
        [t] = {
            "2 시스템 버튼 램프 유닛.TestBTN1"(M1009, -) = { "2 시스템 버튼 램프 유닛"; }
            "4 버튼 IO.TestBTN2"(M0062C, -) = { "4 버튼 IO"; }
        }
        [r] = {
            "2 시스템 버튼 램프 유닛.ReadyBTN1"(M1010, -) = { "2 시스템 버튼 램프 유닛"; }
            "3 시스템 외부조건 유닛.Condition1"(M1011, -) = { "3 시스템 외부조건 유닛"; }
            "3 시스템 외부조건 유닛.Condition2"(M1012, -) = { "3 시스템 외부조건 유닛"; }
            "4 버튼 IO.ReadyBTN2"(M0062C, -) = { "4 버튼 IO"; }
        }
        [p] = {
            PausePushBtn(M1013, -) = { "시스템 모델링"; "모델링 기본 구성"; "모델링 확장 구성1"; "모델링 확장 구성2"; "모델링 구조 Unit"; "기본 도형 Unit"; "1 작업 및 행위"; "1 작업 및 행위 유닛"; "2 행위 (Action) 배치"; "2 행위 (Action) 배치 유닛"; "3 작업 (Work) 타입"; "3 작업 (Work) 타입 유닛"; "4 행위 (Action) 타입"; "4 행위 (Action) 타입 유닛"; "5 시스템 인터페이스"; "5 시스템 인터페이스 유닛"; "기본 연결 Unit"; "1 기본 연결 Unit"; "2 StartReset 연결 Unit"; "3 Interlock 연결 Unit"; "4 SelfReset 연결 Unit"; "5 Group 연결 Unit"; "확장 도형 Unit"; "1 외부 시스템 로딩"; "2 시스템 버튼 램프"; "2 시스템 버튼 램프 유닛"; "3 시스템 외부조건"; "3 시스템 외부조건 유닛"; "4 Safety 조건"; "5 Work 초기조건"; "6 멀티 Action"; "7 멀티 Action Skip I/O"; "8 Action 인터페이스 옵션"; "9 Action 출력 옵션"; "10 Action 설정 값"; "11 외부 행위 (Action) 배치"; "12 내부 행위 (Action) 배치"; "13 행위 사용 안함"; "14 Work 설정시간"; "15 Work 데이터전송"; "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)"; "IO Table"; "1 외부 주소"; "2 내부 변수/상수"; "3 내부 연산/명령"; "4 버튼 IO"; "5 램프 IO"; "6 심볼 정의"; }
            "2 시스템 버튼 램프 유닛.PauseBTN1"(M1014, -) = { "2 시스템 버튼 램프 유닛"; }
            "4 버튼 IO.PauseBTN2"(M0062B, -) = { "4 버튼 IO"; }
        }
        [c] = {
            ClearPushBtn(M1015, -) = { "시스템 모델링"; "모델링 기본 구성"; "모델링 확장 구성1"; "모델링 확장 구성2"; "모델링 구조 Unit"; "기본 도형 Unit"; "1 작업 및 행위"; "1 작업 및 행위 유닛"; "2 행위 (Action) 배치"; "2 행위 (Action) 배치 유닛"; "3 작업 (Work) 타입"; "3 작업 (Work) 타입 유닛"; "4 행위 (Action) 타입"; "4 행위 (Action) 타입 유닛"; "5 시스템 인터페이스"; "5 시스템 인터페이스 유닛"; "기본 연결 Unit"; "1 기본 연결 Unit"; "2 StartReset 연결 Unit"; "3 Interlock 연결 Unit"; "4 SelfReset 연결 Unit"; "5 Group 연결 Unit"; "확장 도형 Unit"; "1 외부 시스템 로딩"; "2 시스템 버튼 램프"; "2 시스템 버튼 램프 유닛"; "3 시스템 외부조건"; "3 시스템 외부조건 유닛"; "4 Safety 조건"; "5 Work 초기조건"; "6 멀티 Action"; "7 멀티 Action Skip I/O"; "8 Action 인터페이스 옵션"; "9 Action 출력 옵션"; "10 Action 설정 값"; "11 외부 행위 (Action) 배치"; "12 내부 행위 (Action) 배치"; "13 행위 사용 안함"; "14 Work 설정시간"; "15 Work 데이터전송"; "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)"; "IO Table"; "1 외부 주소"; "2 내부 변수/상수"; "3 내부 연산/명령"; "4 버튼 IO"; "5 램프 IO"; "6 심볼 정의"; }
            "2 시스템 버튼 램프 유닛.ClearBTN1"(M1016, -) = { "2 시스템 버튼 램프 유닛"; }
            "4 버튼 IO.ClearBTN2"(M0062C, -) = { "4 버튼 IO"; }
        }
        [h] = {
            "2 시스템 버튼 램프 유닛.HomeBTN1"(M1017, -) = { "2 시스템 버튼 램프 유닛"; }
            "4 버튼 IO.HomeBTN2"(M0062C, -) = { "4 버튼 IO"; }
        }
    }
    [lamps] = {
        [a] = { AutoModeLamp(-, M1018) = {  } }
        [m] = { ManualModeLamp(-, M1019) = {  } }
        [d] = { DriveLamp(-, M1020) = {  } }
        [e] = { ErrorLamp(-, M1021) = {  } }
        [r] = { ReadyStateLamp(-, M1022) = {  } }
        [i] = { IdleModeLamp(-, M1023) = {  } }
        [o] = { OriginStateLamp(-, M1024) = {  } }
    }
    [conditions] = {
        [d] = {
            "3 시스템 외부조건 유닛_Condition3"(M1025, -) = { "3 시스템 외부조건 유닛"; }
            "3 시스템 외부조건 유닛_Condition4"(M1026, -) = { "3 시스템 외부조건 유닛"; }
        }
    }
    [prop] = {
        [safety] = {
            "4 Safety 조건".Work1.System1.Api1 = { "4 Safety 조건".System1.Api2; }
            "4 Safety 조건".Work1.System1.Api2 = { "4 Safety 조건".System1.Api1; }
            "4 Safety 조건".Work2.System1.Api1 = { "6 멀티 Action".System1.Api1; }
        }
        [autopre] = {
            "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)".Work1.System1.Api1 = { "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)".System1.Api2; }
        }
        [layouts] = {
            "1 기본 연결 Unit_드릴장치" = (1061, 210, 168, 60);
            "2 StartReset 연결 Unit_드릴장치" = (1061, 210, 168, 60);
            "4 SelfReset 연결 Unit_드릴장치" = (1061, 210, 168, 60);
            "5 Group 연결 Unit_드릴장치" = (1061, 216, 168, 60);
            "5 시스템 인터페이스 유닛_드릴장치" = (1159, 210, 168, 60);
            "3 Interlock 연결 Unit_드릴장치" = (1061, 223, 168, 60);
            "1 작업 및 행위 유닛_RBT" = (1310, 296, 164, 91);
            "2 행위 (Action) 배치 유닛_RBT" = (1310, 296, 164, 91);
            "3 작업 (Work) 타입 유닛_RBT" = (1310, 296, 164, 91);
            "4 행위 (Action) 타입 유닛_RBT" = (1310, 296, 164, 91);
            "1 기본 연결 Unit_RBT" = (1310, 296, 164, 91);
            "2 StartReset 연결 Unit_RBT" = (1310, 296, 164, 91);
            "4 SelfReset 연결 Unit_RBT" = (1310, 296, 164, 91);
            "5 Group 연결 Unit_RBT" = (1310, 302, 164, 91);
            "3 Interlock 연결 Unit_RBT" = (1388, 305, 164, 91);
            "6 심볼 정의_Device1" = (1483, 294, 246, 64);
            "1 외부 주소_Device1" = (306, 773, 436, 108);
            "5 시스템 인터페이스_드릴장치" = (552, 548, 243, 90);
            "2 행위 (Action) 배치_RBT" = (872, 769, 322, 176);
            "3 작업 (Work) 타입_RBT" = (876, 774, 308, 173);
            "1 작업 및 행위_RBT" = (876, 759, 305, 164);
            "4 행위 (Action) 타입_RBT" = (856, 773, 306, 163);
            "4 Safety 조건_System1" = (1083, 521, 618, 185);
            "1 작업 및 행위 유닛_Device" = (1143, 522, 474, 304);
            "8 Action 인터페이스 옵션_System1_01" = (1100, 785, 563, 192);
            "6 멀티 Action_System1" = (257, 540, 563, 244);
            "7 멀티 Action Skip I/O_SystemA_01" = (257, 540, 563, 244);
            "7 멀티 Action Skip I/O_SystemA_02" = (257, 540, 563, 244);
            "7 멀티 Action Skip I/O_SystemA_03" = (257, 540, 563, 244);
            "7 멀티 Action Skip I/O_SystemA_04" = (257, 540, 563, 244);
            "6 멀티 Action_System_01" = (1099, 540, 563, 244);
            "6 멀티 Action_System_02" = (1099, 540, 563, 244);
            "6 멀티 Action_System_03" = (1099, 540, 563, 244);
            "6 멀티 Action_System_04" = (1099, 540, 563, 244);
            "7 멀티 Action Skip I/O_SystemB_01" = (1099, 540, 563, 244);
            "7 멀티 Action Skip I/O_SystemB_02" = (1099, 540, 563, 244);
            "7 멀티 Action Skip I/O_SystemB_03" = (1099, 540, 563, 244);
            "7 멀티 Action Skip I/O_SystemB_04" = (1099, 540, 563, 244);
            "9 Action 출력 옵션_System1" = (1099, 556, 563, 192);
            "4 행위 (Action) 타입 유닛_System1" = (1110, 578, 563, 304);
            "11 외부 행위 (Action) 배치_System1" = (962, 607, 307, 130);
            "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)_System1" = (127, 640, 439, 94);
            "10 Action 설정 값_System1" = (1115, 650, 563, 244);
            "2 행위 (Action) 배치 유닛_Device" = (1011, 660, 239, 93);
            "5 시스템 인터페이스 유닛_Device1" = (1233, 815, 267, 114);
        }
        [times] = {
            "14 Work 설정시간".Work1 = {AVG(0.1)};
            "14 Work 설정시간".Work2 = {TON(1)};
            "14 Work 설정시간".Work3 = {AVG(0.1),TON(1)};
        }
        [notrans] = {
            "15 Work 데이터전송".Work3;
        }
    }
    [device file="./dsLib/AutoGen/1 기본 연결 Unit_드릴장치.ds"] "1 기본 연결 Unit_드릴장치"; 
    [device file="./dsLib/AutoGen/2 StartReset 연결 Unit_드릴장치.ds"] "2 StartReset 연결 Unit_드릴장치"; 
    [device file="./dsLib/AutoGen/4 SelfReset 연결 Unit_드릴장치.ds"] "4 SelfReset 연결 Unit_드릴장치"; 
    [device file="./dsLib/AutoGen/5 Group 연결 Unit_드릴장치.ds"] "5 Group 연결 Unit_드릴장치"; 
    [device file="./dsLib/AutoGen/5 시스템 인터페이스 유닛_드릴장치.ds"] "5 시스템 인터페이스 유닛_드릴장치"; 
    [device file="./dsLib/AutoGen/3 Interlock 연결 Unit_드릴장치.ds"] "3 Interlock 연결 Unit_드릴장치"; 
    [device file="./dsLib/AutoGen/1 작업 및 행위 유닛_RBT.ds"] "1 작업 및 행위 유닛_RBT"; 
    [device file="./dsLib/AutoGen/2 행위 (Action) 배치 유닛_RBT.ds"] "2 행위 (Action) 배치 유닛_RBT"; 
    [device file="./dsLib/AutoGen/3 작업 (Work) 타입 유닛_RBT.ds"] "3 작업 (Work) 타입 유닛_RBT"; 
    [device file="./dsLib/AutoGen/4 행위 (Action) 타입 유닛_RBT.ds"] "4 행위 (Action) 타입 유닛_RBT"; 
    [device file="./dsLib/AutoGen/1 기본 연결 Unit_RBT.ds"] "1 기본 연결 Unit_RBT"; 
    [device file="./dsLib/AutoGen/2 StartReset 연결 Unit_RBT.ds"] "2 StartReset 연결 Unit_RBT"; 
    [device file="./dsLib/AutoGen/4 SelfReset 연결 Unit_RBT.ds"] "4 SelfReset 연결 Unit_RBT"; 
    [device file="./dsLib/AutoGen/5 Group 연결 Unit_RBT.ds"] "5 Group 연결 Unit_RBT"; 
    [device file="./dsLib/AutoGen/3 Interlock 연결 Unit_RBT.ds"] "3 Interlock 연결 Unit_RBT"; 
    [device file="./dsLib/Cylinder/DoubleCylinder.ds"] 
        "6 심볼 정의_Device1",
        "1 외부 주소_Device1"; 
    [device file="./dsLib/AutoGen/5 시스템 인터페이스_드릴장치.ds"] "5 시스템 인터페이스_드릴장치"; 
    [device file="./dsLib/AutoGen/2 행위 (Action) 배치_RBT.ds"] "2 행위 (Action) 배치_RBT"; 
    [device file="./dsLib/AutoGen/3 작업 (Work) 타입_RBT.ds"] "3 작업 (Work) 타입_RBT"; 
    [device file="./dsLib/AutoGen/1 작업 및 행위_RBT.ds"] "1 작업 및 행위_RBT"; 
    [device file="./dsLib/AutoGen/4 행위 (Action) 타입_RBT.ds"] "4 행위 (Action) 타입_RBT"; 
    [device file="./dsLib/AutoGen/4 Safety 조건_System1.ds"] "4 Safety 조건_System1"; 
    [device file="./dsLib/AutoGen/1 작업 및 행위 유닛_Device.ds"] "1 작업 및 행위 유닛_Device"; 
    [device file="./dsLib/AutoGen/8 Action 인터페이스 옵션_System1_01.ds"] "8 Action 인터페이스 옵션_System1_01"; 
    [device file="./dsLib/AutoGen/6 멀티 Action_System1.ds"] "6 멀티 Action_System1"; 
    [device file="./dsLib/AutoGen/7 멀티 Action Skip I/O_SystemA_01.ds"] "7 멀티 Action Skip I/O_SystemA_01"; 
    [device file="./dsLib/AutoGen/7 멀티 Action Skip I/O_SystemA_02.ds"] "7 멀티 Action Skip I/O_SystemA_02"; 
    [device file="./dsLib/AutoGen/7 멀티 Action Skip I/O_SystemA_03.ds"] "7 멀티 Action Skip I/O_SystemA_03"; 
    [device file="./dsLib/AutoGen/7 멀티 Action Skip I/O_SystemA_04.ds"] "7 멀티 Action Skip I/O_SystemA_04"; 
    [device file="./dsLib/AutoGen/6 멀티 Action_System_01.ds"] "6 멀티 Action_System_01"; 
    [device file="./dsLib/AutoGen/6 멀티 Action_System_02.ds"] "6 멀티 Action_System_02"; 
    [device file="./dsLib/AutoGen/6 멀티 Action_System_03.ds"] "6 멀티 Action_System_03"; 
    [device file="./dsLib/AutoGen/6 멀티 Action_System_04.ds"] "6 멀티 Action_System_04"; 
    [device file="./dsLib/AutoGen/7 멀티 Action Skip I/O_SystemB_01.ds"] "7 멀티 Action Skip I/O_SystemB_01"; 
    [device file="./dsLib/AutoGen/7 멀티 Action Skip I/O_SystemB_02.ds"] "7 멀티 Action Skip I/O_SystemB_02"; 
    [device file="./dsLib/AutoGen/7 멀티 Action Skip I/O_SystemB_03.ds"] "7 멀티 Action Skip I/O_SystemB_03"; 
    [device file="./dsLib/AutoGen/7 멀티 Action Skip I/O_SystemB_04.ds"] "7 멀티 Action Skip I/O_SystemB_04"; 
    [device file="./dsLib/AutoGen/9 Action 출력 옵션_System1.ds"] "9 Action 출력 옵션_System1"; 
    [device file="./dsLib/AutoGen/4 행위 (Action) 타입 유닛_System1.ds"] "4 행위 (Action) 타입 유닛_System1"; 
    [device file="./dsLib/AutoGen/11 외부 행위 (Action) 배치_System1.ds"] "11 외부 행위 (Action) 배치_System1"; 
    [device file="./dsLib/AutoGen/16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)_System1.ds"] "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)_System1"; 
    [device file="./dsLib/AutoGen/10 Action 설정 값_System1.ds"] "10 Action 설정 값_System1"; 
    [device file="./dsLib/AutoGen/2 행위 (Action) 배치 유닛_Device.ds"] "2 행위 (Action) 배치 유닛_Device"; 
    [device file="./dsLib/AutoGen/5 시스템 인터페이스 유닛_Device1.ds"] "5 시스템 인터페이스 유닛_Device1"; 
}
//DS Language Version = [1.0.0.1]
//DS Library Date = [Library Release Date 24.3.26]
//DS Engine Version = [0.9.8.36]
"""

    let CpuTestText =
        """
[sys] HelloDS = {
    [flow] STN2 = {
        Work1 = {
            Device11111.ADV > Device11111.RET > Device11111_ADV_1 > Device11111_RET_1;
        }
        [aliases] = {
            Work1.Device11111.ADV = { Device11111_ADV_1; }
            Work1.Device11111.RET = { Device11111_RET_1; }
        }
    }
    [flow] "77" = {
        "88"."3".ADV.INTrue > Work1 > "88_Work1";
        Work2 > "88".Work1 > Work1_1;
        Work2 = {
            "3".ADV; 
        }
        Work1 = {
            "1".ADV > "2".ADV > "1_ADV_1";
        }
        [aliases] = {
            "88".Work1 = { "88_Work1"; }
            Work1 = { Work1_1; }
            Work1."1".ADV = { "1_ADV_1"; }
        }
    }
    [flow] STN33 = {
        Work1 = {
            BB.ADV > BB.RET > AA.ADV > AA.RET;
        }
    }
    [flow] "88" = {
        Work1 = {
            "1".ADV > "2".ADV;
        }
        Work2 = {
            "3".ADV; 
        }
    }
    [flow] STN3 = {
        STN2.Device11111.ADV.INTrue > Work1;
    }
    [flow] STN11 = {
        Work1 => Work2 => Work1;
        외부시작.ADV.INTrue > Work1_1;
        Work1 = {
            Device1.ADV > Device1_ADV_1;
        }
        [aliases] = {
            Work1 = { Work1_1; }
            Work1.Device1.ADV = { Device1_ADV_1; }
        }
    }
    [flow] STN1 = {
        Work1 => Work2 => Work1;
        외부시작.ADV.INTrue > Work1_1;
        Work1 = {
            Device1.ADV > Device2.ADV > Device3.ADV > Device4.ADV > Device1.RET, Device2.RET, Device3.RET > Device4.RET;
        }
        [aliases] = {
            Work1 = { Work1_1; }
        }
    }
    [jobs] = {
        STN11.외부시작.ADV.INTrue = { STN11_외부시작.ADV(_:Boolean:True, _); }
        STN1.외부시작.ADV.INTrue = { STN1_외부시작.ADV(_:Boolean:True, _); }
        STN2.Device11111.ADV.INTrue[N3(2, 2)] = { STN2_Device11111_01.ADV(_:Boolean:True, _); STN2_Device11111_02.ADV(_:Boolean:True, _); STN2_Device11111_03.ADV(_:Boolean:True, _); }
        "88"."3".ADV.INTrue[N4(4, 4)] = { "88_3_01".ADV(_:Boolean:True, _); "88_3_02".ADV(_:Boolean:True, _); "88_3_03".ADV(_:Boolean:True, _); "88_3_04".ADV(_:Boolean:True, _); }
        "77"."2".ADV = { "77_2".ADV(_, _); }
        "77"."1".ADV = { "77_1".ADV(_, _); }
        STN1.Device1.ADV = { STN1_Device1.ADV(_, _); }
        STN1.Device2.ADV = { STN1_Device2.ADV(_, _); }
        STN1.Device3.ADV = { STN1_Device3.ADV(_, _); }
        "88"."1".ADV = { "88_1".ADV(_, _); }
        "88"."2".ADV = { "88_2".ADV(_, _); }
        STN2.Device11111.ADV[N3(2, 2)] = { STN2_Device11111_01.ADV(_, _); STN2_Device11111_02.ADV(_, _); STN2_Device11111_03.ADV(_, _); }
        STN1.Device4.ADV = { STN1_Device4.ADV(_, _); }
        STN11.Device1.ADV = { STN11_Device1.ADV(_, _); }
        STN33.AA.ADV = { STN33_AA.ADV(_, _); }
        STN33.BB.ADV = { STN33_BB.ADV(_, _); }
        STN1.Device1.RET = { STN1_Device1.RET(_, _); }
        STN1.Device2.RET = { STN1_Device2.RET(_, _); }
        STN1.Device3.RET = { STN1_Device3.RET(_, _); }
        "88"."3".ADV[N4(1, 0)] = { "88_3_01".ADV(_, _); "88_3_02".ADV(_, _); "88_3_03".ADV(_, _); "88_3_04".ADV(_, _); }
        STN33.AA.RET = { STN33_AA.RET(_, _); }
        "77"."3".ADV = { "77_3".ADV(_, _); }
        STN2.Device11111.RET[N3(3, 1)] = { STN2_Device11111_01.RET(_, _); STN2_Device11111_02.RET(_, _); STN2_Device11111_03.RET(_, _); }
        STN33.BB.RET = { STN33_BB.RET(_, _); }
        STN1.Device4.RET = { STN1_Device4.RET(_, _); }
    }
    [interfaces] = {
        Api1 = { "77".Work1 ~ "77".Work2 }
        Api2 = { "88".Work1 ~ "88".Work1 }
    }
    [buttons] = {
        [a] = { AutoSelect(_, -) = { STN2; "77"; STN33; "88"; STN3; STN11; STN1; } }
        [m] = { ManualSelect(_, -) = { STN2; "77"; STN33; "88"; STN3; STN11; STN1; } }
        [d] = { DrivePushBtn(_, -) = { STN2; "77"; STN33; "88"; STN3; STN11; STN1; } }
        [e] = { EmergencyBtn(_, -) = { STN2; "77"; STN33; "88"; STN3; STN11; STN1; } }
        [p] = { PausePushBtn(_, -) = { STN2; "77"; STN33; "88"; STN3; STN11; STN1; } }
        [c] = { ClearPushBtn(_, -) = { STN2; "77"; STN33; "88"; STN3; STN11; STN1; } }
    }
    [lamps] = {
        [a] = { AutoModeLamp(-, _) = {  } }
        [m] = { ManualModeLamp(-, _) = {  } }
        [d] = { DriveLamp(-, _) = {  } }
        [e] = { ErrorLamp(-, _) = {  } }
        [r] = { ReadyStateLamp(-, _) = {  } }
        [i] = { IdleModeLamp(-, _) = {  } }
        [o] = { OriginStateLamp(-, _) = {  } }
    }
    [prop] = {
        [safety] = {
            "77".Work1."1".ADV = { "77"."3".ADV; }
        }
        [autopre] = {
            STN2.Work1.Device11111.ADV = { STN2.Device11111.RET; }
        }
        [layouts] = {
            STN11_외부시작 = (1103, 95, 220, 80);
            STN1_외부시작 = (1103, 95, 220, 80);
            STN2_Device11111_01 = (1497, 134, 220, 80);
            STN2_Device11111_02 = (1497, 134, 220, 80);
            STN2_Device11111_03 = (1497, 134, 220, 80);
            "88_3_01" = (375, 630, 220, 80);
            "88_3_02" = (375, 630, 220, 80);
            "88_3_03" = (375, 630, 220, 80);
            "88_3_04" = (375, 630, 220, 80);
            "77_2" = (772, 273, 220, 80);
            "77_1" = (991, 346, 220, 80);
            STN1_Device1 = (554, 580, 220, 80);
            STN1_Device2 = (854, 580, 220, 80);
            STN1_Device3 = (1154, 580, 220, 80);
            "88_1" = (620, 300, 220, 80);
            "88_2" = (920, 300, 220, 80);
            STN1_Device4 = (854, 800, 220, 80);
            STN11_Device1 = (843, 460, 220, 80);
            STN33_AA = (1102, 645, 220, 80);
            STN33_BB = (914, 790, 220, 80);
            "77_3" = (334, 676, 220, 80);
        }
        [disable] = {
            STN2.Work1."Device11111.RET";
        }
    }
    [device file="./dsLib/Cylinder/DoubleCylinder.ds"] 
        STN11_외부시작,
        STN1_외부시작,
        STN2_Device11111_01,
        STN2_Device11111_02,
        STN2_Device11111_03,
        "88_3_01",
        "88_3_02",
        "88_3_03",
        "88_3_04",
        "77_2",
        "77_1",
        STN1_Device1,
        STN1_Device2,
        STN1_Device3,
        "88_1",
        "88_2",
        STN1_Device4,
        STN11_Device1,
        STN33_AA,
        STN33_BB,
        "77_3"; 
}
//DS Language Version = [1.0.0.1]
//DS Library Date = [Library Release Date 24.3.26]
//DS Engine Version = [0.9.8.36]
"""

    let CodeElementsText =
        """
[sys] My = {
    [flow] F = {
        Seg1;
    }

    [variables] = { //이름 = (타입,초기값)
        R100 = (word, 0)
        R101 = (word, 0)
        R102 = (word, 5)
        R103 = (dword, 0)
        PI = (float, 3.1415)
    }


}

"""

    let DuplicatedEdgesText =
        """
[sys] B = {
    [flow] F = {
        Vp > Pp;
        Vp |> Pp;
    }
}
"""

  

    let CausalsText =
        """
[sys] L = {
    [flow] F = {
        Main = {
            A.p > A.m > B.p > B.m;
        }
    }
    [jobs] = {
        F.A.p = { A."+"(%I1, %Q1); }
        F.A.m = { A."-"(%I2, %Q2); }
        F.B.p = { B."+"(%I3, %Q3); }
        F.B.m = { B."-"(%I4, %Q4); }
    }
    [device file="cylinder.ds"]
    A,
    B;
}
"""

    let AdoptoedValidText =
        """
[sys] My = {
    [flow] F = {
        Seg1 > Seg2;		// Seg1(Real)> Seg2(Real);
        Seg1 = {
            A.p > A.m;		// A.p(Call)> A.m(Call);
        }
    }
    [flow] F2 = {
        F.Seg1 > Seg;		// F.Seg1(Alias)> Seg(Real);
        Seg = {
            A.p > A.m;		// A.p(Call)> A.m(Call);
        }
    }
    [jobs] = {
        F.A.p = { A."+"(%I1, %Q1); }
        F.A.m = { A."-"(%I2, %Q2); }
        F2.A.p = { A2."+"(%I1, %Q1); }
        F2.A.m = { A2."-"(%I2, %Q2); }
    }
    [device file="cylinder.ds"] 
        A,
        A2; 
}

"""

    let SimpleLoadedDeviceText =
        """
[sys] My = {
    [flow] F = {
        Seg1 > Seg2;		// Seg1(Real)> Seg2(Real);
        Seg1 = {
            F.p > F.m;		// F.p(Call)> F.m(Call);
        }
    }
    [flow] F2 = {
        F.Seg1 > Seg;		// F.Seg1(Alias)> Seg(Real);
        Seg = {
            F.p > F.m;		// F.p(Call)> F.m(Call);
        }
    }
    [jobs] = {
        F.F.p = { F."+"(%I1, %Q1); }
        F.F.m = { F."-"(%I2, %Q2); }
        F2.F.p = { F2."+"(%I1, %Q1); }
        F2.F.m = { F2."-"(%I2, %Q2); }
    }
    [device file="cylinder.ds"] 
        F,
        F2; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExA.mple/dsSimple/cylinder.ds
}

"""

    let SplittedMRIEdgesText =
        """
[sys] A = {
    [flow] F = {
        a3 <|> a4;
        a1 <|> a2 |> a3 |> a2;
        a1 > a2 > a3 > a4;
    }
    [interfaces] = {
        I1 = { F.a1 ~ F.a2 }
        I2 = { F.a2 ~ F.a3 }
        I3 = { F.a3 ~ F.a1 }
        I1 <|> I2;
        I1 <|> I3;
        I1 <|> I4;
        I2 <|> I3;
        I2 <|> I4;
        I3 <|> I4;
    }
}
"""

    let PptGeneratedText =
        """
[sys] SIDE_QTR_Handling = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <|> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <|> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <|> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
    [interfaces] = {
        "SIDE_QTR_Handling.LOADING1" = { _ ~ _ }
        "SIDE_QTR_Handling.LOADING2" = { _ ~ _ }
    }
}
[sys] Pin = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "UP [Vp ~ Sp]" <|> "DOWN[Vm ~ Sm]";
    }
}
[sys] MY = {
    [flow] S101 = {
        "S101_Handling.LOADING1" > "S101_F_A.pRON_U131.ADV" > "S101_F_A.pRON_P132_134Unit.UP" > "S101_F_A.pRON.CLA.mP" > "S101_Handling.LOADING2" > "S101_DASH_P112.UP" > "S101_DASH_P114.LATCH" > "S101_DASH.CLA.mP" > "S101_Weld.WELDING" > "S101_DASH.UNCLA.mP" > "S101_DASH_P114.UNLATCH" > "S101_DASH_U111_1.UNLATCH" > "S101_Handling.UNLOADING" > "S101_F_A.pRON_P104.UP";
        "S101_DASH_P114.UNLATCH" > "S101_DASH_U115.UNLATCH" > "S101_Handling.UNLOADING";
        "S101_DASH_P114.UNLATCH" > "S101_DASH_UNIT.RET" > "S101_Handling.UNLOADING";
        "S101_DASH.UNCLA.mP" > "S101_F_A.pRON_P132_134Unit.DOWN" > "S101_DASH_U111_1.UNLATCH";
        "S101_F_A.pRON_P132_134Unit.DOWN" > "S101_DASH_U115.UNLATCH";
        "S101_F_A.pRON_P132_134Unit.DOWN" > "S101_DASH_UNIT.RET";
        "S101_DASH.UNCLA.mP" > "S101_F_A.pRON_U131.RET" > "S101_DASH_U111_1.UNLATCH";
        "S101_F_A.pRON_U131.RET" > "S101_DASH_U115.UNLATCH";
        "S101_F_A.pRON_U131.RET" > "S101_DASH_UNIT.RET";
        "S101_Weld.WELDING" > "S101_DASH_P112.DOWN" > "S101_DASH_P114.UNLATCH";
        "S101_DASH_P112.DOWN" > "S101_F_A.pRON_P132_134Unit.DOWN";
        "S101_DASH_P112.DOWN" > "S101_F_A.pRON_U131.RET";
        "S101_Weld.WELDING" > "S101_F_A.pRON.UNCLA.mP" > "S101_DASH_P114.UNLATCH";
        "S101_F_A.pRON.UNCLA.mP" > "S101_F_A.pRON_P132_134Unit.DOWN";
        "S101_F_A.pRON.UNCLA.mP" > "S101_F_A.pRON_U131.RET";
        "S101_Weld.WELDING" > "S101_F_A.pRON_P104.DOWN" > "S101_DASH_P114.UNLATCH";
        "S101_F_A.pRON_P104.DOWN" > "S101_F_A.pRON_P132_134Unit.DOWN";
        "S101_F_A.pRON_P104.DOWN" > "S101_F_A.pRON_U131.RET";
        "S101_Weld.WELDING" > "S101_F_A.pRON_U133.UNCLA.mP" > "S101_DASH_P114.UNLATCH";
        "S101_F_A.pRON_U133.UNCLA.mP" > "S101_F_A.pRON_P132_134Unit.DOWN";
        "S101_F_A.pRON_U133.UNCLA.mP" > "S101_F_A.pRON_U131.RET";
        "S101_DASH_P114.LATCH" > "S101_DASH_U111_1.LATCH" > "S101_Weld.WELDING";
        "S101_Handling.LOADING2" > "S101_DASH_U115.LATCH" > "S101_DASH_P114.LATCH";
        "S101_Handling.LOADING2" > "S101_DASH_UNIT.ADV" > "S101_DASH_P114.LATCH";
        "S101_F_A.pRON_P132_134Unit.UP" > "S101_F_A.pRON_U133.CLA.mP" > "S101_Handling.LOADING2";
    }
    [flow] SIDE_REINF = {
        "#201-1" = {
            SIDE_REINF_Handling."SIDE_REINF_Handling.LOADING1" > SIDE_REINF_REINF_Shift."SIDE_REINF_REINF_Shift.ADV" > SIDE_REINF_REINF_Pin."SIDE_REINF_REINF_Pin.UP" > SIDE_REINF_Handling."SIDE_REINF_Handling.LOADING2" > SIDE_REINF_REINF1_ClA.mp."SIDE_REINF_REINF1_ClA.mp.CLA.mP" > SIDE_REINF_Weld."SIDE_REINF_Weld.WELDING" > SIDE_REINF_REINF1_ClA.mp."SIDE_REINF_REINF1_ClA.mp.UNCLA.mP";
            SIDE_REINF_Weld."SIDE_REINF_Weld.WELDING" > SIDE_REINF_REINF2_ClA.mp."SIDE_REINF_REINF2_ClA.mp.UNCLA.mP";
            SIDE_REINF_Weld."SIDE_REINF_Weld.WELDING" > SIDE_REINF_REINF_Pin."SIDE_REINF_REINF_Pin.DOWN";
            SIDE_REINF_Weld."SIDE_REINF_Weld.WELDING" > SIDE_REINF_REINF_Shift."SIDE_REINF_REINF_Shift.RET";
            SIDE_REINF_Handling."SIDE_REINF_Handling.LOADING2" > SIDE_REINF_REINF2_ClA.mp."SIDE_REINF_REINF2_ClA.mp.CLA.mP" > SIDE_REINF_Weld."SIDE_REINF_Weld.WELDING";
        }
    }
    [flow] SIDE_QTR = {
        "SIDE_MAIN.#205" > "#205-1" > "#205-2";
        "#205-1" = {
            SIDE_QTR_Handling."SIDE_QTR_Handling.LOADING1" > SIDE_QTR_REINF_Shift."SIDE_QTR_REINF_Shift.ADV" > SIDE_QTR_REINF_Pin."SIDE_QTR_REINF_Pin.UP" > SIDE_QTR_Handling."SIDE_QTR_Handling.LOADING2" > SIDE_QTR_REINF1_ClA.mp."SIDE_QTR_REINF1_ClA.mp.CLA.mP" > SIDE_QTR_Weld."SIDE_QTR_Weld.WELDING" > SIDE_QTR_REINF1_ClA.mp."SIDE_QTR_REINF1_ClA.mp.UNCLA.mP";
            SIDE_QTR_Weld."SIDE_QTR_Weld.WELDING" > SIDE_QTR_REINF2_ClA.mp."SIDE_QTR_REINF2_ClA.mp.UNCLA.mP";
            SIDE_QTR_Weld."SIDE_QTR_Weld.WELDING" > SIDE_QTR_REINF_Pin."SIDE_QTR_REINF_Pin.DOWN";
            SIDE_QTR_Weld."SIDE_QTR_Weld.WELDING" > SIDE_QTR_REINF_Shift."SIDE_QTR_REINF_Shift.RET";
            SIDE_QTR_Handling."SIDE_QTR_Handling.LOADING2" > SIDE_QTR_REINF2_ClA.mp."SIDE_QTR_REINF2_ClA.mp.CLA.mP" > SIDE_QTR_Weld."SIDE_QTR_Weld.WELDING";
        }
        "#205-2" = {
            SIDE_QTR_Handling."SIDE_QTR_Handling.LOADING1" > SIDE_QTR_REINF_Shift."SIDE_QTR_REINF_Shift.ADV" > SIDE_QTR_REINF_Pin."SIDE_QTR_REINF_Pin.UP" > SIDE_QTR_Handling."SIDE_QTR_Handling.LOADING2" > SIDE_QTR_REINF1_ClA.mp."SIDE_QTR_REINF1_ClA.mp.CLA.mP" > SIDE_QTR_Weld."SIDE_QTR_Weld.WELDING" > SIDE_QTR_REINF1_ClA.mp."SIDE_QTR_REINF1_ClA.mp.UNCLA.mP";
            SIDE_QTR_Weld."SIDE_QTR_Weld.WELDING" > SIDE_QTR_REINF2_ClA.mp."SIDE_QTR_REINF2_ClA.mp.UNCLA.mP";
            SIDE_QTR_Weld."SIDE_QTR_Weld.WELDING" > SIDE_QTR_REINF_Pin."SIDE_QTR_REINF_Pin.DOWN";
            SIDE_QTR_Weld."SIDE_QTR_Weld.WELDING" > SIDE_QTR_REINF_Shift."SIDE_QTR_REINF_Shift.RET";
            SIDE_QTR_Handling."SIDE_QTR_Handling.LOADING2" > SIDE_QTR_REINF2_ClA.mp."SIDE_QTR_REINF2_ClA.mp.CLA.mP" > SIDE_QTR_Weld."SIDE_QTR_Weld.WELDING";
        }
    }
    [flow] SIDE_MAIN = {
        "SIDE_REINF.#201-1" > "#201" > "#202" > "#205" > "SIDE_QTR.#205-2" > "#206";
        "#201" = {
            SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING1" > SIDE_MAIN_REINF_Shift."SIDE_MAIN_REINF_Shift.ADV" > SIDE_MAIN_REINF_Pin."SIDE_MAIN_REINF_Pin.UP" > SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING2" > SIDE_MAIN_REINF1_ClA.mp."SIDE_MAIN_REINF1_ClA.mp.CLA.mP" > SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF1_ClA.mp."SIDE_MAIN_REINF1_ClA.mp.UNCLA.mP";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF2_ClA.mp."SIDE_MAIN_REINF2_ClA.mp.UNCLA.mP";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF_Pin."SIDE_MAIN_REINF_Pin.DOWN";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF_Shift."SIDE_MAIN_REINF_Shift.RET";
            SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING2" > SIDE_MAIN_REINF2_ClA.mp."SIDE_MAIN_REINF2_ClA.mp.CLA.mP" > SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING";
        }
        "#202" = {
            SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING1" > SIDE_MAIN_REINF_Shift."SIDE_MAIN_REINF_Shift.ADV" > SIDE_MAIN_REINF_Pin."SIDE_MAIN_REINF_Pin.UP" > SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING2" > SIDE_MAIN_REINF1_ClA.mp."SIDE_MAIN_REINF1_ClA.mp.CLA.mP" > SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF1_ClA.mp."SIDE_MAIN_REINF1_ClA.mp.UNCLA.mP";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF2_ClA.mp."SIDE_MAIN_REINF2_ClA.mp.UNCLA.mP";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF_Pin."SIDE_MAIN_REINF_Pin.DOWN";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF_Shift."SIDE_MAIN_REINF_Shift.RET";
            SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING2" > SIDE_MAIN_REINF2_ClA.mp."SIDE_MAIN_REINF2_ClA.mp.CLA.mP" > SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING";
        }
        "#206" = {
            SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING1" > SIDE_MAIN_REINF_Shift."SIDE_MAIN_REINF_Shift.ADV" > SIDE_MAIN_REINF_Pin."SIDE_MAIN_REINF_Pin.UP" > SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING2" > SIDE_MAIN_REINF1_ClA.mp."SIDE_MAIN_REINF1_ClA.mp.CLA.mP" > SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF1_ClA.mp."SIDE_MAIN_REINF1_ClA.mp.UNCLA.mP";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF2_ClA.mp."SIDE_MAIN_REINF2_ClA.mp.UNCLA.mP";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF_Pin."SIDE_MAIN_REINF_Pin.DOWN";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF_Shift."SIDE_MAIN_REINF_Shift.RET";
            SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING2" > SIDE_MAIN_REINF2_ClA.mp."SIDE_MAIN_REINF2_ClA.mp.CLA.mP" > SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING";
        }
        "#205" = {
            SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING1" > SIDE_MAIN_REINF_Shift."SIDE_MAIN_REINF_Shift.ADV" > SIDE_MAIN_REINF_Pin."SIDE_MAIN_REINF_Pin.UP" > SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING2" > SIDE_MAIN_REINF1_ClA.mp."SIDE_MAIN_REINF1_ClA.mp.CLA.mP" > SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF1_ClA.mp."SIDE_MAIN_REINF1_ClA.mp.UNCLA.mP";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF2_ClA.mp."SIDE_MAIN_REINF2_ClA.mp.UNCLA.mP";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF_Pin."SIDE_MAIN_REINF_Pin.DOWN";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF_Shift."SIDE_MAIN_REINF_Shift.RET";
            SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING2" > SIDE_MAIN_REINF2_ClA.mp."SIDE_MAIN_REINF2_ClA.mp.CLA.mP" > SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING";
        }
    }
}
[sys] S101_F_A.pRON_P104 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "UP [Vp ~ Sp]" <|> "DOWN[Vm ~ Sm]";
    }
}
[sys] SIDE_MAIN_REINF_Pin = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "UP [Vp ~ Sp]" <|> "DOWN[Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_MAIN_REINF_Pin.UP" = { _ ~ _ }
        "SIDE_MAIN_REINF_Pin.DOWN" = { _ ~ _ }
    }
}
[sys] S101_F_A.pRON_U133 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLA.mP [Vp ~ Sp]" <|> "UNCLA.mP [Vm ~ Sm]";
    }
}
[sys] SIDE_REINF_Weld = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <|> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <|> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <|> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
    [interfaces] = {
        "SIDE_REINF_Weld.WELDING" = { _ ~ _ }
    }
}
[sys] SIDE_MAIN_Handling = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <|> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <|> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <|> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
    [interfaces] = {
        "SIDE_MAIN_Handling.LOADING2" = { _ ~ _ }
        "SIDE_MAIN_Handling.LOADING1" = { _ ~ _ }
    }
}
[sys] S101_F_A.pRON_U131 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "ADV [Vp ~ Sp]" <|> "RET[Vm ~ Sm]";
    }
}
[sys] SIDE_REINF_REINF2_ClA.mp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLA.mP [Vp ~ Sp]" <|> "UNCLA.mP [Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_REINF_REINF2_ClA.mp.CLA.mP" = { _ ~ _ }
        "SIDE_REINF_REINF2_ClA.mp.UNCLA.mP" = { _ ~ _ }
    }
}
[sys] SIDE_REINF_Handling = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <|> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <|> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <|> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
    [interfaces] = {
        "SIDE_REINF_Handling.LOADING2" = { _ ~ _ }
        "SIDE_REINF_Handling.LOADING1" = { _ ~ _ }
    }
}
[sys] SIDE_MAIN_REINF1_ClA.mp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLA.mP [Vp ~ Sp]" <|> "UNCLA.mP [Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_MAIN_REINF1_ClA.mp.CLA.mP" = { _ ~ _ }
        "SIDE_MAIN_REINF1_ClA.mp.UNCLA.mP" = { _ ~ _ }
    }
}
[sys] SIDE_QTR_REINF1_ClA.mp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLA.mP [Vp ~ Sp]" <|> "UNCLA.mP [Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_QTR_REINF1_ClA.mp.CLA.mP" = { _ ~ _ }
        "SIDE_QTR_REINF1_ClA.mp.UNCLA.mP" = { _ ~ _ }
    }
}
[sys] S101_DASH_P114 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "LATCH[Vp ~ Sp]" <|> "UNLATCH [Vm ~ Sm]";
    }
}
[sys] S101_DASH_U111_1 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "LATCH[Vp ~ Sp]" <|> "UNLATCH [Vm ~ Sm]";
    }
}
[sys] Shift = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "ADV [Vp ~ Sp]" <|> "RET[Vm ~ Sm]";
    }
}
[sys] SIDE_QTR_REINF_Shift = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "ADV [Vp ~ Sp]" <|> "RET[Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_QTR_REINF_Shift.ADV" = { _ ~ _ }
        "SIDE_QTR_REINF_Shift.RET" = { _ ~ _ }
    }
}
[sys] S101_F_A.pRON_P132_134Unit = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "UP [Vp ~ Sp]" <|> "DOWN[Vm ~ Sm]";
    }
}
[sys] Robot = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <|> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <|> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <|> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
}
[sys] SIDE_MAIN_REINF2_ClA.mp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLA.mP [Vp ~ Sp]" <|> "UNCLA.mP [Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_MAIN_REINF2_ClA.mp.CLA.mP" = { _ ~ _ }
        "SIDE_MAIN_REINF2_ClA.mp.UNCLA.mP" = { _ ~ _ }
    }
}
[sys] S101_DASH = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLA.mP [Vp ~ Sp]" <|> "UNCLA.mP [Vm ~ Sm]";
    }
}
[sys] S101_DASH_U115 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "LATCH[Vp ~ Sp]" <|> "UNLATCH [Vm ~ Sm]";
    }
}
[sys] S101_DASH_P112 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "UP [Vp ~ Sp]" <|> "DOWN[Vm ~ Sm]";
    }
}
[sys] S101_Handling = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <|> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <|> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <|> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
}
[sys] SIDE_QTR_REINF_Pin = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "UP [Vp ~ Sp]" <|> "DOWN[Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_QTR_REINF_Pin.UP" = { _ ~ _ }
        "SIDE_QTR_REINF_Pin.DOWN" = { _ ~ _ }
    }
}
[sys] S101_DASH_UNIT = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "ADV [Vp ~ Sp]" <|> "RET[Vm ~ Sm]";
    }
}
[sys] SIDE_REINF_REINF_Shift = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "ADV [Vp ~ Sp]" <|> "RET[Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_REINF_REINF_Shift.ADV" = { _ ~ _ }
        "SIDE_REINF_REINF_Shift.RET" = { _ ~ _ }
    }
}
[sys] SIDE_QTR_Weld = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <|> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <|> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <|> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
    [interfaces] = {
        "SIDE_QTR_Weld.WELDING" = { _ ~ _ }
    }
}
[sys] Latch = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "LATCH[Vp ~ Sp]" <|> "UNLATCH [Vm ~ Sm]";
    }
}
[sys] SIDE_REINF_REINF_Pin = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "UP [Vp ~ Sp]" <|> "DOWN[Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_REINF_REINF_Pin.UP" = { _ ~ _ }
        "SIDE_REINF_REINF_Pin.DOWN" = { _ ~ _ }
    }
}
[sys] SIDE_MAIN_Weld = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <|> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <|> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <|> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
    [interfaces] = {
        "SIDE_MAIN_Weld.WELDING" = { _ ~ _ }
    }
}
[sys] SIDE_MAIN_REINF_Shift = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "ADV [Vp ~ Sp]" <|> "RET[Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_MAIN_REINF_Shift.ADV" = { _ ~ _ }
        "SIDE_MAIN_REINF_Shift.RET" = { _ ~ _ }
    }
}
[sys] S101_F_A.pRON = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLA.mP [Vp ~ Sp]" <|> "UNCLA.mP [Vm ~ Sm]";
    }
}
[sys] S101_Weld = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <|> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <|> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <|> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
}
[sys] SIDE_REINF_REINF1_ClA.mp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLA.mP [Vp ~ Sp]" <|> "UNCLA.mP [Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_REINF_REINF1_ClA.mp.CLA.mP" = { _ ~ _ }
        "SIDE_REINF_REINF1_ClA.mp.UNCLA.mP" = { _ ~ _ }
    }
}
[sys] SIDE_QTR_REINF2_ClA.mp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLA.mP [Vp ~ Sp]" <|> "UNCLA.mP [Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_QTR_REINF2_ClA.mp.CLA.mP" = { _ ~ _ }
        "SIDE_QTR_REINF2_ClA.mp.UNCLA.mP" = { _ ~ _ }
    }
}
[sys] ClA.mp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLA.mP [Vp ~ Sp]" <|> "UNCLA.mP [Vm ~ Sm]";
    }
}
"""

    let RecursiveSystemText =
        """
[sys] P = {
    [sys] P1 = {
        [flow] F = {
            Vp > Vm;
        }
    }

    [sys] P2 = {
        [flow] F = {
            Vp > Vm;
        }
    }
}
"""

    let Ppt20221213Text =
        """
[sys] FactoryIO = {
    [flow] A.pI = {
    }
    [flow] Line = {
        Clear |> Clear |> Copy1_Assy반출 |> Green공급및고정;		// Clear(Real)|> Clear(Real) |> Copy1_Assy반출(Alias) |> Green공급및고정(Real);
        "Line_Robot_X+" > 로봇조립Sub;		// "Line_Robot_X+"(Call)> 로봇조립Sub(Real);
        "Line_Robot_X-" > 로봇조립Main;		// "Line_Robot_X-"(Call)> 로봇조립Main(Real);
        Blue공급및고정 => 로봇조립Sub => Assy반출;		// Blue공급및고정(Real)=> 로봇조립Sub(Real) => Assy반출(Real);
        GoSub => Blue공급및고정 => Assy반출;		// GoSub(Real)=> Blue공급및고정(Real) => Assy반출(Real);
        GoMain => Green공급및고정 => 로봇조립Main => 로봇조립Sub;		// GoMain(Real)=> Green공급및고정(Real) => 로봇조립Main(Real) => 로봇조립Sub(Real);
        Assy반출 = {
            "Line_Assy_Upper+" > Line_Sub_PartOff;		// "Line_Assy_Upper+"(Call)> Line_Sub_PartOff(Call);
            "Line_Assy_Upper+" > "Line_Assy_UpConv+" > "Line_Assy_Upper-" > "Line_Assy_DownConv+";		// "Line_Assy_Upper+"(Call)> "Line_Assy_UpConv+"(Call) > "Line_Assy_Upper-"(Call) > "Line_Assy_DownConv+"(Call);
        }
        Green공급및고정 = {
            "Line_Main_Conv+" > "Line_Main_ClA.mp+" > "Line_Main_ClA.mp-";		// "Line_Main_Conv+"(Call)> "Line_Main_ClA.mp+"(Call) > "Line_Main_ClA.mp-"(Call);
        }
        Blue공급및고정 = {
            "Line_Sub_Conv+" > "Line_Sub_ClA.mp+" > "Line_Sub_ClA.mp-";		// "Line_Sub_Conv+"(Call)> "Line_Sub_ClA.mp+"(Call) > "Line_Sub_ClA.mp-"(Call);
        }
        로봇조립Sub = {
            "Line_Robot_Grab-" > "Line_Robot_Z-";		// "Line_Robot_Grab-"(Call)> "Line_Robot_Z-"(Call);
            "Line_Robot_Z+" > "Line_Robot_Grab-" > "Line_Robot_X-";		// "Line_Robot_Z+"(Call)> "Line_Robot_Grab-"(Call) > "Line_Robot_X-"(Call);
        }
        로봇조립Main = {
            "Line_Robot_Grab+" > Line_Main_PartOff;		// "Line_Robot_Grab+"(Call)> Line_Main_PartOff(Call);
            "Line_Robot_Grab+" > "Line_Robot_X+";		// "Line_Robot_Grab+"(Call)> "Line_Robot_X+"(Call);
            "Line_Robot_Z+" > "Line_Robot_Grab+" > "Line_Robot_Z-";		// "Line_Robot_Z+"(Call)> "Line_Robot_Grab+"(Call) > "Line_Robot_Z-"(Call);
        }
        [aliases] = {
            Assy반출 = { Copy1_Assy반출; }
        }
    }
    [jobs] = {
        "Line_Sub_Conv+" = { Line_Sub."Conv+"(_, _); }
        Line_Sub_PartOff = { Line_Sub.PartOff(_, _); }
        "Line_Sub_ClA.mp+" = { Line_Sub."ClA.mp+"(_, _); }
        "Line_Sub_ClA.mp-" = { Line_Sub."ClA.mp-"(_, _); }
        "Line_Robot_Grab+" = { Line_Robot."Grab+"(_, _); }
        "Line_Robot_Grab-" = { Line_Robot."Grab-"(_, _); }
        "Line_Robot_X-" = { Line_Robot."X-"(_, _); }
        "Line_Robot_Z+" = { Line_Robot."Z+"(_, _); }
        "Line_Robot_Z-" = { Line_Robot."Z-"(_, _); }
        "Line_Robot_X+" = { Line_Robot."X+"(_, _); }
        "Line_Assy_UpConv+" = { Line_Assy."UpConv+"(_, _); }
        "Line_Assy_DownConv+" = { Line_Assy."DownConv+"(_, _); }
        "Line_Assy_Upper+" = { Line_Assy."Upper+"(_, _); }
        "Line_Assy_Upper-" = { Line_Assy."Upper-"(_, _); }
        Line_Assy_JobClear = { Line_Assy.JobClear(_, _); }
        "Line_Main_Conv+" = { Line_Main."Conv+"(_, _); }
        Line_Main_PartOff = { Line_Main.PartOff(_, _); }
        "Line_Main_ClA.mp+" = { Line_Main."ClA.mp+"(_, _); }
        "Line_Main_ClA.mp-" = { Line_Main."ClA.mp-"(_, _); }
    }
    [interfaces] = {
        "JobClearClear~_" = { _ ~ _ }
        "StartMainGoMain~_" = { _ ~ _ }
        "StartSubGoSub~_" = { _ ~ _ }
    }
    [device file="Lib/Sub.ds"] Line_Sub; // C:\Users\kwak\Downloads\FactoryIO\Lib\Sub.pptx
    [device file="Lib/Robot.ds"] Line_Robot; // C:\Users\kwak\Downloads\FactoryIO\Lib\Robot.pptx
    [device file="Lib/Assy.ds"] Line_Assy; // C:\Users\kwak\Downloads\FactoryIO\Lib\Assy.pptx
    [device file="Lib/Main.ds"] Line_Main; // C:\Users\kwak\Downloads\FactoryIO\Lib\Main.pptx
}
"""


    let ParseNormal (text: string) =
        let systemRepo = ShareableSystemRepository()

        ModelParser.ParseFromString2(
            text,
            ParserOptions.Create4Simulation(systemRepo, ".", "ActiveCpuNA.me", None, DuNone)
        )
        |> ignore

        debugfn "Done"


    let Main (_args: string[]) =
        //ParseNormal(SplittedMRIEdgesText)
        //ParseNormal(DuplicatedEdgesText)
        //ParseNormal(AdoptoedValidText)
        //ParseNormal(AdoptoedA.mbiguousText)
        //ParseNormal(CodeElementsText)
        ParseNormal(EveryScenarioText)
    //ParseNormal(PptGeneratedText)

    let ReadAllInput (fn: string) = System.IO.File.ReadAllText(fn)
