```Model.AddEdges(edgeInfos:MEdge seq, parentSeg:Segment) =```
- parentSeg 를 지정할 경우, 이를 사용 (edge 가 parentSeg 에 포함된 경우)
- parentSeg 가 null 일 경우, 특정 system segment 로 할당.
    - 기본은 model 의 active system 의 system segment 로 할당    
    - 예외 :
        비인과외부에서 들어오는 edge : 비인과 system 의 system segment
        exSystem reset 인 경우 : exSystem 의 segment.  (e.g cylinder)

- source system 이 parentSeg 의 system 과 다르면 parentSeg 에 source 를 link 로 추가
- target system 이 parentSeg 의 system 과 다르면 parentSeg 에 target 를 link 로 추가


