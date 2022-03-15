# new callSegment

```ex
[Sys]M = {U; D}  // Motor up / down
[Sys]B = {1; 2; 3; 4}    // Button.층 호출버튼
[Sys]S = {1D; 2D; 2U; 3D; 3U; 4U}    // Sensor Up, Down
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
        
        //층간 상하강 행위간 인터락
        
        
        M_12 |> M_21 |> M_32 |> M_43 |> M_34 |> M_23 |> M_12
        M_43 <| M_32 <| M_21 <| M_12 <| M_23 <| M_34 <| M_43
       

        //호출에 따른 층간 상하강 행위   
        M_12 < (Set2F | Set3F | Set4F) & M_21
        M_23 < (Set3F | Set4F) & (M_12 | M_32) 
        M_34 <  Set4F & (M_43 | M_23) > M_34
        M_43 < (Set1F | Set2F | Set3F) & M_34 
        M_23 < (Set1F | Set2F) & (M_32 | M_12)
        M_21 <  Set1F & (M_12 | M_32)


}
```
