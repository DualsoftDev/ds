
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

