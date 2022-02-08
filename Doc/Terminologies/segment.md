# Segment

- S/R/E relay 를 갖는 작업의 단위 (**S**tart/**R**eset/**E**nd)
    - [relay.md](relay.md) 참고
- $\approx$ C# 멤버 함수 $\approx$ 기능
- E relay 를 살펴 봄으로써 End 인지 Not End 상태인지를 확인할 수 있다.
- 모든 Segment 는, 특정 DsSystem 내에 존재한다.
- 하나의 segment 는 부모 segment 를 가질 수도 있고, 없을 수도 있다.
    - 부모 segment 가 없는 segment 는 DsSystem 의 TopLevelSegment 이며,
    - 모든 TopLevelSegment 는 RealSegment 이다.

#### RealSegment
- $\approx$ C# 멤버 함수 정의
- 내부에 H/T relay(**H**ead, **T**ail) 를 갖는다.
- 내부에 자식 segment 들을 가질 수 있다.
    - 자식 segment 는 RealSegment 일 수도 있고, $\approx$ C# local 함수 정의 및 호출
    - RefSegment 일 수도 있다. $\approx$ C# 멤버 함수 호출
- H/T relay 의 조합으로 segment 의 상태를 R/G/H/F (**R**eady/**G**oing/**H**oming/**F**inish)로 표현한다.

| H | T | Status |
| ----- | ---- | ---- |
| 0 | 0 | Ready |
| 1 | 0 | Going |
| 1 | 1 | Finish |
| 0 | 1 | Homing |
- TopLevelSegment 는 다른 segment 에 의해서 피참조될 수 있다.

#### RefSegment (참조/Referencing segment)
- $\approx$ C# 멤버 함수 호출
- 자식 segment 를 가질 수 <ins>없다.</ins>
- 내부에 H/T relay 가 없다.
- 실제 참조하고 있는 대상 segment (피참조/Referenced segment$\approx$ C# 호출된 함수)가 존재해야 하며,
    - 이는 피참조 segment 가 속한 system 의 TopLevelSegment 이어야 한다.
    - 참조 segment 와 피참조 segment 가 속한 system 은 다를 수도 있다.
        - C#에서 특정 멤버 함수를 호출하는 멤버 함수가 정의된 class 와, 호출되는 멤버함수가 정의된 class 는 서로 다를 수 있다.
- 다른 segment 에 의해서 피참조될 수 없다.
