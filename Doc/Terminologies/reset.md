#### Assumption
- Start 와 Reset 은 양립할 수 없다.
- Start ~~및 reset 모두,~~ 진행 중 멈춤 및 재시작 가능해야 한다.
    - Start pause 중에 reset 허용
    - ~~Reset pause 중에 start 허용~~
- Start 에 대한 child 인과를 모델링
- Reset 의 child 안전인과는 사용자가 지정하지 않으면 default reset 인과 자동 생성
    - Reset 의 child 인과는 안전인과에 해당하는 것으로, 필요한 부분만 지정하면 나머지는 자동으로 생성
<!-- 
- child segment 를 배치할 때에 RelayE + RelaySC + RelayRC 가 생성된다.
    - 단, child segment 가 Start port 접근 가능할 때에만 RelaySC 가 생성되고,
        Reset port 접근 가능할 때에만 RelayRC 가 생성된다.
    - RelayE : 자식 본연의 End 상태.  부모가 start 시키지 않아도 다른 부모에 의해서 ON 될 수 있다.
    - RelaySC : start 에 의한 complete
        - 부모가 start 시켰고, RelayE 가 ON 되었을 때에만 ON 시킨다.
        - 부모 자체의 reset 에 의해서만 clear 된다.  (RelayE 가 off 되더라도 ON 유지)
    - RelayRC : reset 에 의한 complete
        - **Reset 완료 flag**
            - Start 가 완료 flag 를 가지는 것처럼, reset 도 완료 flag 를 따로 가진다.
            - Reset port 가 존재하는 segment 만 RelayRC 를 가진다.
        - 부모가 reset 시켰고, RelayE 가 ON 되었을 때에만 ON 시킨다.
        - 부모 자체의 start 에 의해서만 clear 된다.  (RelayE 가 off 되더라도 ON 유지)
         -->
- Start 인과와 Reset 인과는 동일 segment 에 한판에 그린다.
    - Edge 에 start 인과용인지, reset 인과용인지 marking
    - start 인과 및 reset 인과가 보는 child 의 S/R/E 는 동일
- ~~End 상태는 HT 일 때로만 한정한다.~~


#### Notation
- ht: Ready
- Ht: Going
- HT: Finish
- hT: Homing


#### Start ON 시
children 을 start 인과 순서대로 작업을 완료시켜 나가는 과정
1. 자신의 상태 검사
    - ht -> children 이 원위치 상태인지 검사.  아니면 error
        <!-- - Children 의 모든 RelayRC 를 off 시킴 -->
        - 자신의 children 의 모든 relay 가 clean 한 상태
        - 자신의 상태를 Ht 로 변경 후, Ht step 수행
    - Ht -> (Start 가 꺼졌다 다시 ON 되었을 때, 자신의 상태가 Ht 임)
        <!-- - 모든 children 의 RelayRC 를 off -->
        - Start 인과 순서대로 검사.
            - 이미 수행한 child 는 skip 하고 다음 child 인과 수행
            - Terminal children 까지 모두 수행완료 되면
                - 자신을 HT 로 변경해서 End marking 하고 Finish
    - HT -> 이미 finish 상태이므로 skip
    - hT -> (Reset 진행 중 멈춘 상태임)
        - ~~Ht 상태로 변경 후, 위의 Ht step 수행~~
        - Error.  reset 진행 중 멈춤 상태에서 재시작 불가.

#### Reset ON 시
children 을 reset 안전인과 순서를 감안하여 reset 시켜나가는 과정
1. 자신의 상태 검사
    - ht -> 이미 ready 상태이므로 skip
    - Ht -> (Start 진행 중 멈춘 상태임)
        - **G**oing pause -> **H**oming 상태 변환이므로, 모든 child 의 작업 완료 flag reset
        - hT 상태로 변경 후, 아래의 hT step 수행
    - HT -> children 이 last 상태인지 검사.  (아니면 error?)
        - **F**inish -> **H**oming 상태 변환이므로, 모든 child 의 작업 완료 flag reset
        - 자신의 상태를 hT 로 변경 후, hT step 수행
    - hT -> (Reset 이 꺼졌다 다시 ON 되었을 때, 자신의 상태가 hT 임)
        - Reset 인과 순서대로 검사.
            - 이미 수행한 child 는 skip 하고 다음 child reset 인과 수행
            - Terminal children 모두 수행완료 되면
                - 자신을 ht 로 변경하고 사용된 모든 children 의 flag reset
