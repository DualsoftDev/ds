# 나의 취미 

 ![AAA](/MySystem/img/my4.gif)
 ![AAA](/MySystem/png/my4.dio.png)

```
 [sys]나의_취미  = {
    동전넣기 > 위치확인 > 버튼;

      동전넣기 = {뽑기.동전인식 ~ _ }
      버튼     = {장치.버튼누름 ~ _ }

    (X축_덜감확인) > @pushs (좌);
    (X축_더감확인) > @pushs (우);
    (Y축_덜감확인) > @pushs (상);
    (Y축_더감확인) > @pushs (하);
    (고양이.야옹) > WOW;

      좌 = {뽑기.Xm ~ _ };
      우 = {뽑기.Xp ~ _ };
      상 = {뽑기.Yp ~ _ };
      하 = {뽑기.Ym ~ _ };

  }

  [sys]고양이  = {

    (움직임감지) > 야옹;
    야옹 = {성대.울림 ~ 소리.야옹};

  }

  [sys]뽑기  = {

    #import [X](../PathLib/interlock.ds);
    #import [Y](../PathLib/interlock.ds);
    #import [Z](../PathLib/interlock.ds);
    #import [갈고리](../PathLib/interlock_returnTimmer.ds);
    #import [잡기](../PathLib/button.ds);

    잡기.btn > 인형뽑기처리 <| _runr;
    인형뽑기처리 = {갈고리.ret, Z.adv > 갈고리.adv > Z.ret;
                   Z.ret > X.ret, Y.ret; };

  }
```
../PathLib
```
  [sys]interlock  = { adv <||> ret };  //segment 해당 call 정의 없을시에 별도의 UI에서 interface 매핑 필요
  [sys]interlock_returnTimmer  = 
            { adv <||> ret;
              ret = {_ ~ @s (1);
            };
  [sys]button  = {@selfr(btn)};
```
