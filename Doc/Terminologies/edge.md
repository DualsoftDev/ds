# Edge

#### 공통
- StartEdge : 인과 순서를 정의하는 edge
    - $\approx$ C# 함수 호출 순서
- ResetEdge : [Reset](reset.md) 을 정의하는 edge
#### MEdge (modeling edge)
- Segment 와 Segment 간 연결
- Peer segment 간 연결만 허용
    - Segment 경계를 뚫고 연결할 수 없다.
    - 즉 다른 segment 의 child segment 와 바로 연결할 수 없다.
    - 자신의 child segment 와도 연결할 수 없다.
#### CEdge (compiled edge)


#### 기타
- SafetyEdge : 안전 인과
