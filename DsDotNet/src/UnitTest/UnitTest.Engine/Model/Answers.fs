namespace T

[<AutoOpen>]
module ModelAnswers =
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
    let answerEveryScenarioText = """
[sys] DS_Units_V9 = {
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
        #"2 행위 (Action) 배치_전원" > 드릴작업;		// #"2 행위 (Action) 배치_전원"(Call)> 드릴작업(Real);
        공급작업 = {
            RBT.투입, RBT.홈; // island
        }
    }
    [flow] "2 행위 (Action) 배치 유닛" = {
        #"2 행위 (Action) 배치 유닛_전원" > 드릴작업;		// #"2 행위 (Action) 배치 유닛_전원"(Call)> 드릴작업(Real);
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
        #"3 작업 (Work) 타입_전원" > 드릴작업;		// #"3 작업 (Work) 타입_전원"(Call)> 드릴작업(Real);
        공급작업 = {
            RBT.투입, RBT.홈; // island
        }
        Flow2, Flow1; // island
    }
    [flow] "3 작업 (Work) 타입 유닛" = {
        #"3 작업 (Work) 타입 유닛_전원" > 드릴작업;		// #"3 작업 (Work) 타입 유닛_전원"(Call)> 드릴작업(Real);
        공급작업 = {
            RBT.투입, RBT.홈; // island
        }
        Work1, Flow2, Flow1, "3 작업 (Work) 타입_드릴작업"; // island
        [aliases] = {
            "3 작업 (Work) 타입".드릴작업 = { "3 작업 (Work) 타입_드릴작업"; }
        }
    }
    [flow] "4 행위 (Action) 타입" = {
        #"4 행위 (Action) 타입_전원" > 드릴작업;		// #"4 행위 (Action) 타입_전원"(Call)> 드릴작업(Real);
        공급작업 = {
            RBT.투입, RBT.홈; // island
        }
    }
    [flow] "4 행위 (Action) 타입 유닛" = {
        #"4 행위 (Action) 타입 유닛_Action1" > 드릴작업1;		// #"4 행위 (Action) 타입 유닛_Action1"(Call)> 드릴작업1(Real);
        #"4 행위 (Action) 타입 유닛_전원" > 드릴작업;		// #"4 행위 (Action) 타입 유닛_전원"(Call)> 드릴작업(Real);
        공급작업 = {
            RBT.투입, RBT.홈; // island
        }
        드릴작업1 = {
            System1.Api1; // island
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
        #"1 기본 연결 Unit_전원" > 드릴작업;		// #"1 기본 연결 Unit_전원"(Call)> 드릴작업(Real);
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
        #"2 StartReset 연결 Unit_전원" > 드릴작업;		// #"2 StartReset 연결 Unit_전원"(Call)> 드릴작업(Real);
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
        #"3 Interlock 연결 Unit_전원" > 드릴작업;		// #"3 Interlock 연결 Unit_전원"(Call)> 드릴작업(Real);
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
        #"4 SelfReset 연결 Unit_전원" > 드릴작업 =|> 드릴작업클리어;		// #"4 SelfReset 연결 Unit_전원"(Call)> 드릴작업(Real) =|> 드릴작업클리어(Real);
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
        #"5 Group 연결 Unit_전원" > 드릴작업;		// #"5 Group 연결 Unit_전원"(Call)> 드릴작업(Real);
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
            SystemB."Api2(IN230 < x < 3214_OUT500)"; // island
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
            System1."Api1[-]"; // island
        }
        Work2 = {
            System1."Api1[P]"; // island
        }
    }
    [flow] "10 Action 설정 값" = {
        Work1 = {
            System1.Api1; // island
        }
        Work2 = {
            System1."Api2(IN100_OUT500)"; // island
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
        #"12 내부 행위 (Action) 배치_Action3" > Work1_1;		// #"12 내부 행위 (Action) 배치_Action3"(Call)> Work1_1(Alias);
        Work1 = {
            "12 내부 행위 (Action) 배치_Action1"(), "12 내부 행위 (Action) 배치_Action2"(); // island
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
            Action."1"; // island
        }
    }
    [flow] "14 Work 설정시간" = {
        Work1, Work2, Work3; // island
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
        #"2 내부 변수_상수_Operator" > Work1;		// #"2 내부 변수_상수_Operator"(Call)> Work1(Real);
        Work1 = {
            "2 내부 변수_상수_Command"(); // island
        }
    }
    [flow] "3 내부 연산_명령" = {
        #"3 내부 연산_명령_Operator" > Work1;		// #"3 내부 연산_명령_Operator"(Call)> Work1(Real);
        Work1 = {
            "3 내부 연산_명령_Command2"(); // island
        }
    }
    [flow] "4 버튼 IO" = {
    }
    [flow] "5 램프 IO" = {
    }
    [flow] "6 심볼 정의" = {
        #"6 심볼 정의_Operator" > Work1;		// #"6 심볼 정의_Operator"(Call)> Work1(Real);
        Work1 = {
            "6 심볼 정의_Command"(); // island
        }
        Work2 = {
            Device1.ADV > Device1."RET(OUT300)";		// Device1.ADV(Call)> Device1."RET(OUT300)"(Call);
        }
    }
    [flow] "7 외부호출" = {
        Script1, Script2, MotionRET, MotionADV; // island
    }
    [jobs] = {
        "1 작업 및 행위".RBT.투입 = (%IX0.0.7, %QX0.1.7);
        "1 작업 및 행위".RBT.홈 = (%IX0.0.8, %QX0.1.8);
        "1 작업 및 행위 유닛".Device.Action1 = (%IX0.0.4, -);
        "1 작업 및 행위 유닛".RBT.투입 = (%IX0.0.5, %QX0.1.5);
        "1 작업 및 행위 유닛".RBT.홈 = (%IX0.0.6, %QX0.1.6);
        "2 행위 (Action) 배치".RBT.투입 = (%IX0.0.21, %QX0.1.21);
        "2 행위 (Action) 배치".RBT.홈 = (%IX0.0.22, %QX0.1.22);
        "2 행위 (Action) 배치 유닛".Device.Action1 = (%IX0.0.17, %QX0.1.17);
        "2 행위 (Action) 배치 유닛".Device.Action2 = (%IX0.0.18, %QX0.1.18);
        "2 행위 (Action) 배치 유닛".RBT.투입 = (%IX0.0.19, %QX0.1.19);
        "2 행위 (Action) 배치 유닛".RBT.홈 = (%IX0.0.20, %QX0.1.20);
        "3 작업 (Work) 타입".RBT.투입 = (%IX0.0.29, %QX0.1.29);
        "3 작업 (Work) 타입".RBT.홈 = (%IX0.0.30, %QX0.1.30);
        "3 작업 (Work) 타입 유닛".RBT.투입 = (%IX0.0.27, %QX0.1.27);
        "3 작업 (Work) 타입 유닛".RBT.홈 = (%IX0.0.28, %QX0.1.28);
        "4 행위 (Action) 타입".RBT.투입 = (%IX0.0.38, %QX0.1.38);
        "4 행위 (Action) 타입".RBT.홈 = (%IX0.0.39, %QX0.1.39);
        "4 행위 (Action) 타입 유닛".System1.Api1 = (%IX0.0.37, %QX0.1.37);
        "4 행위 (Action) 타입 유닛".RBT.투입 = (%IX0.0.35, %QX0.1.35);
        "4 행위 (Action) 타입 유닛".RBT.홈 = (%IX0.0.36, %QX0.1.36);
        "5 시스템 인터페이스".드릴장치.드릴링A위치 = (%IX0.0.49, %QX0.1.49);
        "5 시스템 인터페이스".드릴장치.드릴링B위치 = (%IX0.0.50, %QX0.1.50);
        "5 시스템 인터페이스 유닛".드릴장치.드릴링A위치 = (%IX0.0.46, %QX0.1.46);
        "5 시스템 인터페이스 유닛".드릴장치.드릴링B위치 = (%IX0.0.47, %QX0.1.47);
        "5 시스템 인터페이스 유닛".Device1.Api1 = (%IX0.0.48, -);
        "1 기본 연결 Unit".드릴장치.드릴링A위치 = (%IX0.0.0, %QX0.1.0);
        "1 기본 연결 Unit".드릴장치.드릴링B위치 = (%IX0.0.1, %QX0.1.1);
        "1 기본 연결 Unit".RBT.투입 = (%IX0.0.2, %QX0.1.2);
        "1 기본 연결 Unit".RBT.홈 = (%IX0.0.3, %QX0.1.3);
        "2 StartReset 연결 Unit".드릴장치.드릴링A위치 = (%IX0.0.23, %QX0.1.23);
        "2 StartReset 연결 Unit".드릴장치.드릴링B위치 = (%IX0.0.24, %QX0.1.24);
        "2 StartReset 연결 Unit".RBT.투입 = (%IX0.0.25, %QX0.1.25);
        "2 StartReset 연결 Unit".RBT.홈 = (%IX0.0.26, %QX0.1.26);
        "3 Interlock 연결 Unit".드릴장치.드릴링A위치 = (%IX0.0.31, %QX0.1.31);
        "3 Interlock 연결 Unit".드릴장치.드릴링B위치 = (%IX0.0.32, %QX0.1.32);
        "3 Interlock 연결 Unit".RBT.투입 = (%IX0.0.33, %QX0.1.33);
        "3 Interlock 연결 Unit".RBT.홈 = (%IX0.0.34, %QX0.1.34);
        "4 SelfReset 연결 Unit".드릴장치.드릴링A위치 = (%IX0.0.42, %QX0.1.42);
        "4 SelfReset 연결 Unit".드릴장치.드릴링B위치 = (%IX0.0.43, %QX0.1.43);
        "4 SelfReset 연결 Unit".RBT.투입 = (%IX0.0.44, %QX0.1.44);
        "4 SelfReset 연결 Unit".RBT.홈 = (%IX0.0.45, %QX0.1.45);
        "5 Group 연결 Unit".드릴장치.드릴링A위치 = (%IX0.0.51, %QX0.1.51);
        "5 Group 연결 Unit".드릴장치.드릴링B위치 = (%IX0.0.52, %QX0.1.52);
        "5 Group 연결 Unit".RBT.투입 = (%IX0.0.53, %QX0.1.53);
        "5 Group 연결 Unit".RBT.홈 = (%IX0.0.54, %QX0.1.54);
        "4 Safety 조건".System1.Api1 = (%IX0.0.40, %QX0.1.40);
        "4 Safety 조건".System1.Api2 = (%IX0.0.41, %QX0.1.41);
        "6 멀티 Action".System1.Api1 = (%IX0.0.59, %QX0.1.59);
        "6 멀티 Action".System.Api[N4(4, 4)] = { "6 멀티 Action__System_01".Api(%IX0.0.55, %QX0.1.55); "6 멀티 Action__System_02".Api(%IX0.0.56, %QX0.1.56); "6 멀티 Action__System_03".Api(%IX0.0.57, %QX0.1.57); "6 멀티 Action__System_04".Api(%IX0.0.58, %QX0.1.58); }
        "7 멀티 Action Skip IO".SystemA.Api[N4(4, 4)] = { "7 멀티 Action Skip IO__SystemA_01".Api(%IX0.0.60, %QX0.1.60); "7 멀티 Action Skip IO__SystemA_02".Api(%IX0.0.61, %QX0.1.61); "7 멀티 Action Skip IO__SystemA_03".Api(%IX0.0.62, %QX0.1.62); "7 멀티 Action Skip IO__SystemA_04".Api(%IX0.0.63, %QX0.1.63); }
        "7 멀티 Action Skip IO".SystemB."Api2(IN230 < x < 3214_OUT500)" = (%ID1025:230 < x < 3214, %QD1025:500);
        "8 Action 인터페이스 옵션".System1.Api1[N1(1, 0)] = { "8 Action 인터페이스 옵션__System1".Api1(%IX0.2.0, -); }
        "8 Action 인터페이스 옵션".System1.Api2[N1(0, 1)] = { "8 Action 인터페이스 옵션__System1".Api2(-, %QX0.3.0); }
        "8 Action 인터페이스 옵션".System1.Api3[N1(0, 0)] = { "8 Action 인터페이스 옵션__System1".Api3(-, -); }
        "8 Action 인터페이스 옵션".System1.Api4[N1(0, 0)] = { "8 Action 인터페이스 옵션__System1".Api4(-, -); }
        "9 Action 출력 옵션".System1."Api1[-]" = (%IX0.2.1, %QX0.3.1);
        "9 Action 출력 옵션".System1."Api1[P]" = (%IX0.2.2, %QX0.3.2);
        "10 Action 설정 값".System1.Api1 = (%IX0.0.9, %QX0.1.9);
        "10 Action 설정 값".System1."Api2(IN100_OUT500)" = (%ID1024:100, %QD1024:500);
        "11 외부 행위 (Action) 배치".System1.Api1 = (%IX0.0.10, %QX0.1.10);
        "11 외부 행위 (Action) 배치".System1.Api2 = (%IX0.0.11, %QX0.1.11);
        "11 외부 행위 (Action) 배치".System1.Api3 = (%IX0.0.12, -);
        "13 행위 사용 안함".Action."1" = (%IX0.0.13, %QX0.1.13);
        "13 행위 사용 안함".Action."2" = (%IX0.0.14, %QX0.1.14);
        "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)".System1.Api1 = (%IX0.0.15, %QX0.1.15);
        "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)".System1.Api2 = (%IX0.0.16, %QX0.1.16);
        "1 외부 주소".Device1.ADV = (P00000, P00040);
        "1 외부 주소".Device1.RET = (P00001, P00041);
        "6 심볼 정의".Device1.ADV = (P00000:Dev1ADV_I:Boolean, P00040:Dev1ADV_O:Boolean);
        "6 심볼 정의".Device1."RET(OUT300)" = (P00001:Dev1RET_I:Boolean, P00041:Dev1RET_O:Int32:300);
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
        "2 행위 (Action) 배치_전원";
        "2 행위 (Action) 배치 유닛_전원";
        "3 작업 (Work) 타입_전원";
        "3 작업 (Work) 타입 유닛_전원";
        "4 행위 (Action) 타입_전원";
        "4 행위 (Action) 타입 유닛_Action1";
        "4 행위 (Action) 타입 유닛_전원";
        "1 기본 연결 Unit_전원";
        "2 StartReset 연결 Unit_전원";
        "3 Interlock 연결 Unit_전원";
        "4 SelfReset 연결 Unit_전원";
        "5 Group 연결 Unit_전원";
        "12 내부 행위 (Action) 배치_Action3";
        "2 내부 변수_상수_Operator";
        "3 내부 연산_명령_Operator";
        "6 심볼 정의_Operator";
        Operator2 = #{$VARIABLE4 !=$PI_PI_PI;}
        Operator3 = #{$Dev1ADV_I == false;}
    }
    [commands] = {
        "12 내부 행위 (Action) 배치_Action1";
        "12 내부 행위 (Action) 배치_Action2";
        "2 내부 변수_상수_Command";
        "3 내부 연산_명령_Command2";
        "6 심볼 정의_Command";
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
            AutoSelect(%MX1000, -) = { "시스템 모델링"; "모델링 기본 구성"; "모델링 확장 구성1"; "모델링 확장 구성2"; "모델링 구조 Unit"; "기본 도형 Unit"; "1 작업 및 행위"; "1 작업 및 행위 유닛"; "2 행위 (Action) 배치"; "2 행위 (Action) 배치 유닛"; "3 작업 (Work) 타입"; "3 작업 (Work) 타입 유닛"; "4 행위 (Action) 타입"; "4 행위 (Action) 타입 유닛"; "5 시스템 인터페이스"; "5 시스템 인터페이스 유닛"; "기본 연결 Unit"; "1 기본 연결 Unit"; "2 StartReset 연결 Unit"; "3 Interlock 연결 Unit"; "4 SelfReset 연결 Unit"; "5 Group 연결 Unit"; "확장 도형 Unit"; "1 외부 시스템 로딩"; "2 시스템 버튼 램프"; "2 시스템 버튼 램프 유닛"; "3 시스템 버튼 램프 확장"; "4 시스템 외부조건"; "4 시스템 외부조건 유닛"; "5 시스템 외부액션 유닛"; "7 시스템 외부액션 타겟 Value"; "4 Safety 조건"; "5 Work 초기조건"; "6 멀티 Action"; "7 멀티 Action Skip IO"; "8 Action 인터페이스 옵션"; "9 Action 출력 옵션"; "10 Action 설정 값"; "11 외부 행위 (Action) 배치"; "12 내부 행위 (Action) 배치"; "13 행위 사용 안함"; "14 Work 설정시간"; "15 Work 데이터전송"; "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)"; "IO Table"; "1 외부 주소"; "2 내부 변수_상수"; "3 내부 연산_명령"; "4 버튼 IO"; "5 램프 IO"; "6 심볼 정의"; "7 외부호출"; }
            AutoBTN1(%MX1001, -) = { "2 시스템 버튼 램프 유닛"; "3 시스템 버튼 램프 확장"; }
            AutoBTN2(M00628, -) = { "4 버튼 IO"; }
        }
        [m] = {
            ManualSelect(%MX1002, -) = { "시스템 모델링"; "모델링 기본 구성"; "모델링 확장 구성1"; "모델링 확장 구성2"; "모델링 구조 Unit"; "기본 도형 Unit"; "1 작업 및 행위"; "1 작업 및 행위 유닛"; "2 행위 (Action) 배치"; "2 행위 (Action) 배치 유닛"; "3 작업 (Work) 타입"; "3 작업 (Work) 타입 유닛"; "4 행위 (Action) 타입"; "4 행위 (Action) 타입 유닛"; "5 시스템 인터페이스"; "5 시스템 인터페이스 유닛"; "기본 연결 Unit"; "1 기본 연결 Unit"; "2 StartReset 연결 Unit"; "3 Interlock 연결 Unit"; "4 SelfReset 연결 Unit"; "5 Group 연결 Unit"; "확장 도형 Unit"; "1 외부 시스템 로딩"; "2 시스템 버튼 램프"; "2 시스템 버튼 램프 유닛"; "3 시스템 버튼 램프 확장"; "4 시스템 외부조건"; "4 시스템 외부조건 유닛"; "5 시스템 외부액션 유닛"; "7 시스템 외부액션 타겟 Value"; "4 Safety 조건"; "5 Work 초기조건"; "6 멀티 Action"; "7 멀티 Action Skip IO"; "8 Action 인터페이스 옵션"; "9 Action 출력 옵션"; "10 Action 설정 값"; "11 외부 행위 (Action) 배치"; "12 내부 행위 (Action) 배치"; "13 행위 사용 안함"; "14 Work 설정시간"; "15 Work 데이터전송"; "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)"; "IO Table"; "1 외부 주소"; "2 내부 변수_상수"; "3 내부 연산_명령"; "4 버튼 IO"; "5 램프 IO"; "6 심볼 정의"; "7 외부호출"; }
            ManualBTN1(%MX1003, -) = { "2 시스템 버튼 램프 유닛"; "3 시스템 버튼 램프 확장"; }
            ManualBTN2(M00629, -) = { "4 버튼 IO"; }
        }
        [d] = {
            DrivePushBtn(%MX1004, -) = { "시스템 모델링"; "모델링 기본 구성"; "모델링 확장 구성1"; "모델링 확장 구성2"; "모델링 구조 Unit"; "기본 도형 Unit"; "1 작업 및 행위"; "1 작업 및 행위 유닛"; "2 행위 (Action) 배치"; "2 행위 (Action) 배치 유닛"; "3 작업 (Work) 타입"; "3 작업 (Work) 타입 유닛"; "4 행위 (Action) 타입"; "4 행위 (Action) 타입 유닛"; "5 시스템 인터페이스"; "5 시스템 인터페이스 유닛"; "기본 연결 Unit"; "1 기본 연결 Unit"; "2 StartReset 연결 Unit"; "3 Interlock 연결 Unit"; "4 SelfReset 연결 Unit"; "5 Group 연결 Unit"; "확장 도형 Unit"; "1 외부 시스템 로딩"; "2 시스템 버튼 램프"; "2 시스템 버튼 램프 유닛"; "3 시스템 버튼 램프 확장"; "4 시스템 외부조건"; "4 시스템 외부조건 유닛"; "5 시스템 외부액션 유닛"; "7 시스템 외부액션 타겟 Value"; "4 Safety 조건"; "5 Work 초기조건"; "6 멀티 Action"; "7 멀티 Action Skip IO"; "8 Action 인터페이스 옵션"; "9 Action 출력 옵션"; "10 Action 설정 값"; "11 외부 행위 (Action) 배치"; "12 내부 행위 (Action) 배치"; "13 행위 사용 안함"; "14 Work 설정시간"; "15 Work 데이터전송"; "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)"; "IO Table"; "1 외부 주소"; "2 내부 변수_상수"; "3 내부 연산_명령"; "4 버튼 IO"; "5 램프 IO"; "6 심볼 정의"; "7 외부호출"; }
            DriveBTN1(%MX1005, -) = { "2 시스템 버튼 램프 유닛"; "3 시스템 버튼 램프 확장"; }
            DriveBTN2(M0062A, -) = { "4 버튼 IO"; }
        }
        [e] = {
            EmergencyBtn(%MX1006, -) = { "시스템 모델링"; "모델링 기본 구성"; "모델링 확장 구성1"; "모델링 확장 구성2"; "모델링 구조 Unit"; "기본 도형 Unit"; "1 작업 및 행위"; "1 작업 및 행위 유닛"; "2 행위 (Action) 배치"; "2 행위 (Action) 배치 유닛"; "3 작업 (Work) 타입"; "3 작업 (Work) 타입 유닛"; "4 행위 (Action) 타입"; "4 행위 (Action) 타입 유닛"; "5 시스템 인터페이스"; "5 시스템 인터페이스 유닛"; "기본 연결 Unit"; "1 기본 연결 Unit"; "2 StartReset 연결 Unit"; "3 Interlock 연결 Unit"; "4 SelfReset 연결 Unit"; "5 Group 연결 Unit"; "확장 도형 Unit"; "1 외부 시스템 로딩"; "2 시스템 버튼 램프"; "2 시스템 버튼 램프 유닛"; "3 시스템 버튼 램프 확장"; "4 시스템 외부조건"; "4 시스템 외부조건 유닛"; "5 시스템 외부액션 유닛"; "7 시스템 외부액션 타겟 Value"; "4 Safety 조건"; "5 Work 초기조건"; "6 멀티 Action"; "7 멀티 Action Skip IO"; "8 Action 인터페이스 옵션"; "9 Action 출력 옵션"; "10 Action 설정 값"; "11 외부 행위 (Action) 배치"; "12 내부 행위 (Action) 배치"; "13 행위 사용 안함"; "14 Work 설정시간"; "15 Work 데이터전송"; "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)"; "IO Table"; "1 외부 주소"; "2 내부 변수_상수"; "3 내부 연산_명령"; "4 버튼 IO"; "5 램프 IO"; "6 심볼 정의"; "7 외부호출"; }
            EmergencyBTN1(%MX1007, -) = { "2 시스템 버튼 램프 유닛"; }
            "EmergencyBTN1 "(%MX1008:False, -) = { "3 시스템 버튼 램프 확장"; }
            EmergencyBTN2(M0062D, -) = { "4 버튼 IO"; }
        }
        [t] = {
            TestBTN1(%MX1009, -) = { "2 시스템 버튼 램프 유닛"; }
            "TestBTN1 "(%MD2048:777, -) = { "3 시스템 버튼 램프 확장"; }
            TestBTN2(M0062C, -) = { "4 버튼 IO"; }
        }
        [r] = {
            ReadyBTN1(%MX1056, -) = { "2 시스템 버튼 램프 유닛"; }
            "ReadyBTN1 "(%MD2112:10 < x < 333, -) = { "3 시스템 버튼 램프 확장"; }
            ReadyBTN2(M0062C, -) = { "4 버튼 IO"; }
        }
        [p] = {
            PausePushBtn(%MX1120, -) = { "시스템 모델링"; "모델링 기본 구성"; "모델링 확장 구성1"; "모델링 확장 구성2"; "모델링 구조 Unit"; "기본 도형 Unit"; "1 작업 및 행위"; "1 작업 및 행위 유닛"; "2 행위 (Action) 배치"; "2 행위 (Action) 배치 유닛"; "3 작업 (Work) 타입"; "3 작업 (Work) 타입 유닛"; "4 행위 (Action) 타입"; "4 행위 (Action) 타입 유닛"; "5 시스템 인터페이스"; "5 시스템 인터페이스 유닛"; "기본 연결 Unit"; "1 기본 연결 Unit"; "2 StartReset 연결 Unit"; "3 Interlock 연결 Unit"; "4 SelfReset 연결 Unit"; "5 Group 연결 Unit"; "확장 도형 Unit"; "1 외부 시스템 로딩"; "2 시스템 버튼 램프"; "2 시스템 버튼 램프 유닛"; "3 시스템 버튼 램프 확장"; "4 시스템 외부조건"; "4 시스템 외부조건 유닛"; "5 시스템 외부액션 유닛"; "7 시스템 외부액션 타겟 Value"; "4 Safety 조건"; "5 Work 초기조건"; "6 멀티 Action"; "7 멀티 Action Skip IO"; "8 Action 인터페이스 옵션"; "9 Action 출력 옵션"; "10 Action 설정 값"; "11 외부 행위 (Action) 배치"; "12 내부 행위 (Action) 배치"; "13 행위 사용 안함"; "14 Work 설정시간"; "15 Work 데이터전송"; "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)"; "IO Table"; "1 외부 주소"; "2 내부 변수_상수"; "3 내부 연산_명령"; "4 버튼 IO"; "5 램프 IO"; "6 심볼 정의"; "7 외부호출"; }
            PauseBTN1(%MX1121, -) = { "2 시스템 버튼 램프 유닛"; "3 시스템 버튼 램프 확장"; }
            PauseBTN2(M0062B, -) = { "4 버튼 IO"; }
        }
        [c] = {
            ClearPushBtn(%MX1122, -) = { "시스템 모델링"; "모델링 기본 구성"; "모델링 확장 구성1"; "모델링 확장 구성2"; "모델링 구조 Unit"; "기본 도형 Unit"; "1 작업 및 행위"; "1 작업 및 행위 유닛"; "2 행위 (Action) 배치"; "2 행위 (Action) 배치 유닛"; "3 작업 (Work) 타입"; "3 작업 (Work) 타입 유닛"; "4 행위 (Action) 타입"; "4 행위 (Action) 타입 유닛"; "5 시스템 인터페이스"; "5 시스템 인터페이스 유닛"; "기본 연결 Unit"; "1 기본 연결 Unit"; "2 StartReset 연결 Unit"; "3 Interlock 연결 Unit"; "4 SelfReset 연결 Unit"; "5 Group 연결 Unit"; "확장 도형 Unit"; "1 외부 시스템 로딩"; "2 시스템 버튼 램프"; "2 시스템 버튼 램프 유닛"; "3 시스템 버튼 램프 확장"; "4 시스템 외부조건"; "4 시스템 외부조건 유닛"; "5 시스템 외부액션 유닛"; "7 시스템 외부액션 타겟 Value"; "4 Safety 조건"; "5 Work 초기조건"; "6 멀티 Action"; "7 멀티 Action Skip IO"; "8 Action 인터페이스 옵션"; "9 Action 출력 옵션"; "10 Action 설정 값"; "11 외부 행위 (Action) 배치"; "12 내부 행위 (Action) 배치"; "13 행위 사용 안함"; "14 Work 설정시간"; "15 Work 데이터전송"; "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)"; "IO Table"; "1 외부 주소"; "2 내부 변수_상수"; "3 내부 연산_명령"; "4 버튼 IO"; "5 램프 IO"; "6 심볼 정의"; "7 외부호출"; }
            ClearBTN1(%MX1123, -) = { "2 시스템 버튼 램프 유닛"; }
            "ClearBTN1 "(%MX1124:False, -) = { "3 시스템 버튼 램프 확장"; }
            ClearBTN2(M0062C, -) = { "4 버튼 IO"; }
        }
        [h] = {
            HomeBTN1(%MX1125, -) = { "2 시스템 버튼 램프 유닛"; }
            "HomeBTN1 "(%MX1126, -) = { "3 시스템 버튼 램프 확장"; }
            HomeBTN2(M0062C, -) = { "4 버튼 IO"; }
        }
    }
    [lamps] = {
        [a] = { AutoModeLamp(-, %MX1127) = {  } }
        [m] = { ManualModeLamp(-, %MX1128) = {  } }
        [d] = { DriveLamp(-, %MX1129) = {  } }
        [e] = { ErrorLamp(-, %MX1130) = {  } }
        [r] = { ReadyStateLamp(-, %MX1131) = {  } }
        [i] = { IdleModeLamp(-, %MX1132) = {  } }
        [o] = { OriginStateLamp(-, %MX1133) = {  } }
    }
    [conditions] = {
        [r] = {
            Condition1(%MX1134, -) = { "4 시스템 외부조건 유닛"; }
            Condition2(%MX1135, -) = { "4 시스템 외부조건 유닛"; }
        }
        [d] = {
            Condition3(%MX1136, -) = { "4 시스템 외부조건 유닛"; }
            Condition4(%MX1137, -) = { "4 시스템 외부조건 유닛"; }
        }
    }
    [actions] = {
        [e] = {
            EmgAction1(-, %MX1138) = { "5 시스템 외부액션 유닛"; }
            EmgAction2(-, %MX1139) = { "5 시스템 외부액션 유닛"; }
            EmgAction3(-, %MX1140:False) = { "7 시스템 외부액션 타겟 Value"; }
            EmgAction4(-, %MD2176:3000) = { "7 시스템 외부액션 타겟 Value"; }
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
            "4 Safety 조건".Work1.System1.Api1 = { "4 Safety 조건".System1.Api2; }
            "4 Safety 조건".Work1.System1.Api2 = { "4 Safety 조건".System1.Api1; }
            "4 Safety 조건".Work2.System1.Api1 = { "6 멀티 Action".System1.Api1; }
        }
        [autopre] = {
            "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)".Work1.System1.Api1 = { "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)".System1.Api2; }
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
            "9 Action 출력 옵션__System1" = (1099, 556, 563, 192);
            "10 Action 설정 값__System1" = (1115, 650, 563, 244);
            "11 외부 행위 (Action) 배치__System1" = (962, 607, 307, 130);
            "13 행위 사용 안함__Action" = (1105, 730, 209, 93);
            "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)__System1" = (127, 640, 439, 94);
            "1 외부 주소__Device1" = (306, 773, 436, 108);
            "6 심볼 정의__Device1" = (1483, 294, 246, 64);
        }
        [motions] = {
            "7 외부호출".MotionRET = {./Assets/Cylinder/DoubleType.obj:RET};
            "7 외부호출".MotionADV = {./Assets/Cylinder/DoubleType.obj:ADV};
        }
        [scripts] = {
            "7 외부호출".Script1 = {ThirdParty.AddressInfo.Provider.testFunc1()};
            "7 외부호출".Script2 = {ThirdParty.AddressInfo.Provider.testFunc2()};
        }
        [times] = {
            "14 Work 설정시간".Work1 = {AVG(0.1)};
            "14 Work 설정시간".Work2 = {AVG(1)};
            "14 Work 설정시간".Work3 = {AVG(0.1),TON(1)};
        }
        [finish] = {
            "5 Work 초기조건".Work1;
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
    [device file="./dsLib/AutoGen/10 Action 설정 값__System1.ds"] "10 Action 설정 값__System1"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/10 Action 설정 값__System1.ds
    [device file="./dsLib/AutoGen/11 외부 행위 (Action) 배치__System1.ds"] "11 외부 행위 (Action) 배치__System1"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/11 외부 행위 (Action) 배치__System1.ds
    [device file="./dsLib/AutoGen/13 행위 사용 안함__Action.ds"] "13 행위 사용 안함__Action"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/13 행위 사용 안함__Action.ds
    [device file="./dsLib/AutoGen/16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)__System1.ds"] "16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)__System1"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/AutoGen/16 Auto Pre 조건(자동운전시 전제조건 수동조작가능)__System1.ds
    [device file="./dsLib/Cylinder/DoubleCylinder.ds"] 
        "1 외부 주소__Device1",
        "6 심볼 정의__Device1"; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/Cylinder/DoubleCylinder.ds
}
//DS Library Date = [Library Release Date 24.3.26]
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
    let answerSafetyValid = """
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
        F.A.m = { A.ADV(_, _); }
        F.A.p = { A.RET(_, _); }
    }
    [prop] = {
        [safety] = {
            F.Main.A.p = { F.A.m; }
        }
    }
    [device file="./dsLib/Cylinder/DoubleCylinder.ds"] A; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsSimple/dsLib/Cylinder/DoubleCylinder.ds
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
