# Monitor

## 용법

- Segment가 모니터링될 값을 외부로 알림

- 모니터(Monitor)

    | State  | Description |
    | ----  | ---- |
    | Undefined   |초기 접속 시 상태|
    | Idle      |상태변화 이벤트 'R' or 'F' 유지 상태 모니터  (Not Monitor.Stop)|
    | Work       |상태변화 이벤트 'G' or 'H' 유지 상태 모니터 (Not Monitor.Stop)|
    | Stop of StopType |'R/G/F/H' 상태에서 STOP 조건 발동 시 상태 모니터|

- 정지 타입(StopType)

    | StopType  | Description |
    | ----  | ---- |
    | Pause   |‘G’ 상태에서 Start  CMD off 변경 시 Stop 발동|
    | Maintenance      |‘H’ 상태에서 Reset CMD off 변경 시 Stop 발동|
    | Timeout       |Segment 의 시간(평균 Going 시간 x배) 지연 시 Stop 발동|
    | Condition |모든    Segments 의 정의 인과위반시 Stop 발동|

