#### Assumption

- Start 와 Reset 은 양립할 수 없다.
- Start/Reset 진행 중 멈춤 및 재시작 가능해야 한다.
  - Start pause 중에 reset 허용
  - Reset pause 중에 ready시에 start 허용
- Start 에 대한 child 인과를 모델링 (Going인과Edges)
- Reset 의 child 안전인과는 사용자가 지정하지 않으면 default reset 인과 자동 생성 (Homing인과Edges)
  - Reset 의 child 인과는 안전인과에 해당하는 것으로, 필요한 부분만 지정하면 나머지는 자동으로 생성

- child segment 를 배치 할때 기준이 Going 인과이며 이 child들로 Homing 인과를 추가로 구성한다.
  - 정의 없을시에 원위치로 동시에 호출
- child segment 를 배치할 때에 RelayS + RelayR + RelayE 가 생성된다. (SR/RR/ER)
- child segment 자신의 고유 인터페이스 PortS/ PortR/ PortE (SP/RP/EP)가 존재한다.
  - PortE : 자식 본연의 ON/OFF 상태.  부모가 start 시키지 않아도 다른 부모에 의해서 ON 될 수 있다.
  - RelayE : start 에 의한 complete
    - 부모가 start 시켰고, PortE 가 ON 되었을 때에만 RelayE를 마킹한다.
    - 부모 자체의 reset 에 의해서만 clear 된다.  (RelayE 가 off 되더라도 Port상태는 마지막 상태 유지)
    - RelayRC : reset 에 의한 complete
      - **Reset 완료 flag**
        - Start 가 완료 flag 를 가지는 것처럼, reset 도 완료 flag 를 따로 가진다.
        - 부모가 Homing상태에서 Homing인과 순서를 지키면서 원위치 𝜑값에 해당하면 EH를 ON 시킨다.
        - 부모가 Ready 상태되면 사용릴레이 전부 클리어 (SH,RH,EH)

- Going 인과와 Homing 인과는 동일 segment 에 한판에 그린다.
  - Edge 에 Going 인과용인지, Homing 인과용인지 marking
  - Going 인과 vs Homing 인과
    - child의 Relay는 별개 [flag.md](flag.md)
    - child의 SP/RP/EP는 동일

#### Start ON 시

children 을 start 인과 순서대로 작업을 완료시켜 나가는 과정

1. 자신의 상태 검사
    - Ready -> children 이 원위치 상태인지 검사.  아니면 (StopType.Violation)
        <!-- - Children 의 모든 RelayRC 를 off 시킴 -->
        - 자신의 children 의 모든 relay 가 clean 한 상태
        - 자신의 상태를 Going 로 변경 후, Going step 수행
    - Going -> (Start 가 꺼졌다 다시 ON 되었을 때, 자신의 상태가 이미 Going 임)
        <!-- - 모든 children 의 RelayRC 를 off -->
        - Start 인과 순서대로 검사.
            - 이미 수행한 child 는 skip 하고 다음 child 인과 수행
            - Terminal children 까지 모두 수행완료 되면
                - 자신을 Finish 로 변경
    - Finish -> 이미 finish 상태이므로 skip
    - Homing -> (StopType.Violation)  reset 진행 중 멈춤 상태에서 재시작 불가. (Ready상태에서 처음 시작가능)

#### Reset ON 시

children 을 reset 안전인과 순서를 감안하여 reset 시켜나가는 과정

1. 자신의 상태 검사
    - Ready -> 이미 ready 상태이므로 skip
    - Going -> 자신의 EndPort OFF 로 유지한 채, Homing 수행 (Ready시에 필요시 Change Status event)
    - Finish -> 자신의 EndPort ON 로 유지한 채, Homing 수행 (Ready시에 EndPort Off)
    - Homing -> (Reset 이 꺼졌다 다시 ON 되었을 때, 자신의 상태가 Homing 임)
        - 모든 children 의 초기 상태 검사 : [origin.md](origin.md) : 원위치 찾기
        - 원위치 ON 인 child segment 에 대해서 ON 시킴
        - 원위치 OFF 인 child segment 에 대해서 OFF 시킴
        - 원위치 Unknown 인 child 는 skip
        - Terminal children 모두 수행완료 되면
            - 사용된 모든 children 의 flag off 자신을 Ready 로 변경

#### Counter examples

- default reset 인과 적용 기준
- Example1
    seg reset 시 B+ -> B- 가 수행되어서는 안되나, 수행 되므로
    강제 ON/OFF 기능필요

```
[Sys]sys1 = 
    [arrG] {seg1 > seg2}
    [arrR] {seg1 <!> seg2}
    seg1 = [arrG] {A.+ > B.+ > B.-}
    seg2 = [arrG] {A.+ > B.+ > B.-}
        
  
[Sys]A = [arrR] {+ <!> -}
[Sys]B = [arrR] {+ <!> -}

```

