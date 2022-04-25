# Interface (시스템간 신호처리)

  - 기본처리 : 모든 행위별 Start End Reset 정보가 없을경우 인터페이스 정의를 받는다.
  - 생성된 TAG_ID 에 매칭되는 인터페이스는 DS 외부에서 DS eventManager로 연동한다. (타 시스템별 3rd party api 개발별도)
  - 생성된 TAG_ID 는 타입을 제공한다. (기본형 Bit, 확장형 word, string, real,...)
```
[sys]my = { A >  B }; 
```
예시1
| my_TAG | TAG_ID  |예시|
| ----- | ----   | ----   | 
| A_ST (start port of A) |OUT1|PLC.M100|
| A_RT |OUT2|PLC.M101|
| A_ET |IN1|PLC.M102|
| B_ST |OUT3|PLC.M103|
| B_RT |OUT4|PLC.M104|
| B_ET |IN2|PLC.M105|

</br></br>
예시2
```
[sys]my = { A >  @selfr (B) }; 
```
| my_TAG | TAG_ID  |예시|
| ----- | ----   | ----   | 
| A_ST (start port of A) |OUT1|PLC.M100|
| A_RT |OUT2|PLC.M101|
| A_ET |IN1|PLC.M100|
| B_ST |OUT3|PC.D:\bin\file empty|
| B_RT |||
| B_ET |IN2|PC.D:\bin\file exist|

</br></br>
예시3
```
[sys]my = { A >  B ;
    B = {devOut ~ devin1, devin2};
    }; 
```
| my_TAG | TAG_ID  |
| ----- | ----   | 
| A_ST |OUT1|
| A_RT |OUT2|
| A_ET |IN1|
| B_devout_ST |OUT3|
| B_RT |OUT4|
| B_devin1_ET |IN2|
| B_devin2_ET |IN3|

</br></br>
예시4
```
[sys]my = { A.adv >  A.ret 

  !#import {
    interlock as A,

  } from "./PathLib/template.dst";
}
```
```

[syst]interlock = { adv <||>  ret 
  #selfr(adv);
  #selfr(ret);

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

| my_TAG | TAG_ID  |
| ----- | ----   | 
|   my_A_ST1(A_ret_retox_out_actuator|OUT1|
|   my_A_ST2(A_ret_retoy_out_actuator|OUT2|
|   my_A_ST3(A_ret_retoz_out_actuator|OUT3|
|   my_A_ST4(A_adv_advox_out_actuator|OUT4|
|   my_A_ST5(A_adv_advoy_out_actuator|OUT5|
|   my_A_ST6(A_adv_advoz_out_actuator|OUT6|
|   my_A_RT1||
|   my_A_RT2||
|   my_A_ET1(A_ret_retix_in_sensor1)    |IN1|
|   my_A_ET2(A_ret_retix_in_sensor2)    |IN2|
|   my_A_ET3(A_ret_retiy_in_sensor1)    |IN3|
|   my_A_ET4(A_ret_retiy_in_sensor2)    |IN4|
|   my_A_ET5(A_ret_retiz_in_sensor1)    |IN5|
|   my_A_ET6(A_ret_retiz_in_sensor2)    |IN6|
|   my_A_ET7(A_adv_advix_in_sensor1)    |IN7|
|   my_A_ET8(A_adv_advix_in_sensor2)    |IN8|
|   my_A_ET9(A_adv_adviy_in_sensor1)    |IN9|
|   my_A_ET10(A_adv_adviy_in_sensor2)   |IN10|
|   my_A_ET11(A_adv_adviz_in_sensor1)   |IN11|
|   my_A_ET12(A_adv_adviz_in_sensor2)   |IN12|



