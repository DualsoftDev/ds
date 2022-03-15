# Macro

## 매크로 용법

- 이사님 도와주세요 ~
```ex
[Sys]My = {
        [accE] = {B; S}
        [accS] = {M}
        [macro] = {
            12 = M.U ~ S.2U
            12 = M.U ~ S.2U
            23 = M.U ~ S.3U
            34 = M.U ~ S.4U
            43 = M.D ~ S.3D
            32 = M.D ~ S.2D
            21 = M.D ~ S.1D
              }

        //호출 Set기억 
        B.1 > Set1F <| M_21
        B.2 > Set2F <| M_32
              Set2F <| M_12
        B.3 > Set3F <| M_23
              Set3F <| M_43
        B.4 > Set4F <| M_34  
        ```