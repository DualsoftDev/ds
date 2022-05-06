# 나의 취미 

 ![AAA](/MySystem/img/my4.gif)
 ![AAA](/MySystem/png/my4.dio.png)

```ds
[sys]취미생활 = {

    [task]나의_취미  = {
        동전넣기 = {뽑기.동전인식 ~ _ }
        버튼     = {장치.버튼누름 ~ _ }
        위치확인;

        좌 = {뽑기.Xm ~ _ };
        우 = {뽑기.Xp ~ _ };
        상 = {뽑기.Yp ~ _ };
        하 = {뽑기.Ym ~ _ };
    }

    [flow of 나의_취미]X ={
        동전넣기 > 위치확인 > 버튼;
        (X.덜감확인) > @pushs (좌);
        (X.더감확인) > @pushs (우);
        (Y.덜감확인) > @pushs (상);
        (Y.더감확인) > @pushs (하);
        (동물.고양이.야옹) > WOW;
    }

    
    [task]뽑기  = {
        !#import {axis as X} from "./interlock.ds";
        !#import {axis as Y} from "./interlock.ds";
        !#import {axis as Z} from "./interlock.ds";
        !#import {interlock_returnTimer as 갈고리} from "./interlock.ds";
        !#import {button as 잡기} from "./interlock.ds";

        인형뽑기처리 = {갈고리.ret, Z.adv > 갈고리.adv > Z.ret;
                        Z.ret > X.ret, Y.ret; };
    }

    [flow]뽑기 = {
        잡기.btn > 뽑기.인형뽑기처리 <| _runr;
    }
}

[sys]동물 = {

    [task]고양이  = {
        움직임감지;
        야옹 = {성대.울림 ~ 소리.야옹};
    }
    

    [flow]고양이 = {
        (고양이.움직임감지) > 고양이.야옹;
    }
}

}
```

./interlock.ds

```ds
[task]axis  = { adv; ret; 더감확인; 덜감확인;}; //segment 해당 call 정의 없을시에 별도의 UI에서 interface 매핑 필요
[flow]axis  = { axis.adv <||> axis.ret };

[task]interlock_returnTimer  = {
    adv; ret;
    ret = {_ ~ @s (1);}
}
[flow of interlock_returnTimer]interlock_returnTimer  = 
          { adv <||> ret;};

[task]button  = {btn;};
[flow]button  = {@selfr(btn)};
```
