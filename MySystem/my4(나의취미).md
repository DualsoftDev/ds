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

    #call (Xm, Xp);
    #call (Ym, Yp);
    #call (Zm, Zp);
    #call (풀기, 잡기);
    #call (버튼);

    풀기 <||> 잡기;
    잡기버튼 > 인형뽑기처리 <| _runr;
    인형뽑기처리 = {풀기, Zp > 잡기 > Zm > Xm, Ym, 풀기 > 잡기};

  }
```
```
  //call macro from [sys]뽑기(수정 시 책임필요)
  [sys]_뽑기_dev  = { 
                Xm;Xp;
                Ym;Yp;
                Zm;Zp;
                풀기;잡기;

    Xm = {O.Xm ~ I.Xm};
    Xp = {O.Xp ~ I.Xp};
    
    Ym = {O.Ym ~ I.Ym};
    Yp = {O.Yp ~ I.Yp};
    
    Zm = {O.Zm ~ I.Zm};
    Zp = {O.Zp ~ I.Zp};
    
    잡기 = {O.잡기 ~ @s (1)};
    풀기 = {O.풀기 ~ I.풀기};

    버튼 = {O.버튼 ~ _};
  }
  //call macro from [sys]뽑기
```
