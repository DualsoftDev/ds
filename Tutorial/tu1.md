:smile: Welcome to the DS world  :smile:

## Elevator system :jeans:


 ![AAA](./png/tu1.dio.png)
 
  - action list 
    1. up_down
    2. btn_reset
    3. inter_lock
    4. M


```ex
[sys] elevator = {
    [flow] M = {U;D;}         //1모터 2방향 연결
    [task] B = {F1;F2;F3;}    //각 층 정보 저장 및 활성화
    [task] T = {  //모터, 센서정보 할당 필요 
        U12 = {M.U ~ S.S2U};
        U23 = {M.U ~ S.S3U};
        D32 = {M.D ~ S.S2D};
        D21 = {M.D ~ S.S1D};
    }

    [flow] btn_reset = {
        B.F1 <| (T.D21);
        B.F2 <| (T.U12) ? (T.D32);
        B.F3 <| (T.U23);
    }

    [flow] inter_lock = {
        T.U12 <||> T.D21 <| T.D32 <||> T.U23 <| T.U12;    
    }

    [flow] up_down = {
        T.U12 < B.F2 , T.D21;
        T.U23 < B.F3 , T.D32 ? B.F3 , T.U12;
        T.D32 < B.F2 , T.U23;
        T.D21 < B.F1 , T.D32 ? B.F1 , T.U12;
    }
}




```
