# DS text language spec

Edge Start 기호  '>', '<'

- Ex) A > B  의미 : A 완료(Finish) 후에 B Start

Edge Reset 기호 '|>', '<|', '<|>'

- Ex) A |> B  의미 : A실행(Going)시 B Reset
- Ex) A <|> B 의미 : A실행(Going)시 B Reset, B실행(Going)시 C Reset

Real Segment 정의 방법 : indent (\t) 이후 이름 = {edge1;edge2;...;edgeN} 형식으로 정의

- Ex) RealSeg1 = {Seg1 > Seg2; Seg1 > Seg3; Seg1 <!> Seg2 }
- Real Segment 는 CallSegment를 Child로 등록 가능하다. (CallSegement는 주로 라이브러리 형태로 미리제공예정)
- 예약어 [arrH] 입력 받으시 Homing 인과로 추가해석 (Start Edge 만 가능)

Call Segment 정의 방법 : indent (\t) 이후 이름 = { System.SegA, System.SegB>>System.SegC } 형식으로 정의

- Ex) CallSeg1 = {Sys.A,Sys.C >> Sys.B}  (A,C 동시 실행후 B 완료관찰)

```ex)

[Sys]sys1 = {seg0}
    [accS] {Valve}
    [accE] {Sensor}
    seg0 = {seg1 > seg2;seg1 <!> seg2}
      [arrH] {seg2 > seg1}
    seg1 = { Valve.V+, Valve.Open >> Sensor.S+ }
    seg2 = { Valve.V-, Valve.Open >> Sensor.S- }
        

[Sys]Valve  = {V+ <!> V-;Open}
[Sys]Sensor = {S+;S-}
[Sys]Cylinder =  { Valve.V+ >P+ > Sensor.S+
                   Valve.V- >P- > Sensor.S-
                   P+ <!> Sensor.S-
                   P- <!> Sensor.S+
                 }
    [accS] {Sensor}
    [accE] {Valve}
    
```

- 구성 요소
  - DsSystem : Root Segment Edges
    - indent 없이 [Sys]SystemName = {children segments edge List} (edge없을시 ';' 로 구분하여 행위만 나열)
      - brace '{}' 내부의 child 구분자는 `;` or line break
      - child segment 하부의 segment 는 위 목록에 기술하지 않음.  해당 child segment 정의에서 기술
    - indent 후 하부에 child segment 에 대한 세부 정의 및 속성 정의
    - indent 는 TAB만 지원 스페이스 안됨
  - (Child) Segment Edges (Real Segment는 Root에만 존재 가능)
    - SegmentName = {children segments edge List} (edge없을시 ';' 로 구분하여 행위만 나열)
    - CallSegment : <타시스템 이름>.<타시스템 대상 root segment>
      - e.g : `Cylinder.Adv` : Cylinder 시스템의 전진 segment
      - Child 가 DAG 인 경우의 정의 방법  ( '>>' 기호로 참고 )
        - dag1 =   { Valve.V+, Valve.Open >> Sensor.S+ }
  - Properties
    - [bracket 내부에 속성 명 정의] = {속성 세부 사항}
    - 현재 정의된 속성명
      - accXYZ : XYZ 는 S/R/E 의 조합.  e.g accS, accSRE, accRE, ... 등 총 7가지
      - arrG : Going 인과 : 행위 Start Edge / 행위 Reset Edge 혼용  [arrG] 생략가능
      - arrH : Homing 인과(안전복귀인과)  : 행위 Start Edge 전용
    - 추가 확장 필요한 속성명
      - ?? exportXYZ : XYZ 는 S/R/E 의 조합.   타 시스템이 해당 segment 의 S/R/E 를 볼 수 있는지 여부
    - 인과 정의
      - `>` or `<` 인과 순서
      - `|>` or `<|` or `<|>` reset 인과
      - `,` 는 and(&) 관계.  인과 방향성보다 우선순위가 높음
        - e.g `Sys1.A, Sys2.A > X`
  - 주석.  `//` 로 시작하는 line comment or `/*` 와 `*/` 의 block comment

- [elevator.md](../Samples/elevator.md) 참고 (수정중)
