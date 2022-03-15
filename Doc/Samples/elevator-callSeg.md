# new callSegment

```ex
[Sys]M = {U; D}  // Motor up / down
[Sys]B = {1; 2; 3; 4}    // Button.층 호출버튼
[Sys]S = {1D; 2D; 2U; 3D; 3U; 4U}    // Sensor Up, Down
[Sys]My = {
        [accE] = {B; S}
        [accS] = {M}

        //호출 Set기억 
        B.1 > Set1F <| 21
        B.2 > Set2F <| 23; Set2F <| 12
        B.3 > Set3F <| 23; Set3F <| 43
        B.4 > Set4F <| 34  

        //호출에 따른 층간 상하강 행위
        Set1F > 21
        Set1F > 32
        Set1F > 43
        Set2F > 12
        Set2F > 32
        Set2F > 43
        Set3F > 43
        Set3F > 23
        Set3F > 12
        Set4F > 12
        Set4F > 23
        Set4F > 34

        //현 위치에 따른 층간 상하강 행위
        21 > 12 
        (12 || 32) > 23
        (23 || 43) > 34
        34 > 43
        (23 || 43)> 32
        (12 || 32)> 21

        //층간 상하강 행위간 인터락
        12 <|> 23 <|> 34 <|> 43 <|> 32 <|> 21

    }
    12 = { M.U >> S.2U }
    23 = { M.U >> S.3U }
    34 = { M.U >> S.4U }
    43 = { M.D >> S.3D }
    32 = { M.D >> S.2D }
    21 = { M.D >> S.1D }
```
