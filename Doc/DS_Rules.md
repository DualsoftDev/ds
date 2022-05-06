# Digital Twin Segment (DTS) language
## 4대 법칙


## 제1. 인과행위(Edge) 법칙
  - Segment는 이전 행위 결과(Finish)에 의해 시작(Start)하고 다른 행위의 움직임(Going)에 초기화(Reset) 된다.
    -  A(Finish) → Start  B
    -  B(Going) → Reset  C
<img src="IMG/Rule1.png">

## 제2. 상태관찰(Segment) 법칙
  - Segment는 부모 행위에 의해 자신의 On/Off 값을 4가지 상태(R/G/F/H)로 해석 한다.
  - 4가지 단일 상태(Ready, Going, Homimg, Finish)
     
<img src="IMG/Rule2.png">

## 3. 고유행위(Flow) 법칙
  - Segment는 시공간을 무시한 인과 논리 처리를 기본으로 하며, 내부 자식 행위들은 복수의 초기값이 정해진 고유한 흐름(Processing)을 가진다.

<img src="IMG/Rule3.png">

## 4. 리셋우선(Task) 법칙
  - Segment는 Interface 입력이 유지되어야 동작하며, 시작(Start)과 복귀(Reset)가 동시에 입력될 때 항시 Reset 명령을 우선한다. 반대로 Start를 우선 하려면 별도의 인과처리 사용한다.
<img src="IMG/Rule4.png">
