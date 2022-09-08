모델 > 시스템s > Segments >> child segments..


하나의 모델에는 
    1. active system 이 복수개 존재할 수 있다.  하나 이상은 반드시 존재해야 한다.
    1. 복수의 passive system 이 존재할 수 있다.
    1. 유일한 하나의 비인과외부 시스템이 존재해야 한다.


하나의 시스템에는
    1. 복수개의 Toplevel segment 들이 존재한다. (n >= 1)
    1. Toplevel segment 는 계층적으로 다른 segment 를 포함할 수 있다.
        - 이때 포함되는 child 들은 다른 시스템의 toplevel segment 가 될 수 있다.
    1. System A 에 포함된 toplevel segment S1, S2 에 대해서, S1 은 S2 를 child 로 가질 수 있다(~~~없다(?)~~~)
        - 가질 수 있다면, S1 의 child 를 외부에서 관찰 가능하다는 모순 (but, S2 가 toplevel 이므로 관찰가능)
        - System A 가 매 10도씩 움직이는 36개의 toplevel 을 갖고 있다면, 이들 toplevel 을 조합해서 90 도 회전하는 toplevel 을 만들 수 있다(~~~없다.(?)~~~)

시스템 종류
    1. active system : 모델에서 제어를 대상으로 하는 system
    1. passive system : active system 에서 제어하려는 device system
    1. 비인과외부 시스템

하나의 segment 는
    1. Relays
        1. 내부에 Head, Tail relay
        1. 부착된 Start, Reset, End relay

Segment 의 parent / child 관계는 modeling 된 관계를 따른다.


부모는 자식들을 살펴볼 수 있으나 손자는 살펴볼 수 없다

형제들끼리 살펴볼 수 없다

Segment 의 GOING 상태는 외부에서 관찰할 수 없다.
    - 부모가 시켰으면 child 는 going 상태이다.  부모만 Going 상태를 알 수 있다.


