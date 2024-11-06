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

[sys] DS_Units_V12 = {
    [flow] "시스템 모델링" = {
        "System B", "System A"; 
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
            RBT.투입, RBT.홈; 
        }
        드릴작업; 
    }
    [flow] "1 작업 및 행위 유닛" = {
        Device.Action1 > Work1;
        공급작업 = {
            RBT.투입, RBT.홈; 
        }
        드릴작업; 
    }
    [flow] "2 행위 (Action) 배치" = {
        #전원 > 드릴작업;
        공급작업 = {
            RBT.투입, RBT.홈; 
        }
    }
    [flow] "2 행위 (Action) 배치 유닛" = {
        Device.Action1 > Work1_1;
        #전원 > 드릴작업;
        Work1 = {
            Device.Action1, Device.Action2; 
        }
        공급작업 = {
            RBT.투입, RBT.홈; 
        }
        [aliases] = {
            Work1 = { Work1_1; }
        }
    }
    [flow] "3 작업 (Work) 타입" = {
        #전원 > 드릴작업;
        공급작업 = {
            RBT.투입, RBT.홈; 
        }
        Flow2, Flow1; 
    }
    [flow] "3 작업 (Work) 타입 유닛" = {
        #전원 > 드릴작업;
        공급작업 = {
            RBT.투입, RBT.홈; 
        }
        Work1, Flow2, Flow1, "3 작업 (Work) 타입_드릴작업"; 
        [aliases] = {
            "3 작업 (Work) 타입".드릴작업 = { "3 작업 (Work) 타입_드릴작업"; }
        }
    }
    [flow] "4 행위 (Action) 타입" = {
        공급작업 = {
            RBT.투입, RBT.홈; 
        }
        드릴작업 = {
            #전원CMD; 
        }
    }
    [flow] "4 행위 (Action) 타입 유닛" = {
        #전원 > 드릴작업;
        공급작업 = {
            RBT.투입, RBT.홈; 
        }
        드릴작업1 = {
            System1.Api1; 
        }
        내부Work = {
            #Action; 
        }
    }
    [flow] "5 시스템 인터페이스" = {
        드릴작업 = {
            드릴장치.드릴링A위치, 드릴장치.드릴링B위치; 
        }
        이동A, 드릴, 이동B; 
    }
    [flow] "5 시스템 인터페이스 유닛" = {
        Device1.Api1 > Work2;
        드릴작업 = {
            드릴장치.드릴링A위치, 드릴장치.드릴링B위치; 
        }
        Work1, 이동A, 드릴, 이동B; 
    }
    [flow] "기본 연결 Unit" = {
    }
    [flow] "1 기본 연결 Unit" = {
        Work1_1 |> Work2_1;
        Work1 > Work2;
        드릴작업 |> 공급작업 > 드릴작업;
        #전원 > 드릴작업;
        드릴작업 = {
            드릴장치.드릴링A위치, 드릴장치.드릴링B위치; 
        }
        공급작업 = {
            RBT.투입, RBT.홈; 
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
        #전원 > 드릴작업;
        드릴작업 = {
            드릴장치.드릴링A위치, 드릴장치.드릴링B위치; 
        }
        공급작업 = {
            RBT.투입, RBT.홈; 
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
        #전원 > 드릴작업;
        드릴작업 = {
            드릴장치.드릴링A위치, 드릴장치.드릴링B위치; 
        }
        공급작업 = {
            RBT.투입, RBT.홈; 
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
        #전원 > 드릴작업;
        드릴작업 = {
            드릴장치.드릴링A위치, 드릴장치.드릴링B위치; 
        }
        공급작업 = {
            RBT.투입, RBT.홈; 
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
        #전원 > 드릴작업;
        드릴작업 = {
            드릴장치.드릴링A위치, 드릴장치.드릴링B위치; 
        }
        공급작업 = {
            RBT.투입, RBT.홈; 
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
    [flow] "3 시스템 버튼 램프 확장" = {
        "System A"; 
    }
    [flow] "4 시스템 외부조건" = {
        "System A"; 
    }
    [flow] "4 시스템 외부조건 유닛" = {
        "System A"; 
    }
    [flow] "5 시스템 외부액션 유닛" = {
        "System A"; 
    }
    [flow] "7 시스템 외부액션 타겟 Value" = {
        "System A"; 
    }
    [flow] "4 Safety 조건" = {
        Work1 = {
            System1.Api1, System1.Api2; 
        }
        Work2 = {
            System1.Api1; 
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
            System1.Api1; 
        }
        Work2 = {
            System.Api; 
        }
    }
    [flow] "7 멀티 Action Skip IO" = {
        Work1 = {
            SystemA.Api; 
        }
        Work2 = {
            SystemB.Api2((230, 3214) : 500); 
        }
    }
    [flow] "8 Action 인터페이스 옵션" = {
        Work1 = {
            System1.Api1, System1.Api2; 
        }
        Work2 = {
            System1.Api3, System1.Api4; 
        }
    }
    [flow] "9 Action 출력 옵션" = {
        Work1 = {
            System1.Api1; 
        }
        Work2 = {
            System2.ADV, System2.RET; 
        }
    }
    [flow] "10 Action 설정 값" = {
        Work1 = {
            System1.Api1; 
        }
        Work2 = {
            System1.Api2(100 : 500); 
        }
    }
    [flow] "11 외부 행위 (Action) 배치" = {
        System1.Api3 > Work1_1;
        Work1 = {
            System1.Api1, System1.Api2; 
        }
        [aliases] = {
            Work1 = { Work1_1; }
        }
    }
    [flow] "12 내부 행위 (Action) 배치" = {
        #Action3 > Work1_1;
        Work1 = {
            #Action1, #Action2; 
        }
        [aliases] = {
            Work1 = { Work1_1; }
        }
    }
    [flow] "13 행위 사용 안함" = {
        Work1 = {
            Action."1", Action."2"; 
        }
        Work2 = {
            Action."1", Action."2"; 
        }
    }
    [flow] "14 Work 설정시간" = {
        Work1; 
    }
    [flow] "15 Work 데이터전송" = {
        Work1 => Work2 => Work4;
        Work1 > Work3 => Work4;
        Work1_1 => Work2_1 => Work4_1;
        Work1_1 > Work31 => Work4_1;
        [aliases] = {
            Work1 = { Work1_1; }
            Work2 = { Work2_1; }
            Work4 = { Work4_1; }
        }
    }
    [flow] "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)" = {
        Work1 = {
            System1.Api1 > System1.Api2;
        }
    }
    [flow] "IO Table" = {
    }
    [flow] "1 외부 주소" = {
        Work1 = {
            Device1.ADV > Device1.RET;
        }
    }
    [flow] "2 내부 변수_상수" = {
        #Operator > Work1;
        Work1 = {
            #Command; 
        }
    }
    [flow] "3 내부 연산_명령" = {
        #Operator > Work1;
        Work1 = {
            #Command2; 
        }
    }
    [flow] "4 버튼 IO" = {
    }
    [flow] "5 램프 IO" = {
    }
    [flow] "6 심볼 정의" = {
        #Operator3 > Work1;
        Work1 = {
            #Command3; 
        }
        Work2 = {
            Device1.ADV > Device1.RET(True : 300);
        }
    }
    [flow] "7 외부호출" = {
        Script1, Script2, MotionRET, MotionADV; 
    }
    [flow] "8 Error 설정시간" = {
        Work1 = {
            DEV1.ADV; 
        }
        Work2 = {
            DEV2.ADV; 
        }
        Work3 = {
            DEV3.ADV; 
        }
        Work4 = {
            DEV4.ADV, DEV4.RET; 
        }
    }
    [flow] "9 Negative 입력 옵션" = {
        Work1 = {
            System1.Api1(300 : 400); 
        }
        Work2 = {
            System1.Api1; 
        }
    }
    [jobs] = {
        "1 작업 및 행위".RBT.투입 = { "1 작업 및 행위__RBT".투입(P00007, P00047); }
        "1 작업 및 행위".RBT.홈 = { "1 작업 및 행위__RBT".홈(P00008, P00048); }
        "1 작업 및 행위 유닛".Device.Action1 = { "1 작업 및 행위 유닛__Device".Action1(P00004, -); }
        "1 작업 및 행위 유닛".RBT.투입 = { "1 작업 및 행위 유닛__RBT".투입(P00005, P00045); }
        "1 작업 및 행위 유닛".RBT.홈 = { "1 작업 및 행위 유닛__RBT".홈(P00006, P00046); }
        "2 행위 (Action) 배치".RBT.투입 = { "2 행위 (Action) 배치__RBT".투입(P00015, P00055); }
        "2 행위 (Action) 배치".RBT.홈 = { "2 행위 (Action) 배치__RBT".홈(P00016, P00056); }
        "2 행위 (Action) 배치 유닛".Device.Action1 = { "2 행위 (Action) 배치 유닛__Device".Action1(P00011, P00051); }
        "2 행위 (Action) 배치 유닛".Device.Action2 = { "2 행위 (Action) 배치 유닛__Device".Action2(P00012, P00052); }
        "2 행위 (Action) 배치 유닛".RBT.투입 = { "2 행위 (Action) 배치 유닛__RBT".투입(P00013, P00053); }
        "2 행위 (Action) 배치 유닛".RBT.홈 = { "2 행위 (Action) 배치 유닛__RBT".홈(P00014, P00054); }
        "3 작업 (Work) 타입".RBT.투입 = { "3 작업 (Work) 타입__RBT".투입(P0001D, P0005D); }
        "3 작업 (Work) 타입".RBT.홈 = { "3 작업 (Work) 타입__RBT".홈(P0001E, P0005E); }
        "3 작업 (Work) 타입 유닛".RBT.투입 = { "3 작업 (Work) 타입 유닛__RBT".투입(P0001B, P0005B); }
        "3 작업 (Work) 타입 유닛".RBT.홈 = { "3 작업 (Work) 타입 유닛__RBT".홈(P0001C, P0005C); }
        "4 행위 (Action) 타입".RBT.투입 = { "4 행위 (Action) 타입__RBT".투입(P00026, P00066); }
        "4 행위 (Action) 타입".RBT.홈 = { "4 행위 (Action) 타입__RBT".홈(P00027, P00067); }
        "4 행위 (Action) 타입 유닛".System1.Api1 = { "4 행위 (Action) 타입 유닛__System1".Api1(P00025, P00065); }
        "4 행위 (Action) 타입 유닛".RBT.투입 = { "4 행위 (Action) 타입 유닛__RBT".투입(P00023, P00063); }
        "4 행위 (Action) 타입 유닛".RBT.홈 = { "4 행위 (Action) 타입 유닛__RBT".홈(P00024, P00064); }
        "5 시스템 인터페이스".드릴장치.드릴링A위치 = { "5 시스템 인터페이스__드릴장치".드릴링A위치(P00031, P00071); }
        "5 시스템 인터페이스".드릴장치.드릴링B위치 = { "5 시스템 인터페이스__드릴장치".드릴링B위치(P00032, P00072); }
        "5 시스템 인터페이스 유닛".드릴장치.드릴링A위치 = { "5 시스템 인터페이스 유닛__드릴장치".드릴링A위치(P0002E, P0006E); }
        "5 시스템 인터페이스 유닛".드릴장치.드릴링B위치 = { "5 시스템 인터페이스 유닛__드릴장치".드릴링B위치(P0002F, P0006F); }
        "5 시스템 인터페이스 유닛".Device1.Api1 = { "5 시스템 인터페이스 유닛__Device1".Api1(P00030, -); }
        "1 기본 연결 Unit".드릴장치.드릴링A위치 = { "1 기본 연결 Unit__드릴장치".드릴링A위치(P00000, P00040); }
        "1 기본 연결 Unit".드릴장치.드릴링B위치 = { "1 기본 연결 Unit__드릴장치".드릴링B위치(P00001, P00041); }
        "1 기본 연결 Unit".RBT.투입 = { "1 기본 연결 Unit__RBT".투입(P00002, P00042); }
        "1 기본 연결 Unit".RBT.홈 = { "1 기본 연결 Unit__RBT".홈(P00003, P00043); }
        "2 StartReset 연결 Unit".드릴장치.드릴링A위치 = { "2 StartReset 연결 Unit__드릴장치".드릴링A위치(P00017, P00057); }
        "2 StartReset 연결 Unit".드릴장치.드릴링B위치 = { "2 StartReset 연결 Unit__드릴장치".드릴링B위치(P00018, P00058); }
        "2 StartReset 연결 Unit".RBT.투입 = { "2 StartReset 연결 Unit__RBT".투입(P00019, P00059); }
        "2 StartReset 연결 Unit".RBT.홈 = { "2 StartReset 연결 Unit__RBT".홈(P0001A, P0005A); }
        "3 Interlock 연결 Unit".드릴장치.드릴링A위치 = { "3 Interlock 연결 Unit__드릴장치".드릴링A위치(P0001F, P0005F); }
        "3 Interlock 연결 Unit".드릴장치.드릴링B위치 = { "3 Interlock 연결 Unit__드릴장치".드릴링B위치(P00020, P00060); }
        "3 Interlock 연결 Unit".RBT.투입 = { "3 Interlock 연결 Unit__RBT".투입(P00021, P00061); }
        "3 Interlock 연결 Unit".RBT.홈 = { "3 Interlock 연결 Unit__RBT".홈(P00022, P00062); }
        "4 SelfReset 연결 Unit".드릴장치.드릴링A위치 = { "4 SelfReset 연결 Unit__드릴장치".드릴링A위치(P0002A, P0006A); }
        "4 SelfReset 연결 Unit".드릴장치.드릴링B위치 = { "4 SelfReset 연결 Unit__드릴장치".드릴링B위치(P0002B, P0006B); }
        "4 SelfReset 연결 Unit".RBT.투입 = { "4 SelfReset 연결 Unit__RBT".투입(P0002C, P0006C); }
        "4 SelfReset 연결 Unit".RBT.홈 = { "4 SelfReset 연결 Unit__RBT".홈(P0002D, P0006D); }
        "5 Group 연결 Unit".드릴장치.드릴링A위치 = { "5 Group 연결 Unit__드릴장치".드릴링A위치(P00033, P00073); }
        "5 Group 연결 Unit".드릴장치.드릴링B위치 = { "5 Group 연결 Unit__드릴장치".드릴링B위치(P00034, P00074); }
        "5 Group 연결 Unit".RBT.투입 = { "5 Group 연결 Unit__RBT".투입(P00035, P00075); }
        "5 Group 연결 Unit".RBT.홈 = { "5 Group 연결 Unit__RBT".홈(P00036, P00076); }
        "4 Safety 조건".System1.Api1 = { "4 Safety 조건__System1".Api1(P00028, P00068); }
        "4 Safety 조건".System1.Api2 = { "4 Safety 조건__System1".Api2(P00029, P00069); }
        "6 멀티 Action".System1.Api1 = { "6 멀티 Action__System1".Api1(P0003B, P0007B); }
        "6 멀티 Action".System.Api = { "6 멀티 Action__System_01".Api(P00037, P00077); "6 멀티 Action__System_02".Api(P00038, P00078); "6 멀티 Action__System_03".Api(P00039, P00079); "6 멀티 Action__System_04".Api(P0003A, P0007A); }
        "7 멀티 Action Skip IO".SystemA.Api = { "7 멀티 Action Skip IO__SystemA_01".Api(P0003C, P0007C); "7 멀티 Action Skip IO__SystemA_02".Api(P0003D, P0007D); "7 멀티 Action Skip IO__SystemA_03".Api(P0003E, P0007E); "7 멀티 Action Skip IO__SystemA_04".Api(P0003F, P0007F); }
        "7 멀티 Action Skip IO".SystemB.Api2 = { "7 멀티 Action Skip IO__SystemB".Api2(P0400;Int32, P0784;Int32); }
        "8 Action 인터페이스 옵션".System1.Api1 = { "8 Action 인터페이스 옵션__System1".Api1(P00040, -); }
        "8 Action 인터페이스 옵션".System1.Api2 = { "8 Action 인터페이스 옵션__System1".Api2(-, P00080); }
        "8 Action 인터페이스 옵션".System1.Api3 = { "8 Action 인터페이스 옵션__System1".Api3(-, -); }
        "8 Action 인터페이스 옵션".System1.Api4 = { "8 Action 인터페이스 옵션__System1".Api4(-, -); }
        "9 Action 출력 옵션".System1.Api1 = { "9 Action 출력 옵션__System1".Api1(P00048, P00088); }
        "9 Action 출력 옵션".System2.ADV = { "9 Action 출력 옵션__System2".ADV(P00049, P00089); }
        "9 Action 출력 옵션".System2.RET = { "9 Action 출력 옵션__System2".RET(P0004A, P0008A); }
        "10 Action 설정 값".System1.Api1 = { "10 Action 설정 값__System1".Api1(P00009, P00049); }
        "10 Action 설정 값".System1.Api2 = { "10 Action 설정 값__System1".Api2(P0384;Int32, P0768;Int32); }
        "11 외부 행위 (Action) 배치".System1.Api1 = { "11 외부 행위 (Action) 배치__System1".Api1(P0000A, P0004A); }
        "11 외부 행위 (Action) 배치".System1.Api2 = { "11 외부 행위 (Action) 배치__System1".Api2(P0000B, P0004B); }
        "11 외부 행위 (Action) 배치".System1.Api3 = { "11 외부 행위 (Action) 배치__System1".Api3(P0000C, -); }
        "13 행위 사용 안함".Action."1" = { "13 행위 사용 안함__Action"."1"(P0000D, P0004D); }
        "13 행위 사용 안함".Action."2" = { "13 행위 사용 안함__Action"."2"(P0000E, P0004E); }
        "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)".System1.Api1 = { "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)__System1".Api1(P0000F, P0004F); }
        "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)".System1.Api2 = { "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)__System1".Api2(P00010, P00050); }
        "1 외부 주소".Device1.ADV = { "1 외부 주소__Device1".ADV(P00000, P00040); }
        "1 외부 주소".Device1.RET = { "1 외부 주소__Device1".RET(P00001, P00041); }
        "6 심볼 정의".Device1.ADV = { "6 심볼 정의__Device1".ADV(P00000;Boolean;Dev1ADV_I, P00040;Boolean;Dev1ADV_O); }
        "6 심볼 정의".Device1.RET = { "6 심볼 정의__Device1".RET(P00001;Boolean;Dev1RET_I, P2624;Int32;Dev1RET_O); }
        "8 Error 설정시간".DEV1.ADV = { "8 Error 설정시간__DEV1".ADV(P00041, P00081); }
        "8 Error 설정시간".DEV3.ADV = { "8 Error 설정시간__DEV3".ADV(P00043, P00083); }
        "8 Error 설정시간".DEV4.ADV = { "8 Error 설정시간__DEV4_01".ADV(P00044, P00084); "8 Error 설정시간__DEV4_02".ADV(P00046, P00086); }
        "8 Error 설정시간".DEV2.ADV = { "8 Error 설정시간__DEV2".ADV(P00042, P00082); }
        "8 Error 설정시간".DEV4.RET = { "8 Error 설정시간__DEV4_01".RET(P00045, P00085); "8 Error 설정시간__DEV4_02".RET(P00047, P00087); }
        "9 Negative 입력 옵션".System1.Api1 = { "9 Negative 입력 옵션__System1".Api1(P0416;Int32, P0800;Int32); }
    }
    [variables] = {
        Int32 VARIABLE1;
        Int32 VARIABLE2;
        Int32 VARIABLE3;
        Double VARIABLE4;
        Double Var1;
        const Double PI = 3.14;
        const Double PI_PI_PI = 3.14;
        const Double PI_PI = 3.14;
    }
    [operators] = {
        전원;
        Action3;
        Operator;
        Operator3 = #{$Dev1ADV_I == false;}
        Operator2 = #{$VARIABLE4 !=$PI_PI_PI;}
    }
    [commands] = {
        전원CMD;
        Action;
        Action1;
        Action2;
        Command;
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
            AutoSelect(M00628, -) = {  }
            AutoBTN1(M00629, -) = { "2 시스템 버튼 램프 유닛"; "3 시스템 버튼 램프 확장"; }
            AutoBTN2(M00628, -) = { "4 버튼 IO"; }
        }
        [m] = {
            ManualSelect(M0062A, -) = {  }
            ManualBTN1(M0062B, -) = { "2 시스템 버튼 램프 유닛"; "3 시스템 버튼 램프 확장"; }
            ManualBTN2(M00629, -) = { "4 버튼 IO"; }
        }
        [d] = {
            DrivePushBtn(M0062C, -) = {  }
            DriveBTN1(M0062D, -) = { "2 시스템 버튼 램프 유닛"; "3 시스템 버튼 램프 확장"; }
            DriveBTN2(M0062A, -) = { "4 버튼 IO"; }
        }
        [e] = {
            EmergencyBtn(M0062E, -) = {  }
            EmergencyBTN1(M0062F, -) = { "2 시스템 버튼 램프 유닛"; "3 시스템 버튼 램프 확장"; }
            EmergencyBTN2(M0062D, -) = { "4 버튼 IO"; }
        }
        [t] = {
            TestBTN1(M00630, -) = { "2 시스템 버튼 램프 유닛"; "3 시스템 버튼 램프 확장"; }
            TestBTN2(M0062C, -) = { "4 버튼 IO"; }
        }
        [r] = {
            ReadyBTN1(M00631, -) = { "2 시스템 버튼 램프 유닛"; "3 시스템 버튼 램프 확장"; }
            ReadyBTN2(M0062C, -) = { "4 버튼 IO"; }
        }
        [p] = {
            PausePushBtn(M00632, -) = {  }
            PauseBTN1(M00633, -) = { "2 시스템 버튼 램프 유닛"; "3 시스템 버튼 램프 확장"; }
            PauseBTN2(M0062B, -) = { "4 버튼 IO"; }
        }
        [c] = {
            ClearPushBtn(M00634, -) = {  }
            ClearBTN1(M00635, -) = { "2 시스템 버튼 램프 유닛"; "3 시스템 버튼 램프 확장"; }
            ClearBTN2(M0062C, -) = { "4 버튼 IO"; }
        }
        [h] = {
            HomeBTN1(M00636, -) = { "2 시스템 버튼 램프 유닛"; "3 시스템 버튼 램프 확장"; }
            HomeBTN2(M0062C, -) = { "4 버튼 IO"; }
        }
    }
    [lamps] = {
        [a] = { AutoModeLamp(-, M00637) = {  } }
        [m] = { ManualModeLamp(-, M00638) = {  } }
        [d] = { DriveLamp(-, M00639) = {  } }
        [e] = { ErrorLamp(-, M0063A) = {  } }
        [r] = { ReadyStateLamp(-, M0063B) = {  } }
        [i] = { IdleModeLamp(-, M0063C) = {  } }
        [o] = { OriginStateLamp(-, M0063D) = {  } }
    }
    [conditions] = {
        [r] = {
            Condition1(M0063E, -) = { "4 시스템 외부조건 유닛"; }
            Condition2(M0063F, -) = { "4 시스템 외부조건 유닛"; }
        }
        [d] = {
            Condition3(M00640, -) = { "4 시스템 외부조건 유닛"; }
            Condition4(M00641, -) = { "4 시스템 외부조건 유닛"; }
        }
    }
    [actions] = {
        [e] = {
            EmgAction1(-, M00642) = { "5 시스템 외부액션 유닛"; }
            EmgAction2(-, M00643) = { "5 시스템 외부액션 유닛"; }
            EmgAction3(-, M00644:False) = { "7 시스템 외부액션 타겟 Value"; }
            EmgAction4(-, M4224:3000) = { "7 시스템 외부액션 타겟 Value"; }
        }
        [p] = {
            PauseAction1(_, _) = { "5 시스템 외부액션 유닛"; }
            PauseAction2(_, _) = { "5 시스템 외부액션 유닛"; }
            PauseAction3(_, _:False) = { "7 시스템 외부액션 타겟 Value"; }
            PauseAction4(_, _) = { "7 시스템 외부액션 타겟 Value"; }
        }
    }
    [prop] = {
        [safety] = {
            "4 Safety 조건".Work1.System1.Api1 = { "4 Safety 조건".Work1.System1.Api2; }
            "4 Safety 조건".Work1.System1.Api2 = { "4 Safety 조건".Work2.System1.Api1; }
            "4 Safety 조건".Work2.System1.Api1 = { "6 멀티 Action".Work1.System1.Api1; }
        }
        [autopre] = {
            "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)".Work1.System1.Api1 = { "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)".Work1.System1.Api2; }
        }
        [layouts] = {
            "1 작업 및 행위__RBT" = (876, 759, 305, 164);
            "1 작업 및 행위 유닛__Device" = (1143, 522, 474, 304);
            "1 작업 및 행위 유닛__RBT" = (1310, 296, 164, 91);
            "2 행위 (Action) 배치__RBT" = (872, 769, 322, 176);
            "2 행위 (Action) 배치 유닛__Device" = (1011, 660, 239, 93);
            "2 행위 (Action) 배치 유닛__RBT" = (1310, 296, 164, 91);
            "3 작업 (Work) 타입__RBT" = (876, 774, 308, 173);
            "3 작업 (Work) 타입 유닛__RBT" = (1310, 296, 164, 91);
            "4 행위 (Action) 타입__RBT" = (856, 773, 306, 163);
            "4 행위 (Action) 타입 유닛__System1" = (1110, 578, 563, 304);
            "4 행위 (Action) 타입 유닛__RBT" = (1310, 296, 164, 91);
            "5 시스템 인터페이스__드릴장치" = (552, 548, 243, 90);
            "5 시스템 인터페이스 유닛__드릴장치" = (1159, 210, 168, 60);
            "5 시스템 인터페이스 유닛__Device1" = (1233, 815, 267, 114);
            "1 기본 연결 Unit__드릴장치" = (1061, 210, 168, 60);
            "1 기본 연결 Unit__RBT" = (1310, 296, 164, 91);
            "2 StartReset 연결 Unit__드릴장치" = (1061, 210, 168, 60);
            "2 StartReset 연결 Unit__RBT" = (1310, 296, 164, 91);
            "3 Interlock 연결 Unit__드릴장치" = (1061, 223, 168, 60);
            "3 Interlock 연결 Unit__RBT" = (1388, 305, 164, 91);
            "4 SelfReset 연결 Unit__드릴장치" = (1061, 210, 168, 60);
            "4 SelfReset 연결 Unit__RBT" = (1310, 296, 164, 91);
            "5 Group 연결 Unit__드릴장치" = (1061, 216, 168, 60);
            "5 Group 연결 Unit__RBT" = (1310, 302, 164, 91);
            "4 Safety 조건__System1" = (1083, 521, 618, 185);
            "6 멀티 Action__System1" = (257, 540, 563, 244);
            "6 멀티 Action__System_01" = (1099, 540, 563, 244);
            "6 멀티 Action__System_02" = (1099, 540, 563, 244);
            "6 멀티 Action__System_03" = (1099, 540, 563, 244);
            "6 멀티 Action__System_04" = (1099, 540, 563, 244);
            "7 멀티 Action Skip IO__SystemA_01" = (257, 540, 563, 244);
            "7 멀티 Action Skip IO__SystemA_02" = (257, 540, 563, 244);
            "7 멀티 Action Skip IO__SystemA_03" = (257, 540, 563, 244);
            "7 멀티 Action Skip IO__SystemA_04" = (257, 540, 563, 244);
            "7 멀티 Action Skip IO__SystemB" = (1099, 540, 563, 244);
            "8 Action 인터페이스 옵션__System1" = (1100, 785, 563, 192);
            "9 Action 출력 옵션__System1" = (257, 556, 563, 192);
            "9 Action 출력 옵션__System2" = (1172, 816, 563, 162);
            "1 외부 주소__Device1" = (306, 773, 436, 108);
            "6 심볼 정의__Device1" = (1483, 294, 246, 64);
            "8 Error 설정시간__DEV1" = (412, 601, 281, 96);
            "8 Error 설정시간__DEV3" = (412, 866, 281, 108);
            "8 Error 설정시간__DEV4_01" = (1577, 820, 330, 98);
            "8 Error 설정시간__DEV4_02" = (1577, 820, 330, 98);
            "8 Error 설정시간__DEV2" = (1255, 601, 281, 96);
            "10 Action 설정 값__System1" = (1115, 650, 563, 244);
            "11 외부 행위 (Action) 배치__System1" = (962, 607, 307, 130);
            "13 행위 사용 안함__Action" = (1105, 730, 209, 93);
            "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)__System1" = (127, 640, 439, 94);
            "9 Negative 입력 옵션__System1" = (1099, 556, 563, 192);
        }
        [motions] = {
            "7 외부호출".MotionRET = {./Assets/Cylinder/DoubleType.obj:RET};
            "7 외부호출".MotionADV = {./Assets/Cylinder/DoubleType.obj:ADV};
        }
        [scripts] = {
            "7 외부호출".Script1 = {ThirdParty.AddressInfo.Provider.testFunc1()};
            "7 외부호출".Script2 = {ThirdParty.AddressInfo.Provider.testFunc2()};
        }
        [errors] = {
            "8 Error 설정시간".Work1.DEV1.ADV = {MAX(10000ms)};
            "8 Error 설정시간".Work2.DEV2.ADV = {CHK(500ms)};
            "8 Error 설정시간".Work3.DEV3.ADV = {MAX(1000ms), CHK(500ms)};
            "8 Error 설정시간".Work4.DEV4.ADV = {MAX(1000ms), CHK(500ms)};
        }
        [finish] = {
            "5 Work 초기조건".Work1;
        }
        [disable] = {
            "13 행위 사용 안함".Work2."Action.2";
        }
        [notrans] = {
            "5 Group 연결 Unit".Work3;
            "5 Group 연결 Unit".Work2;
            "15 Work 데이터전송".Work31;
            "15 Work 데이터전송".Work3;
        }
    }
    [device file="./dsLib/AutoGen/1 작업 및 행위__RBT.ds"] "1 작업 및 행위__RBT"; 
    [device file="./dsLib/AutoGen/1 작업 및 행위 유닛__Device.ds"] "1 작업 및 행위 유닛__Device"; 
    [device file="./dsLib/AutoGen/1 작업 및 행위 유닛__RBT.ds"] "1 작업 및 행위 유닛__RBT"; 
    [device file="./dsLib/AutoGen/2 행위 (Action) 배치__RBT.ds"] "2 행위 (Action) 배치__RBT"; 
    [device file="./dsLib/AutoGen/2 행위 (Action) 배치 유닛__Device.ds"] "2 행위 (Action) 배치 유닛__Device"; 
    [device file="./dsLib/AutoGen/2 행위 (Action) 배치 유닛__RBT.ds"] "2 행위 (Action) 배치 유닛__RBT"; 
    [device file="./dsLib/AutoGen/3 작업 (Work) 타입__RBT.ds"] "3 작업 (Work) 타입__RBT"; 
    [device file="./dsLib/AutoGen/3 작업 (Work) 타입 유닛__RBT.ds"] "3 작업 (Work) 타입 유닛__RBT"; 
    [device file="./dsLib/AutoGen/4 행위 (Action) 타입__RBT.ds"] "4 행위 (Action) 타입__RBT"; 
    [device file="./dsLib/AutoGen/4 행위 (Action) 타입 유닛__System1.ds"] "4 행위 (Action) 타입 유닛__System1"; 
    [device file="./dsLib/AutoGen/4 행위 (Action) 타입 유닛__RBT.ds"] "4 행위 (Action) 타입 유닛__RBT"; 
    [device file="./dsLib/AutoGen/5 시스템 인터페이스__드릴장치.ds"] "5 시스템 인터페이스__드릴장치"; 
    [device file="./dsLib/AutoGen/5 시스템 인터페이스 유닛__드릴장치.ds"] "5 시스템 인터페이스 유닛__드릴장치"; 
    [device file="./dsLib/AutoGen/5 시스템 인터페이스 유닛__Device1.ds"] "5 시스템 인터페이스 유닛__Device1"; 
    [device file="./dsLib/AutoGen/1 기본 연결 Unit__드릴장치.ds"] "1 기본 연결 Unit__드릴장치"; 
    [device file="./dsLib/AutoGen/1 기본 연결 Unit__RBT.ds"] "1 기본 연결 Unit__RBT"; 
    [device file="./dsLib/AutoGen/2 StartReset 연결 Unit__드릴장치.ds"] "2 StartReset 연결 Unit__드릴장치"; 
    [device file="./dsLib/AutoGen/2 StartReset 연결 Unit__RBT.ds"] "2 StartReset 연결 Unit__RBT"; 
    [device file="./dsLib/AutoGen/3 Interlock 연결 Unit__드릴장치.ds"] "3 Interlock 연결 Unit__드릴장치"; 
    [device file="./dsLib/AutoGen/3 Interlock 연결 Unit__RBT.ds"] "3 Interlock 연결 Unit__RBT"; 
    [device file="./dsLib/AutoGen/4 SelfReset 연결 Unit__드릴장치.ds"] "4 SelfReset 연결 Unit__드릴장치"; 
    [device file="./dsLib/AutoGen/4 SelfReset 연결 Unit__RBT.ds"] "4 SelfReset 연결 Unit__RBT"; 
    [device file="./dsLib/AutoGen/5 Group 연결 Unit__드릴장치.ds"] "5 Group 연결 Unit__드릴장치"; 
    [device file="./dsLib/AutoGen/5 Group 연결 Unit__RBT.ds"] "5 Group 연결 Unit__RBT"; 
    [device file="./dsLib/AutoGen/4 Safety 조건__System1.ds"] "4 Safety 조건__System1"; 
    [device file="./dsLib/AutoGen/6 멀티 Action__System1.ds"] "6 멀티 Action__System1"; 
    [device file="./dsLib/AutoGen/6 멀티 Action__System_01.ds"] "6 멀티 Action__System_01"; 
    [device file="./dsLib/AutoGen/6 멀티 Action__System_02.ds"] "6 멀티 Action__System_02"; 
    [device file="./dsLib/AutoGen/6 멀티 Action__System_03.ds"] "6 멀티 Action__System_03"; 
    [device file="./dsLib/AutoGen/6 멀티 Action__System_04.ds"] "6 멀티 Action__System_04"; 
    [device file="./dsLib/AutoGen/7 멀티 Action Skip IO__SystemA_01.ds"] "7 멀티 Action Skip IO__SystemA_01"; 
    [device file="./dsLib/AutoGen/7 멀티 Action Skip IO__SystemA_02.ds"] "7 멀티 Action Skip IO__SystemA_02"; 
    [device file="./dsLib/AutoGen/7 멀티 Action Skip IO__SystemA_03.ds"] "7 멀티 Action Skip IO__SystemA_03"; 
    [device file="./dsLib/AutoGen/7 멀티 Action Skip IO__SystemA_04.ds"] "7 멀티 Action Skip IO__SystemA_04"; 
    [device file="./dsLib/AutoGen/7 멀티 Action Skip IO__SystemB.ds"] "7 멀티 Action Skip IO__SystemB"; 
    [device file="./dsLib/AutoGen/8 Action 인터페이스 옵션__System1.ds"] "8 Action 인터페이스 옵션__System1"; 
    [device file="./dsLib/AutoGen/9 Action 출력 옵션__System1.ds"] "9 Action 출력 옵션__System1"; 
    [device file="./dsLib/Cylinder/DoubleCylinder.ds"] 
        "9 Action 출력 옵션__System2",
        "1 외부 주소__Device1",
        "6 심볼 정의__Device1",
        "8 Error 설정시간__DEV1",
        "8 Error 설정시간__DEV3",
        "8 Error 설정시간__DEV4_01",
        "8 Error 설정시간__DEV4_02",
        "8 Error 설정시간__DEV2"; 
    [device file="./dsLib/AutoGen/10 Action 설정 값__System1.ds"] "10 Action 설정 값__System1"; 
    [device file="./dsLib/AutoGen/11 외부 행위 (Action) 배치__System1.ds"] "11 외부 행위 (Action) 배치__System1"; 
    [device file="./dsLib/AutoGen/13 행위 사용 안함__Action.ds"] "13 행위 사용 안함__Action"; 
    [device file="./dsLib/AutoGen/16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)__System1.ds"] "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)__System1"; 
    [device file="./dsLib/AutoGen/9 Negative 입력 옵션__System1.ds"] "9 Negative 입력 옵션__System1"; 
    [versions] = {
        DS-Langugage-Version = 1.0.0.1;
        DS-Engine-Version = 0.9.10.16;
    }
}
//DS Library Date = [Library Release Date 24.3.26]

"""

    let CpuTestText =
        """
[sys] HelloDS = {
    [flow] STN1 = {
        Work2 => Work1 => Work2;
        Work1_1 > Work3;
        Work1 = {
            Device1.ADV > Device1.RET > Device1_ADV_1 > Device1_RET_1;
        }
        STN2_Work1; 
        [aliases] = {
            STN2.Work1 = { STN2_Work1; }
            Work1.Device1.ADV = { Device1_ADV_1; }
            Work1.Device1.RET = { Device1_RET_1; }
            Work1 = { Work1_1; }
        }
    }
    [flow] STN2 = {
        Work1; 
    }
    [jobs] = {
        STN1.Device1.ADV = { STN1__Device1.ADV(%IX0.0.0, %QX0.1.0); }
        STN1.Device1.RET = { STN1__Device1.RET(%IX0.0.1, %QX0.1.1); }
    }
    [buttons] = {
        [a] = { AutoSelect(%MX1000, -) = {  } }
        [m] = { ManualSelect(%MX1001, -) = {  } }
        [d] = { DrivePushBtn(%MX1002, -) = {  } }
        [e] = { EmergencyBtn(%MX1003, -) = {  } }
        [p] = { PausePushBtn(%MX1004, -) = {  } }
        [c] = { ClearPushBtn(%MX1005, -) = {  } }
    }
    [lamps] = {
        [a] = { AutoModeLamp(-, %MX1006) = {  } }
        [m] = { ManualModeLamp(-, %MX1007) = {  } }
        [d] = { DriveLamp(-, %MX1008) = {  } }
        [e] = { ErrorLamp(-, %MX1009) = {  } }
        [r] = { ReadyStateLamp(-, %MX1010) = {  } }
        [i] = { IdleModeLamp(-, %MX1011) = {  } }
        [o] = { OriginStateLamp(-, %MX1012) = {  } }
    }
    [prop] = {
        [layouts] = {
            STN1__Device1 = (1258, 799, 240, 80);
        }
        [errors] = {
            STN1.Work1.Device1.ADV = {MAX(1000ms)};
        }
    }
    [device file="./dsLib/Cylinder/DoubleCylinder.ds"] STN1__Device1; 
    [versions] = {
        DS-Langugage-Version = 1.0.0.1;
        DS-Engine-Version = 0.9.10.16;
    }
}
//DS Library Date = [Library Release Date 24.3.26]
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
        F_Seg1 > Seg;		// F_Seg1(Alias)> Seg(Real);
        Seg = {
            F.p > F.m;		// F.p(Call)> F.m(Call);
        }
        [aliases] = {
            F.Seg1 = { F_Seg1; }
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
        F2; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/cylinder.ds
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

        ModelParser.ParseFromString(
            text,
            ParserOptions.Create4Simulation(systemRepo, "", "ActiveCpuNA.me", None, DuNone)
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
