# Macro
- 매크로 : 재활용 가능한 부분들을 정의 후 재사용하기 위한 방법
- 현재 macro 지정 방법
    - 지정 가능한 부분
        - DAG call
    - 지정 / 사용 위치
        - 하나의 system 정의 부분 내에서 지정하고, 그 system 안에서만 사용한다.
    - 지정 방법
        - `[macro]` 혹은 `[macro=MACRONAME]` 의 keyword property 를 이용해서 정의한다.
        - `MACRONAME`을 지정한 경우, `MACRONAME`.name 형태로 호출하여 사용할 수 있다.



## 매크로 용법

```ex
[Sys]My = {
    [accE] = {B; S}
    [accS] = {M}
    [macro] = {
        12 = { M.U ~ S.2U }   // 정의된 이름이 System 내에서 중복되지 않아야 한다.
        23 = { M.U ~ S.3U }
        34 = { M.U ~ S.4U }
        43 = { M.D ~ S.3D }
        32 = { M.D ~ S.2D }
        21 = { M.D ~ S.1D }
    }
    //호출 Set기억 
    B.1 > Set1F <| 21
    B.2 > Set2F <| 32
          Set2F <| 12
    B.3 > Set3F <| 23
          Set3F <| 43
    B.4 > Set4F <| 34  
```

or 
```
[Sys]My = {
    [accE] = {B; S}
    [accS] = {M}
    [macro=K] = {             // 정의된 macro 명 K 가 system 내에서 중복되지 않아야 한다.
        12 = { M.U ~ S.2U }
        23 = { M.U ~ S.3U }
        34 = { M.U ~ S.4U }
        43 = { M.D ~ S.3D }
        32 = { M.D ~ S.2D }
        21 = { M.D ~ S.1D }
    }
    //호출 Set기억 
    B.1 > Set1F <| K.21
    B.2 > Set2F <| K.32
          Set2F <| K.12
    B.3 > Set3F <| K.23
          Set3F <| K.43
    B.4 > Set4F <| K.34  
```