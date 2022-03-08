
#### Children 원위치 찾기
Going Edges의 Start DAG 기준으로
- Child의 Reset이 뒤에서 오면 OFF
- Child의 Reset이 앞에서 오면 ON
- Child의 Reset이 방향을 모르면 None
- child 의 peer segments 들의 정보만으로 reset 을 결정할 수 없는 segment 들
- e.g child segments 중에서 "A+" 만 사용되고 "A-" 는 사용되지 않은 경우

Children 원위치 공식
- 행위는 복수개의 고유 값(원위치)를 가짐
- [OriginCalc](PPT/OriginCalc.pptx)  
