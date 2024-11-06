namespace T

[<AutoOpen>]
module ModelAnswers =

    let answerEveryScenarioText = """
[sys] DS_Units_V12 = {
    [flow] "시스템 모델링" = {
        "System B", "System A"; // island
    }
    [flow] "모델링 기본 구성" = {
    }
    [flow] "모델링 확장 구성1" = {
        "System A"; // island
    }
    [flow] "모델링 확장 구성2" = {
        "System A", Flow2, Flow1; // island
    }
    [flow] "모델링 구조 Unit" = {
    }
    [flow] "기본 도형 Unit" = {
    }
    [flow] "1 작업 및 행위" = {
        공급작업 = {
            RBT.투입, RBT.홈; // island
        }
        드릴작업; // island
    }
    [flow] "1 작업 및 행위 유닛" = {
        Device.Action1 > Work1;		// Device.Action1(Call)> Work1(Real);
        공급작업 = {
            RBT.투입, RBT.홈; // island
        }
        드릴작업; // island
    }
    [flow] "2 행위 (Action) 배치" = {
        #전원 > 드릴작업;		// #전원(Call)> 드릴작업(Real);
        공급작업 = {
            RBT.투입, RBT.홈; // island
        }
    }
    [flow] "2 행위 (Action) 배치 유닛" = {
        #전원 > 드릴작업;		// #전원(Call)> 드릴작업(Real);
        Device.Action1 > Work1_1;		// Device.Action1(Call)> Work1_1(Alias);
        Work1 = {
            Device.Action1, Device.Action2; // island
        }
        공급작업 = {
            RBT.투입, RBT.홈; // island
        }
        [aliases] = {
            Work1 = { Work1_1; }
        }
    }
    [flow] "3 작업 (Work) 타입" = {
        #전원 > 드릴작업;		// #전원(Call)> 드릴작업(Real);
        공급작업 = {
            RBT.투입, RBT.홈; // island
        }
        Flow2, Flow1; // island
    }
    [flow] "3 작업 (Work) 타입 유닛" = {
        #전원 > 드릴작업;		// #전원(Call)> 드릴작업(Real);
        공급작업 = {
            RBT.투입, RBT.홈; // island
        }
        Work1, Flow2, Flow1, "3 작업 (Work) 타입_드릴작업"; // island
        [aliases] = {
            "3 작업 (Work) 타입".드릴작업 = { "3 작업 (Work) 타입_드릴작업"; }
        }
    }
    [flow] "4 행위 (Action) 타입" = {
        공급작업 = {
            RBT.투입, RBT.홈; // island
        }
        드릴작업 = {
            #전원CMD; // island
        }
    }
    [flow] "4 행위 (Action) 타입 유닛" = {
        #전원 > 드릴작업;		// #전원(Call)> 드릴작업(Real);
        공급작업 = {
            RBT.투입, RBT.홈; // island
        }
        드릴작업1 = {
            System1.Api1; // island
        }
        내부Work = {
            #Action; // island
        }
    }
    [flow] "5 시스템 인터페이스" = {
        드릴작업 = {
            드릴장치.드릴링A위치, 드릴장치.드릴링B위치; // island
        }
        이동A, 드릴, 이동B; // island
    }
    [flow] "5 시스템 인터페이스 유닛" = {
        Device1.Api1 > Work2;		// Device1.Api1(Call)> Work2(Real);
        드릴작업 = {
            드릴장치.드릴링A위치, 드릴장치.드릴링B위치; // island
        }
        Work1, 이동A, 드릴, 이동B; // island
    }
    [flow] "기본 연결 Unit" = {
    }
    [flow] "1 기본 연결 Unit" = {
        Work1_1 |> Work2_1;		// Work1_1(Alias)|> Work2_1(Alias);
        #전원 > 드릴작업;		// #전원(Call)> 드릴작업(Real);
        드릴작업 |> 공급작업 > 드릴작업;		// 드릴작업(Real)|> 공급작업(Real) > 드릴작업(Real);
        Work1 > Work2;		// Work1(Real)> Work2(Real);
        드릴작업 = {
            드릴장치.드릴링A위치, 드릴장치.드릴링B위치; // island
        }
        공급작업 = {
            RBT.투입, RBT.홈; // island
        }
        [aliases] = {
            Work1 = { Work1_1; }
            Work2 = { Work2_1; }
        }
    }
    [flow] "2 StartReset 연결 Unit" = {
        #전원 > 드릴작업;		// #전원(Call)> 드릴작업(Real);
        드릴작업 |> 공급작업 > 드릴작업;		// 드릴작업(Real)|> 공급작업(Real) > 드릴작업(Real);
        Work2_1 |> Work1_1 > Work2_1;		// Work2_1(Alias)|> Work1_1(Alias) > Work2_1(Alias);
        Work1 => Work2;		// Work1(Real)=> Work2(Real);
        드릴작업 = {
            드릴장치.드릴링A위치, 드릴장치.드릴링B위치; // island
        }
        공급작업 = {
            RBT.투입, RBT.홈; // island
        }
        [aliases] = {
            Work1 = { Work1_1; }
            Work2 = { Work2_1; }
        }
    }
    [flow] "3 Interlock 연결 Unit" = {
        Work1_1 <|> Work2_1;		// Work1_1(Alias)<|> Work2_1(Alias);
        Work1 |> Work2 |> Work1;		// Work1(Real)|> Work2(Real) |> Work1(Real);
        #전원 > 드릴작업;		// #전원(Call)> 드릴작업(Real);
        공급작업 => 드릴작업;		// 공급작업(Real)=> 드릴작업(Real);
        드릴작업 = {
            드릴장치.드릴링A위치, 드릴장치.드릴링B위치; // island
        }
        공급작업 = {
            RBT.투입, RBT.홈; // island
        }
        이동A, 드릴, 이동B; // island
        [aliases] = {
            Work1 = { Work1_1; }
            Work2 = { Work2_1; }
        }
    }
    [flow] "4 SelfReset 연결 Unit" = {
        Work1_1 <|> Work2_1;		// Work1_1(Alias)<|> Work2_1(Alias);
        Work1 =|> Work2;		// Work1(Real)=|> Work2(Real);
        #전원 > 드릴작업 =|> 드릴작업클리어;		// #전원(Call)> 드릴작업(Real) =|> 드릴작업클리어(Real);
        공급작업 => 드릴작업;		// 공급작업(Real)=> 드릴작업(Real);
        Work1_1 > Work2_1;		// Work1_1(Alias)> Work2_1(Alias);
        드릴작업 = {
            드릴장치.드릴링A위치, 드릴장치.드릴링B위치; // island
        }
        공급작업 = {
            RBT.투입, RBT.홈; // island
        }
        [aliases] = {
            Work1 = { Work1_1; }
            Work2 = { Work2_1; }
        }
    }
    [flow] "5 Group 연결 Unit" = {
        #전원 > 드릴작업;		// #전원(Call)> 드릴작업(Real);
        공급작업 => 드릴작업;		// 공급작업(Real)=> 드릴작업(Real);
        Work1_1, Work2_1, Work3_1 > Work4_1;		// Work1_1(Alias), Work2_1(Alias), Work3_1(Alias)> Work4_1(Alias);
        Work3 > Work4;		// Work3(Real)> Work4(Real);
        Work2 > Work4;		// Work2(Real)> Work4(Real);
        Work1 > Work4;		// Work1(Real)> Work4(Real);
        드릴작업 = {
            드릴장치.드릴링A위치, 드릴장치.드릴링B위치; // island
        }
        공급작업 = {
            RBT.투입, RBT.홈; // island
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
        "System A", "System B"; // island
    }
    [flow] "2 시스템 버튼 램프" = {
        "System A"; // island
    }
    [flow] "2 시스템 버튼 램프 유닛" = {
        "System A"; // island
    }
    [flow] "3 시스템 버튼 램프 확장" = {
        "System A"; // island
    }
    [flow] "4 시스템 외부조건" = {
        "System A"; // island
    }
    [flow] "4 시스템 외부조건 유닛" = {
        "System A"; // island
    }
    [flow] "5 시스템 외부액션 유닛" = {
        "System A"; // island
    }
    [flow] "7 시스템 외부액션 타겟 Value" = {
        "System A"; // island
    }
    [flow] "4 Safety 조건" = {
        Work1 = {
            System1.Api1, System1.Api2; // island
        }
        Work2 = {
            System1.Api1; // island
        }
    }
    [flow] "5 Work 초기조건" = {
        Work1, Work1_1; // island
        [aliases] = {
            Work1 = { Work1_1; }
        }
    }
    [flow] "6 멀티 Action" = {
        Work1 = {
            System1.Api1; // island
        }
        Work2 = {
            System.Api; // island
        }
    }
    [flow] "7 멀티 Action Skip IO" = {
        Work1 = {
            SystemA.Api; // island
        }
        Work2 = {
            SystemB.Api2((230, 3214) : 500); // island
        }
    }
    [flow] "8 Action 인터페이스 옵션" = {
        Work1 = {
            System1.Api1, System1.Api2; // island
        }
        Work2 = {
            System1.Api3, System1.Api4; // island
        }
    }
    [flow] "9 Action 출력 옵션" = {
        Work1 = {
            System1.Api1; // island
        }
        Work2 = {
            System2.ADV, System2.RET; // island
        }
    }
    [flow] "10 Action 설정 값" = {
        Work1 = {
            System1.Api1; // island
        }
        Work2 = {
            System1.Api2(100 : 500); // island
        }
    }
    [flow] "11 외부 행위 (Action) 배치" = {
        System1.Api3 > Work1_1;		// System1.Api3(Call)> Work1_1(Alias);
        Work1 = {
            System1.Api1, System1.Api2; // island
        }
        [aliases] = {
            Work1 = { Work1_1; }
        }
    }
    [flow] "12 내부 행위 (Action) 배치" = {
        #Action3 > Work1_1;		// #Action3(Call)> Work1_1(Alias);
        Work1 = {
            #Action1, #Action2; // island
        }
        [aliases] = {
            Work1 = { Work1_1; }
        }
    }
    [flow] "13 행위 사용 안함" = {
        Work1 = {
            Action."1", Action."2"; // island
        }
        Work2 = {
            Action."1", Action."2"; // island
        }
    }
    [flow] "14 Work 설정시간" = {
        Work1; // island
    }
    [flow] "15 Work 데이터전송" = {
        Work1_1 > Work31 => Work4_1;		// Work1_1(Alias)> Work31(Real) => Work4_1(Alias);
        Work1_1 => Work2_1 => Work4_1;		// Work1_1(Alias)=> Work2_1(Alias) => Work4_1(Alias);
        Work1 > Work3 => Work4;		// Work1(Real)> Work3(Real) => Work4(Real);
        Work1 => Work2 => Work4;		// Work1(Real)=> Work2(Real) => Work4(Real);
        [aliases] = {
            Work1 = { Work1_1; }
            Work2 = { Work2_1; }
            Work4 = { Work4_1; }
        }
    }
    [flow] "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)" = {
        Work1 = {
            System1.Api1 > System1.Api2;		// System1.Api1(Call)> System1.Api2(Call);
        }
    }
    [flow] "IO Table" = {
    }
    [flow] "1 외부 주소" = {
        Work1 = {
            Device1.ADV > Device1.RET;		// Device1.ADV(Call)> Device1.RET(Call);
        }
    }
    [flow] "2 내부 변수_상수" = {
        #Operator > Work1;		// #Operator(Call)> Work1(Real);
        Work1 = {
            #Command; // island
        }
    }
    [flow] "3 내부 연산_명령" = {
        #Operator > Work1;		// #Operator(Call)> Work1(Real);
        Work1 = {
            #Command2; // island
        }
    }
    [flow] "4 버튼 IO" = {
    }
    [flow] "5 램프 IO" = {
    }
    [flow] "6 심볼 정의" = {
        #Operator3 > Work1;		// #Operator3(Call)> Work1(Real);
        Work1 = {
            #Command3; // island
        }
        Work2 = {
            Device1.ADV > Device1.RET(True : 300);		// Device1.ADV(Call)> Device1.RET(True : 300)(Call);
        }
    }
    [flow] "7 외부호출" = {
        Script1, Script2, MotionRET, MotionADV; // island
    }
    [flow] "8 Error 설정시간" = {
        Work1 = {
            DEV1.ADV; // island
        }
        Work2 = {
            DEV2.ADV; // island
        }
        Work3 = {
            DEV3.ADV; // island
        }
        Work4 = {
            DEV4.ADV, DEV4.RET; // island
        }
    }
    [flow] "9 Negative 입력 옵션" = {
        Work1 = {
            System1.Api1(300 : 400); // island
        }
        Work2 = {
            System1.Api1; // island
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
            "5 Group 연결 Unit".Work2;
            "5 Group 연결 Unit".Work3;
            "15 Work 데이터전송".Work3;
            "15 Work 데이터전송".Work31;
        }
    }
    [device file="./dsLib/AutoGen/1 작업 및 행위__RBT.ds"] "1 작업 및 행위__RBT"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/1 작업 및 행위__RBT.ds
    [device file="./dsLib/AutoGen/1 작업 및 행위 유닛__Device.ds"] "1 작업 및 행위 유닛__Device"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/1 작업 및 행위 유닛__Device.ds
    [device file="./dsLib/AutoGen/1 작업 및 행위 유닛__RBT.ds"] "1 작업 및 행위 유닛__RBT"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/1 작업 및 행위 유닛__RBT.ds
    [device file="./dsLib/AutoGen/2 행위 (Action) 배치__RBT.ds"] "2 행위 (Action) 배치__RBT"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/2 행위 (Action) 배치__RBT.ds
    [device file="./dsLib/AutoGen/2 행위 (Action) 배치 유닛__Device.ds"] "2 행위 (Action) 배치 유닛__Device"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/2 행위 (Action) 배치 유닛__Device.ds
    [device file="./dsLib/AutoGen/2 행위 (Action) 배치 유닛__RBT.ds"] "2 행위 (Action) 배치 유닛__RBT"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/2 행위 (Action) 배치 유닛__RBT.ds
    [device file="./dsLib/AutoGen/3 작업 (Work) 타입__RBT.ds"] "3 작업 (Work) 타입__RBT"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/3 작업 (Work) 타입__RBT.ds
    [device file="./dsLib/AutoGen/3 작업 (Work) 타입 유닛__RBT.ds"] "3 작업 (Work) 타입 유닛__RBT"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/3 작업 (Work) 타입 유닛__RBT.ds
    [device file="./dsLib/AutoGen/4 행위 (Action) 타입__RBT.ds"] "4 행위 (Action) 타입__RBT"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/4 행위 (Action) 타입__RBT.ds
    [device file="./dsLib/AutoGen/4 행위 (Action) 타입 유닛__System1.ds"] "4 행위 (Action) 타입 유닛__System1"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/4 행위 (Action) 타입 유닛__System1.ds
    [device file="./dsLib/AutoGen/4 행위 (Action) 타입 유닛__RBT.ds"] "4 행위 (Action) 타입 유닛__RBT"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/4 행위 (Action) 타입 유닛__RBT.ds
    [device file="./dsLib/AutoGen/5 시스템 인터페이스__드릴장치.ds"] "5 시스템 인터페이스__드릴장치"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/5 시스템 인터페이스__드릴장치.ds
    [device file="./dsLib/AutoGen/5 시스템 인터페이스 유닛__드릴장치.ds"] "5 시스템 인터페이스 유닛__드릴장치"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/5 시스템 인터페이스 유닛__드릴장치.ds
    [device file="./dsLib/AutoGen/5 시스템 인터페이스 유닛__Device1.ds"] "5 시스템 인터페이스 유닛__Device1"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/5 시스템 인터페이스 유닛__Device1.ds
    [device file="./dsLib/AutoGen/1 기본 연결 Unit__드릴장치.ds"] "1 기본 연결 Unit__드릴장치"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/1 기본 연결 Unit__드릴장치.ds
    [device file="./dsLib/AutoGen/1 기본 연결 Unit__RBT.ds"] "1 기본 연결 Unit__RBT"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/1 기본 연결 Unit__RBT.ds
    [device file="./dsLib/AutoGen/2 StartReset 연결 Unit__드릴장치.ds"] "2 StartReset 연결 Unit__드릴장치"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/2 StartReset 연결 Unit__드릴장치.ds
    [device file="./dsLib/AutoGen/2 StartReset 연결 Unit__RBT.ds"] "2 StartReset 연결 Unit__RBT"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/2 StartReset 연결 Unit__RBT.ds
    [device file="./dsLib/AutoGen/3 Interlock 연결 Unit__드릴장치.ds"] "3 Interlock 연결 Unit__드릴장치"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/3 Interlock 연결 Unit__드릴장치.ds
    [device file="./dsLib/AutoGen/3 Interlock 연결 Unit__RBT.ds"] "3 Interlock 연결 Unit__RBT"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/3 Interlock 연결 Unit__RBT.ds
    [device file="./dsLib/AutoGen/4 SelfReset 연결 Unit__드릴장치.ds"] "4 SelfReset 연결 Unit__드릴장치"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/4 SelfReset 연결 Unit__드릴장치.ds
    [device file="./dsLib/AutoGen/4 SelfReset 연결 Unit__RBT.ds"] "4 SelfReset 연결 Unit__RBT"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/4 SelfReset 연결 Unit__RBT.ds
    [device file="./dsLib/AutoGen/5 Group 연결 Unit__드릴장치.ds"] "5 Group 연결 Unit__드릴장치"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/5 Group 연결 Unit__드릴장치.ds
    [device file="./dsLib/AutoGen/5 Group 연결 Unit__RBT.ds"] "5 Group 연결 Unit__RBT"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/5 Group 연결 Unit__RBT.ds
    [device file="./dsLib/AutoGen/4 Safety 조건__System1.ds"] "4 Safety 조건__System1"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/4 Safety 조건__System1.ds
    [device file="./dsLib/AutoGen/6 멀티 Action__System1.ds"] "6 멀티 Action__System1"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/6 멀티 Action__System1.ds
    [device file="./dsLib/AutoGen/6 멀티 Action__System_01.ds"] "6 멀티 Action__System_01"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/6 멀티 Action__System_01.ds
    [device file="./dsLib/AutoGen/6 멀티 Action__System_02.ds"] "6 멀티 Action__System_02"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/6 멀티 Action__System_02.ds
    [device file="./dsLib/AutoGen/6 멀티 Action__System_03.ds"] "6 멀티 Action__System_03"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/6 멀티 Action__System_03.ds
    [device file="./dsLib/AutoGen/6 멀티 Action__System_04.ds"] "6 멀티 Action__System_04"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/6 멀티 Action__System_04.ds
    [device file="./dsLib/AutoGen/7 멀티 Action Skip IO__SystemA_01.ds"] "7 멀티 Action Skip IO__SystemA_01"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/7 멀티 Action Skip IO__SystemA_01.ds
    [device file="./dsLib/AutoGen/7 멀티 Action Skip IO__SystemA_02.ds"] "7 멀티 Action Skip IO__SystemA_02"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/7 멀티 Action Skip IO__SystemA_02.ds
    [device file="./dsLib/AutoGen/7 멀티 Action Skip IO__SystemA_03.ds"] "7 멀티 Action Skip IO__SystemA_03"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/7 멀티 Action Skip IO__SystemA_03.ds
    [device file="./dsLib/AutoGen/7 멀티 Action Skip IO__SystemA_04.ds"] "7 멀티 Action Skip IO__SystemA_04"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/7 멀티 Action Skip IO__SystemA_04.ds
    [device file="./dsLib/AutoGen/7 멀티 Action Skip IO__SystemB.ds"] "7 멀티 Action Skip IO__SystemB"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/7 멀티 Action Skip IO__SystemB.ds
    [device file="./dsLib/AutoGen/8 Action 인터페이스 옵션__System1.ds"] "8 Action 인터페이스 옵션__System1"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/8 Action 인터페이스 옵션__System1.ds
    [device file="./dsLib/AutoGen/9 Action 출력 옵션__System1.ds"] "9 Action 출력 옵션__System1"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/9 Action 출력 옵션__System1.ds
    [device file="./dsLib/Cylinder/DoubleCylinder.ds"] 
        "9 Action 출력 옵션__System2",
        "1 외부 주소__Device1",
        "6 심볼 정의__Device1",
        "8 Error 설정시간__DEV1",
        "8 Error 설정시간__DEV3",
        "8 Error 설정시간__DEV4_01",
        "8 Error 설정시간__DEV4_02",
        "8 Error 설정시간__DEV2"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/Cylinder/DoubleCylinder.ds
    [device file="./dsLib/AutoGen/10 Action 설정 값__System1.ds"] "10 Action 설정 값__System1"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/10 Action 설정 값__System1.ds
    [device file="./dsLib/AutoGen/11 외부 행위 (Action) 배치__System1.ds"] "11 외부 행위 (Action) 배치__System1"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/11 외부 행위 (Action) 배치__System1.ds
    [device file="./dsLib/AutoGen/13 행위 사용 안함__Action.ds"] "13 행위 사용 안함__Action"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/13 행위 사용 안함__Action.ds
    [device file="./dsLib/AutoGen/16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)__System1.ds"] "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)__System1"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)__System1.ds
    [device file="./dsLib/AutoGen/9 Negative 입력 옵션__System1.ds"] "9 Negative 입력 옵션__System1"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/9 Negative 입력 옵션__System1.ds
}
//DS Library Date = [Library Release Date 24.3.26]
"""

    let answerCylinderText = """
[sys] Cylinder = {
    [flow] F = {
        Vp <|> Vm |> Pp |> Sm;
        Vp |> Pm |> Sp;
        Vm > Pm > Sm;
        Vp > Pp > Sp;
    }
    [interfaces] = {
        "+" = { F.Vp ~ F.Sp }
        "-" = { F.Vm ~ F.Sm }
        "+" <|> "-";
    }
}
"""

    let answerSplittedMRIEdgesText = """
[sys] A = {
    [flow] F = {
        a1 <|> a2;
        a3 |> a2 |> a3 <|> a4;
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

    let answerAutoPreValid = """
[sys] HelloDS = {
    [flow] STN2 = {
        Work1 = {
            Device11111.ADV > Device11111.RET > Device11111_ADV_1 > Device11111_RET_1;		// Device11111.ADV(Call)> Device11111.RET(Call) > Device11111_ADV_1(Alias) > Device11111_RET_1(Alias);
        }
        [aliases] = {
            Work1.Device11111.ADV = { Device11111_ADV_1; }
            Work1.Device11111.RET = { Device11111_RET_1; }
        }
    }
    [jobs] = {
        STN2.Device11111.ADV[N3(2, 2)] = { STN2_Device11111_01.ADV(IB0.0, OB0.0); STN2_Device11111_02.ADV(IB0.2, OB0.2); STN2_Device11111_03.ADV(-, -); }
        STN2.Device11111.RET[N3(3, 1)] = { STN2_Device11111_01.RET(IB0.1, OB0.1); STN2_Device11111_02.RET(IB0.3, -); STN2_Device11111_03.RET(IB0.4, -); }
    }
    [prop] = {
        [autopre] = {
            STN2.Work1.Device11111.ADV = { STN2.Device11111.RET; }
        }
        [layouts] = {
            STN2_Device11111_01 = (1369, 815, 220, 80);
            STN2_Device11111_02 = (1369, 815, 220, 80);
            STN2_Device11111_03 = (1369, 815, 220, 80);
        }
    }
    [device file="./dsLib/Cylinder/DoubleCylinder.ds"] 
        STN2_Device11111_01,
        STN2_Device11111_02,
        STN2_Device11111_03; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/Cylinder/DoubleCylinder.ds
}
"""
    
    let answerLayoutValid = """
[sys] L = {
    [flow] F = {
        A.p > Main;		// A.p(Call)> Main(Real);
        A.m > Main2;		// A.m(Call)> Main2(Real);
        Main = {
            A.p > A.m;		// A.p(Call)> A.m(Call);
        }
        Main2 = {
            A.p > A.m;		// A.p(Call)> A.m(Call);
        }
    }
    [jobs] = {
        F.A.m = { A."-"(%I2, %Q2); }
        F.A.p = { A."+"(%I1, %Q1); }
        F.B.m = { B."-"(%I4, %Q4); }
        F.B.p = { B."+"(%I3, %Q3); }
    }
    [prop] = {
        [layouts] = {
            A = (945, 123, 45, 67);
        }
    }
    [device file="cylinder.ds"] 
        A, 
        B; 
}
"""
    let answerFinishValid = """
[sys] L = {
    [flow] F = {
        A.p > Main;		// A.p(Call)> Main(Real);
        A.m > Main2;		// A.m(Call)> Main2(Real);
        Main = {
            A.p > A.m;		// A.p(Call)> A.m(Call);
        }
        Main2 = {
            A.p > A.m;		// A.p(Call)> A.m(Call);
        }
    }
    [jobs] = {
        F.A.m = { A."-"(%I2, %Q2); }
        F.A.p = { A."+"(%I1, %Q1); }
    }
    [prop] = {
        [finish] = {
            F.Main;
            F.Main2;
        }
    }
    [device file="cylinder.ds"] A; // E:\projects\dualsoft\ds\DsDotNet\src\UnitTest\UnitTest.Engine\Model/../../UnitTest.Model/cylinder.ds
}
"""
    let answerDisableValid = """
[sys] HelloDS = {
    [flow] STN1 = {
        Work1 => Work2 => Work1;		// Work1(Real)=> Work2(Real) => Work1(Real);
        외부시작."ADV(INTrue)" > Work1_1;		// 외부시작."ADV(INTrue)"(Call)> Work1_1(Alias);
        Work1 = {
            Device1.ADV > Device2.ADV;		// Device1.ADV(Call)> Device2.ADV(Call);
            Device2_ADV_1; // island
        }
        [aliases] = {
            Work1 = { Work1_1; }
            Work1.Device2.ADV = { Device2_ADV_1; }
        }
    }
    [jobs] = {
        STN1.외부시작."ADV(INTrue)" = { STN1_외부시작.ADV(IB0.2, -); }
        STN1.Device1.ADV = { STN1_Device1.ADV(IB0.0, OB0.0); }
        STN1.Device2.ADV = { STN1_Device2.ADV(IB0.1, OB0.1); }
    }
    [prop] = {
        [disable] = {
            STN1.Work1."Device2.ADV";
        }
    }
    [device file="./dsLib/Cylinder/DoubleCylinder.ds"] 
        STN1_외부시작,
        STN1_Device1,
        STN1_Device2; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/Cylinder/DoubleCylinder.ds
}
//DS Language Version = [1.0.0.1]
//DS Library Date = [Library Release Date 24.3.26]
//DS Engine Version = [0.9.9.1]
"""
    let answerDuplicatedEdgesText = """
[sys] B = {
    [flow] F = {
        Vp |> Pp;
        Vp > Pp;
    }
}
"""
   

    let answerAdoptoedValidText = """

[sys] My = {
    [flow] F = {
        Seg1 > Seg2;		// Seg1(Real)> Seg2(Real);
        Seg1 = {
            A.p > A.m;		// A.p(Call)> A.m(Call);
        }
    }
    [flow] F2 = {
        F_Seg1 > Seg;		// F_Seg1(Alias)> Seg(Real);
        Seg = {
            A.p > A.m;		// A.p(Call)> A.m(Call);
        }
        [aliases] = {
            F.Seg1 = { F_Seg1; }
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
        A2; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/cylinder.ds
}
"""



[<AutoOpen>]
module ModelComponentAnswers =
    let answerStrongCausal = """
[sys] L = {
[flow] F = {
    Main = {
        A.p <|| A.m;
        A.p ||> A.m;
        A.p >> A.m;
    }
}
[jobs] = {
    F.A.p = { A."+"(%I1, %Q1); }
    F.A.m = { A."-"(%I2, %Q2); }
}
[device file="cylinder.ds"] A;
}
"""
    let answerConditions = """
[sys] HelloDS_DATA = {
    [flow] f1 = {
        Work1 > Work2;		// Work1(Real)> Work2(Real);
    }
    [conditions] = {
        [r] = {
            f1_Condition1(_, _) = { f1; }
            f1_Condition2(_, _) = { f1; }
        }
    }
}
"""
    let answerLamps= """
[sys] HelloDS_DATA = {
    [flow] f1 = {
        Work1 > Work2;		// Work1(Real)> Work2(Real);
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
}
"""

    let answerButtons = """
[sys] HelloDS_DATA = {
    [flow] f1 = {
        Work1 > Work2;		// Work1(Real)> Work2(Real);
    }
    [buttons] = {
        [a] = { AutoSelect(_, -) = { f1; } }
        [m] = { ManualSelect(_, -) = { f1; } }
        [d] = { DrivePushBtn(_, -) = { f1; } }
        [e] = { EmergencyBtn(_, -) = { f1; } }
        [p] = { PausePushBtn(_, -) = { f1; } }
        [c] = { ClearPushBtn(_, -) = { f1; } }
    }
}
"""

    let answerTaskLinkorDevice = """
    [sys] Control = {
    [flow] F = {
        Main <|> Reset;
        FWD <| Main |> BWD;
        FWD > BWD > Main |> FWD2 |> BWD2;
        Main = {
            mv1up > mv1dn;
        }
        [aliases] = {
            FWD = { FWD2; }
            BWD = { BWD2; }
        }
    }
    [jobs] = {
        mv1up = { A."+"(%I300, %Q300); }
        mv1dn = { A."-"(%I301, %Q301); }
        FWD = sysR.RUN;
        BWD = sysR.RUN;
    }
    [interfaces] = {
        G = { F.Main ~ F.Main }
        R = { F.Reset ~ F.Reset }
        G <|> R;
    }
    [device file="cylinder.ds"] A;
    [external file="systemRH.ds"] sysR;
    [external file="systemLH.ds"] sysL;
}
"""
    let answerAliases =  """
[sys] HelloDS = {
    [flow] STN1 = {
        Work1 => Work2 => Work1;		// Work1(Real)=> Work2(Real) => Work1(Real);
        외부시작."ADV(INTrue)" > Work1_1;		// 외부시작."ADV(INTrue)"(Call)> Work1_1(Alias);
        Work1 = {
            Device1.ADV > Device2.ADV;		// Device1.ADV(Call)> Device2.ADV(Call);
            Device2_ADV_1; // island
        }
        [aliases] = {
            Work1 = { Work1_1; }
            Work1.Device2.ADV = { Device2_ADV_1; }
        }
    }
    [jobs] = {
        STN1.외부시작."ADV(INTrue)" = { STN1_외부시작.ADV(IB0.2, -); }
        STN1.Device1.ADV = { STN1_Device1.ADV(IB0.0, OB0.0); }
        STN1.Device2.ADV = { STN1_Device2.ADV(IB0.1, OB0.1); }
    }
    [device file="./dsLib/Cylinder/DoubleCylinder.ds"] 
        STN1_외부시작,
        STN1_Device1,
        STN1_Device2; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/Cylinder/DoubleCylinder.ds
}
//DS Language Version = [1.0.0.1]
//DS Library Date = [Library Release Date 24.3.26]
//DS Engine Version = [0.9.9.1]
 """


    let answerCircularDependency = """
[sys] My = {
    [flow] F = {
        Seg1 > Seg2;		// Seg1(Real)> Seg2(Real);
        Seg1 = {
            Run.R > Run.L;		// Run.R(Call)> Run.L(Call);
        }
    }
    [jobs] = {
        F.Run.R = { sysR.RUN(%I1, %Q1); }
        F.Run.L = { sysL.RUN(%I2, %Q2); }
    }
    [external file="systemRH.ds"] sysR; 
    [external file="systemLH.ds"] sysL; 
}
"""

    let linkAndLinkAliases = """
[sys] Control = {
    [flow] F = {
		Main = { mv1up, mv2dn > mv1dn, mv2up; }
		FWD > BWD > Main;
		Main <|> Reset;
		
		[aliases] = {
			FWD = { FWD2; }
			BWD = { BWD2; }
		}
    }
    [interfaces] = {
        G = { F.Main ~ F.Main }
        R = { F.Reset ~ F.Reset }
        G <|> R;
    }
	[jobs] = {
		mv1up = { M1.Up(%I300, %Q300); }
		mv1dn = { M1.Dn(%I301, %Q301); }
		mv2up = { M2.Up(%I302, %Q302); }
		mv2dn = { M2.Dn(%I303, %Q303); }
		FWD = Mt.fwd;
		BWD = Mt.bwd;
	}
    [external file=""HmiCodeGenExA.mple/test_sA.mple/device/MovingLifter1.ds""] M1;
    [external file=""HmiCodeGenExA.mple/test_sA.mple/device/MovingLifter2.ds""] M2;
	[external file=""HmiCodeGenExA.mple/test_sA.mple/device/motor.ds""] Mt;
}
"""
