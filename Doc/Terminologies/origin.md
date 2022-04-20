
#### Children 원위치 찾기

자신의 Start Point(원위치)는 모든 children Ready (Off) 를 기준으로 한다.
 - On/Off 무시하는 예외 경우 Going Edges의 Start DAG 기준으로
    - 예외 규칙 1) Child의 Reset이 앞에서 오면 해당 Child는 상태무시
    - 예외 규칙 2) Child의 Reset이 정보 없으면 해당 Child는 상태무시


Children 원위치 공식
- 행위는 복수개의 고유 값(원위치)를 가짐
- [OriginCalc](PPT/OriginCalc.pptx)  


#### 초기 위치 판정 Example
###### Example1
```mermaid
flowchart LR
    subgraph seg전후진 
    direction LR
    A+ --> A-
    A+ o.-o A-
    end
```

- `seg전후진` 의 children 의 시작 가능 상태는
    - ~~A+ 의 reset 이 진행 순서상 뒤(A-) 에서 오기 때문에 OFF 이어야 하고,~~
    - ~~A- 의 reset 은 진행 순서상 앞(A+) 에서 오기 때문에 ON 이어야 한다.~~
    - A+, A-의 상태는 Ready
    - 단, A-는 예외 규칙 1에 의해 A- 의 reset 은 진행 순서상 앞(A+) 에서 오기 때문에 ON/OFF 둘중에 아무거나 가능
    - 판정시 reset 관계 해석은 children 내에서 존재하는 것으로만 한정한다.


###### Example2
```mermaid
flowchart LR
    subgraph seg1
    direction LR
    A1+[A+] --> A1-[A-]
    A1+ o.-o A1- --> C+
    end

    subgraph seg2
    direction LR
    C- --> A2+[A+] --> A2-[A-]
    A2+ o.-o A2-
    end

    C+ o..-o C-
    seg1 --> seg2
```
- `seg1` 및 `seg2` 모두에서 A+ 및 A- 는 각 segment 내에서 reset 이 이루어지지만, `seg1.C+` 이나 `seg2.C-` 의 reset 관계는 해당 segment 의 외부에서 이루어지므로 ~~이들은 초기 위치 판정에 관여하지 않는다.~~ 예외 규칙 2로 Reset 정보없음에 의해 상태 무시
    - Don't care condition