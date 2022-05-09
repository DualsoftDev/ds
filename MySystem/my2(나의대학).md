# 나의 대학 

 ![AAA](/MySystem/img/my2.gif)
 ![AAA](/MySystem/png/my2.dio.png)

```
 [sys]나의대학  = { 
   
    [task] t = { 전공학점F }
    [flow] f = {
    
            입학 > 교양이수, 전공시험 > 전공이수 > 졸업;
            #(전공학점F) |> 전공이수; 
    }
  }
```
