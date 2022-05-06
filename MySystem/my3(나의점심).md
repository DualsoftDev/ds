# 나의 점심 

 ![AAA](/MySystem/img/my3.gif)
 ![AAA](/MySystem/png/my3.dio.png)

```
 [sys]나의점심  = { 
   
      [task] t = {}
      [flow] f = {
              @set (시간12시) > A조_식사 <| B조_식사;
              @set (시간12시50분),  A조_식사 > B조_식사 
              @set (시간13시40분) |> B조_식사;

              (생산시작) > 라인생산 <| #(@g (A조_식사) & @g (B조_식사));
      }
  }
```
