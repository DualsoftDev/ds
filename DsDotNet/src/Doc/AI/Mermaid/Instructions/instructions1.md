
cylinder 1개를 전진 후, 후진 시키려면 다음과 같이한다.
```
graph LR
AA((DevA.ADV)) --> AR((DevA.RET))
```

cylinder 2개 (A, B) 에 대해서, A전진, B전진, A후진, B후진을 순서대로 하려면 다음과 같이한다.
```
flowchart LR
AA((DevA.ADV)) --> BA((DevB.ADV)) --> AR((DevA.RET)) --> BR((DevB.RET)) 
```

cylinder 2개 (A, B) 에 대해서, A전진, B전진 동시에 수행하고, A후진, B후진을 동시에 수행 하려면 다음과 같이한다.
```
graph LR;
subgraph adv
AA((DevA.ADV))
BA((DevB.ADV))
end

subgraph ret
AR((DevA.RET))
BR((DevB.RET)) 
end

adv -->|R| ret
```


혹은 다음과 같이 나타낼 수도 있다.
```
graph LR;
subgraph all
AA((DevA.ADV)) --- BA((DevB.ADV)) --> AR((DevA.RET)) --> BR((DevB.RET))
end
```


cylinder 3개(A, B, C)를 동시에 전진한 후,  A 후진 후, B, C 동시 후진
```
graph LR;
subgraph all
AA((CylinderA.ADV)) --- BA((CylinderB.ADV)) --- CA((CylinderC.ADV))
--> AR((CylinderA.RET))
--> BR((CylinderB.RET)) --- CR((CylinderC.RET))
end
```



