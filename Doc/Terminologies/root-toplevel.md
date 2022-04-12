Fill me!!!
<!-- 
root segment 내의 dag 를 구성하는 segment 는 모두 call segment 로 해석한다.
그래야 그나마 RGFH 에 대한 해석이 일부 가능해 진다.


system root 가 다음과 같은 경우,
``` mermaid
flowchart LR
    a - -> b
```

- b 가 call segment 가 아니라면
    - b 에 대해서 다음과 같은 조건이면 제어가 불가능하다.
        - b 의 RGFH 상태를 외부(a)에서 알 수 없다
        - b 의 R->G 상태변화를 외부에서 알 수 없다
    - a.E 감지하더라도,
        - b 가 Ready 상태임을 확인해야만 b 를 start 시킬 수 있다.
        - b 가 Ready 상태가 아니면, b 가 Ready 상태가 될 때까지 기다린 후, b 를 시작시켜야 한다.
        - b 가 Ready->Going 상태로 전환 될 때에, a 를 reset 시켜야 한다.
- b 가 call segment 이고, b 의 refrenced segment 가 다른 곳에서 사용되지 않는다면,
    - call segment 를 this system 에서 관리하므로, RGFH 상태를 가늠할 수 있다.
    - a 를 적절한 시점에, 후행 reset 시킬 수 있다.
        - a 의 모든 outgoing 이 R->G 상태변화 있으면 reset -->

- 기본은 후행 reset
- sink segment 는 외부 reset
    - 외부 reset 이 없으면, 재실행 불가.  채워져서 시작할 수 없음.


