# Edge

## 공통

- Segment 내부에 Source, Target 2개의 Segment를 Start 또는 Reset 속성으로 연결
- 자식에 배치된 Start Edges 반드시 DAG(Directed acyclic graph) 기준을 따라야함

- StartEdge : 이전 행위 결과(F)가 나를 시작하는 것을 나타내는 방식
  - $\approx$ F# 함수 호출 조건
- ResetEdge : ~~이전~~ 특정 행위 진행(G)이 나를 리셋하는 것을 나타내는 방식
  - 선행 및 후행 행위의 진행이 모두 reset 가능
- Segment 당 자식 Edge로는 Going Edges, Homing Edges 2세트 할당 (Homing Edges :기본은 원위치 동시 수행)

  - [origin.md](origin.md) : 원위치 찾기

## MEdge (modeling edge)

- Segment 와 Segment 간 연결
- Peer segment 간 연결만 허용
  - Segment 경계를 뚫고 연결할 수 없다.
  - 즉 다른 segment 의 child segment 와 바로 연결할 수 없다.
  - 자신의 child segment 와도 연결할 수 없다.

## CEdge (compiled edge)

## 기타

- Going Edge Set  : 행위 진행에 관한 자식간 내부 연결정보
- Homing Edge Set : 행위 복귀에 관한 자식간 내부 연결정보
