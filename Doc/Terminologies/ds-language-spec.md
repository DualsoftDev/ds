# DS text language spec

```ex)
[Sys]sys1 = 
    [accSE] {A;B}
    [accS] {Valve}
    [accE] {Sensor}
    [arrG] {seg1 > seg2 > seg3 > seg4}
    [arrR] {seg1 <!> seg2; seg3 <!> seg4}
    seg1 =
        [arrG] {A.+ > B.+ > B.-}
    seg2 = 
        [arrG] {A.+ > B.+ > B.-}
    seg3 = 
        [call] { Valve.V+, Valve.Open |> Sensor.S+ }
    seg4 = 
        [call] { Valve.V-, Valve.Open |> Sensor.S- }
        
[Sys]A = [arrR] {+ <!> -}
[Sys]B = [arrR] {+ <!> -}
[Sys]Valve  = [arrR] {V+ <!> V-;Open}
[Sys]Sensor = [arrR] {S+;S-}
[Sys]Cylinder =
    [accS] {Sensor}
    [accE] {Valve}
    [arrG] {Valve.V+ >P+ > Sensor.S+; Valve.V- >P- > Sensor.S-}
    [arrR] {P+ <!> Sensor.S-; P- <!> Sensor.S+} 
    
```

- 구성 요소
  - DsSystem
    - indent 없이 [Sys]SystemName = {children segments edge List} (edge없을시 ';' 로 구분하여 행위만 나열)
      - brace '{}' 내부의 child 구분자는 `;` or line break
      - child segment 하부의 segment 는 위 목록에 기술하지 않음.  해당 child segment 정의에서 기술
    - indent 후 하부에 child segment 에 대한 세부 정의 및 속성 정의
    - indent 는 TAB만 지원 스페이스 안됨
  - (Child) Segment
    - SegmentName = {children segments edge List} (edge없을시 ';' 로 구분하여 행위만 나열)
    - CallSegment : <타시스템 이름>.<타시스템 대상 segment>
      - e.g : `Cylinder.Adv` : Cylinder 시스템의 전진 segment
      - target 이 DAG 인 경우의 정의 방법
        - dag1 =  [call] { Valve.V+, Valve.Open |> Sensor.S+ }
  - Properties
    - [bracket 내부에 속성 명 정의] = {속성 세부 사항}
    - 현재 정의된 속성명
      - accXYZ : XYZ 는 S/R/E 의 조합.  e.g accS, accSRE, accRE, ... 등 총 7가지
      - arrG : Going 인과 : 행위 Start Edge 전용
      - arrH : Homing 인과(안전복귀인과)  : 행위 Start Edge 전용
      - arrR : Ready(Reset) 인과 : 행위 Reset Edge 전용
      - call : 타시스템 DAG로 Segment 구성
    - 추가 확장 필요한 속성명
      - ?? exportXYZ : XYZ 는 S/R/E 의 조합.   타 시스템이 해당 segment 의 S/R/E 를 볼 수 있는지 여부
    - 인과 정의
      - `>` or `<` 인과 순서
      - `!>` or `<!` or `<!>` reset 인과
      - `,` 는 and(&) 관계.  인과 방향성보다 우선순위가 높음
        - e.g `Sys1.A, Sys2.A > X`
  - 주석.  `//` 로 시작하는 line comment or `/*` 와 `*/` 의 block comment

- [elevator.md](../Samples/elevator.md) 참고
