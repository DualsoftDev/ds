# import
- import keyword 구분: function 시작(`#`)과 구분: `!#import`
- 파일 확장자 `.dst` 추가
- 속성 keyword `syst` 추가
```
!#import {
	interlock as X,
	interlock as Y
	interlock as Z,
	interlock_returnTimmer as 갈고리,
	button as 잡기,
} from "./Pathlib/template.dst";
```


- 파일 내용: ./Pathlib/template.dst

```
[syst]interlock  = { adv <||> ret };  //segment 해당 call 정의 없을시에 별도의 UI에서 interface 매핑 필요
[syst]interlock_returnTimmer  = {
	adv <||> ret;
        ret = {_ ~ @s (1); };
}
[syst]button  = {@selfr(btn)};
```


##1. 예시 (기본사용)



###사용자
```
[sys]my = { A.adv >  A.ret 
  !#import {
    interlock as A,

  } from "./PathLib/template.dst";
}
```
###템플릿
```
[syst]interlock = { adv <||>  ret 
  !#import {

    trans_in as advix,
    trans_in as adviy,
    trans_in as adviz,
    trans_in as retix,
    trans_in as retiy,
    trans_in as retiz,

    trans_out as advox,
    trans_out as advoy,
    trans_out as advoz,
    trans_out as retox,
    trans_out as retoy,
    trans_out as retoz,

  } from "./PathLib/template.dst";

  ret = {retox.out, retoy.out, retoz.out ~ retix.in, retiy.in, retiz.in}
  adv = {advox.out, advoy.out, advoz.out ~ advix.in, adviy.in, adviz.in}
}

[syst]trans_in = { in; 
                   in = {_~sensor1, senso2};
}

[syst]trans_out = { out; 
                   out = {actuator~_};
}
```
###적용결과
```

/* Import 코드생성
  [sys]my = { 
      A_adv >  A_ret ;  
      A_adv <||>  A_ret ;

    A_ret = {
        A_ret_retox_out_actuator,
        A_ret_retoy_out_actuator,
        A_ret_retoz_out_actuator
        ~ 
        A_ret_retix_in_sensor1,
        A_ret_retix_in_sensor2,
        A_ret_retiy_in_sensor1,
        A_ret_retiy_in_sensor2,
        A_ret_retiz_in_sensor1,
        A_ret_retiz_in_sensor2
        };
    A_adv = {
        A_adv_advox_out_actuator,
        A_adv_advoy_out_actuator,
        A_adv_advoz_out_actuator
        ~ 
        A_adv_advix_in_sensor1,
        A_adv_advix_in_sensor2,
        A_adv_adviy_in_sensor1,
        A_adv_adviy_in_sensor2,
        A_adv_adviz_in_sensor1,
        A_adv_adviz_in_sensor2
        };

  }
*/

/* 사용자 Interface (선두주소 자동생성 UI 개발 필요)

Call Start List
        A_ret_retox_out_actuator
        A_ret_retoy_out_actuator
        A_ret_retoz_out_actuator

        A_adv_advox_out_actuator
        A_adv_advoy_out_actuator
        A_adv_advoz_out_actuator


Call End List
        A_ret_retix_in_sensor1
        A_ret_retix_in_sensor2
        A_ret_retiy_in_sensor1
        A_ret_retiy_in_sensor2
        A_ret_retiz_in_sensor1
        A_ret_retiz_in_sensor2

        A_adv_advix_in_sensor1
        A_adv_advix_in_sensor2
        A_adv_adviy_in_sensor1
        A_adv_adviy_in_sensor2
        A_adv_adviz_in_sensor1
        A_adv_adviz_in_sensor2

*/

```

##2. 예시 (템플릿상호참조)

###사용자
```
[sys]my = { A.adv >  A.ret 
  !#import {
    interlock as A,

  } from "./PathLib/template.dst";
}

```
###템플릿

```

[syst]interlock = { adv <||>  ret 
  !#import {

    trans_in as advix,
    trans_in as adviy,
    trans_in as adviz,
    trans_in as retix,
    trans_in as retiy,
    trans_in as retiz,

    trans_out as advox,
    trans_out as advoy,
    trans_out as advoz,
    trans_out as retox,
    trans_out as retoy,
    trans_out as retoz,

  } from "./PathLib/template.dst";

  ret = {retox.out, retoy.out, retoz.out ~ retix.in, retiy.in, retiz.in}
  adv = {advox.out, advoy.out, advoz.out ~ advix.in, adviy.in, adviz.in}
}

[syst]trans_in = { in < (!o.out & in); 
  !#import {
    trans_out as o,
  } from "./PathLib/template.dst";

    in = {_~sensor1, senso2};
}

[syst]trans_out = { out, (i.IN) > moved <| actuator;
  !#import {
    trans_in as i,
  } from "./PathLib/template.dst";

    out = {actuator~_};
}

```
###적용결과
```

/* Import 코드생성
  [sys]my = { 
      A_adv >  A_ret ;  
      A_adv <||>  A_ret ;

    A_ret = {
        A_ret_retox_o_actuator,
        A_ret_retoy_o_actuator,
        A_ret_retoz_o_actuator
        ~ 
        A_ret_retix_i_sensor1, 
        A_ret_retix_i_sensor2,
        A_ret_retiy_i_sensor1,
        A_ret_retiy_i_sensor2,
        A_ret_retiz_i_sensor1,
        A_ret_retiz_i_sensor2,
        !(A_ret_retox_o_actuator & A_ret_retoy_o_actuator & A_ret_retoz_o_actuator)
        };

    A_adv = {
        A_adv_advox_o_actuator,
        A_adv_advoy_o_actuator,
        A_adv_advoz_o_actuator
        ~ 
        A_adv_advix_i_sensor1,
        A_adv_advix_i_sensor2,
        A_adv_adviy_i_sensor1,
        A_adv_adviy_i_sensor2,
        A_adv_adviz_i_sensor1,
        A_adv_adviz_i_sensor2
        !(A_adv_advox_o_actuator & A_adv_advoy_o_actuator & A_adv_advoz_o_actuator)
        };

  }
*/


/* 사용자 Interface (선두주소 자동생성 UI 개발 필요)

Call Start List
        A_ret_retox_o_actuator
        A_ret_retoy_o_actuator
        A_ret_retoz_o_actuator

        A_adv_advox_o_actuator
        A_adv_advoy_o_actuator
        A_adv_advoz_o_actuator


Call End List
        A_ret_retix_i_sensor1
        A_ret_retix_i_sensor2
        A_ret_retiy_i_sensor1
        A_ret_retiy_i_sensor2
        A_ret_retiz_i_sensor1
        A_ret_retiz_i_sensor2

        A_adv_advix_i_sensor1
        A_adv_advix_i_sensor2
        A_adv_adviy_i_sensor1
        A_adv_adviy_i_sensor2
        A_adv_adviz_i_sensor1
        A_adv_adviz_i_sensor2

Call Dummy List
        A_ret_retox_o_moved
        A_ret_retoy_o_moved
        A_ret_retoz_o_moved

        A_adv_advox_o_moved
        A_adv_advoy_o_moved
        A_adv_advoz_o_moved


*/
```




