# 나의 점심 

 ![AAA](/MySystem/img/my3.gif)
 ![AAA](/MySystem/png/my3.dio.png)

```
 [Sys]나의점심  = { @set 시간 12시 > A조 식사 <| B조 식사;
                    @set 시간 12시 50분,  A조 식사 > B조 식사 
                    @set 시간 13시 40분 |> B조 식사;

                    (생산시작) > 라인생산 <| @g A조 식사 & @g A조 식사;
  }
```
