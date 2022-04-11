# 1. Segment (행위단위)

<!-- 확인:  `외부` : 부모를 포함한 상부를 의미.  타 시스템을 의미하는 것이 아님 -->

- 외부 작용(Start/Reset)을 통해 정해진 고유 행위(작업)를 수행하며 자기 유사성을 지니는 기본 단위
- 외부 인터페이스 Start/Reset/End Port(flag) 를 갖는 행위(작업)의 단위 (**S**P/**R**P/**E**P)
  - [flag.md](flag.md) 참고
  - SP/RP/EP 는 반드시 존재해야 행위로 성립한다.(예외경우 Interface Macro제공 [table4.3](/Language/ds-language-table.md)))
  - SP/RP/EP 는 존재하더라도 특정 시스템에서 S/R/E port는 접근이 안될 수도 있다. [System access](/Terminologies/DsSystem.md)

- $\approx$ F# 순수 함수 기능 (차이점 시작전 초기값에 따라 다름)
- 외부에서 EP Flag 를 살펴 봄으로써 On/Off 인지 확인할 수 있다.
- 모든 Segment 는, 특정 DsSystem 내에 유일하게 소속된다.
- 하나의 Segment 는 유일한 부모 Segment 를 가진다.
  - 예외) TopLevel에 존재하는 RootSegment를 동시 호출하는 DsSystem의 유일한 SystemSegment는 제외
  
- Status None  에서 상태평가를 통해 정상 상태 해석
  | Start | Reset  | Out Value | Segment Status |
  | ----- | ----   | --- | --- |
  | 0     | 0      | OFF | Ready |
  | 1     | 0      | OFF | Going |
  | -     | 0      | ON | Finish|
  | -     | 1      | ON | Homing |

## RealSegment

- $\approx$ F# 멤버 함수 정의
- 내부에 상태변수를 가지며, 값은 Homing(H), Ready(R), Going(G), Finish(F) 4가지 중 하나의 상태로 존재
- 외부 명령(Cmd) 수신 시의 예상 상태 변화
  - Command Start On 시 R → G → F
  - Command Reset On 시 F → H → R
- Start/Reset 동시발생시 Reset 우선 (참고 : [DS Rule4](../DS_Rules.md))
- 외부 표출 값(Value) : (외부에서 나의 segment 상태를 해석하는 방법)
  - Value On은 (F, H) 상태
  - Value Off은 (R, G) 상태
  - 초기 접속시 행위 상태는 값과 상관없이 평가불가 (Status None)


-[Status타임차트](./ppt/Status.pptx)

- 내부에 자식 Segment 들을 가질 수 있다.
  - 자식 Segment 는 RealSegment 이거나, 호출가능한 CallSegment 이다.

## CallSegment (호출행위 타 시스템 DAG 실행)

- $\approx$ C#/F# 함수 호출
- 자식 Segment 를 가질 수 없다.
- Target을 가진다.
  - Target = 호출 대상. (Segment or DAG)
  - 타 시스템의 toplevel RealSegment 혹은 toplevel 의 DAG.
  - Target 이 CallSegment 일 수는 없다.

  - Target은 해당 시스템 toplevel에 DAG(directed acyclic graph 유향비순환) 형태로 존재해야 하며,
    - 호출한 Segment 와 호출된 Segment 가 속한 system 은 반드시 다르다.
    - 호출시작은 호출된 DAG의 Head Node(Segment)들의 Start Port에 접근 가능한 Start TAG를 사용
    - 호출결과는 호출된 DAG의 Tail Node(Segment)들의 End Port에 접근 가능한 End Tag를 사용

- Call 시퀀스(상태 추정값*)
  | Start | Reset  | Out Value | Segment Status |
  | ----- | ----   | --- | --- |
  | 0     | -      | OFF | Ready |
  | 1     | 0      | OFF | Going |
  | -     | 0      | ON | Finish|
  | 0     | 1      | ON | Homing |
  | 1     | 1      | - | Error(todo 우선순위 속성필요)|

## FunctionSegment
- System 함수 계산을 내부에 포함하고 있는 segment
  - Return 값이 존재하는 함수 : (e.g. Sin, Cos, Abs, ...)
  - Return 값이 없는 함수 : Delay
  


