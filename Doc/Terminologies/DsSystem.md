# DsSystem

- system 이라는 일반적인 용어는 문맥에 따라 DsSystem 을 뜻할 때도 있다.
- DsSystem 이라고 명시한 때에는 일반적인 system 이 아니라, DS 에서의 system 을 의미한다.

## 용법

- 정의 : 별도의 I/F없이도 Segment간 인과 처리가능한 행위 들의 집합
- 기준 : Segment 는 유일한 System에 배치
- 부모의 자식 성립기준 : 부모행위 소속시스템이 자식 소속시스템간 I/F Access StartPort, ResetPort, EndPort 접근 유효
- Access 타입
  | Parent.IsAccStart(Child) | Parent.IsAccReset(Child)| Parent.IsAccEnd(Child)| Description |
  | ----- | ----  | ---- |---- |
  |false| false| false | AccNot  //접근 불가              |
  |true | false| false | AccS    //시작 행위만 가능          |
  |false| true | false | AccR    //복귀 행위만 가능          |
  |false| false| true  | AccE    //상태 감지만 가능  (파트감지) |
  |true | true | false | AccSR   //시작, 복귀 행위만 가능     |
  |true | false| true  | AccSE   //시작, 감지 행위만 가능     |
  |false| true | true  | AccRE   //복귀, 감지 행위만 가능     |
  |true | true | true  | AccSRE  //시작, 복귀, 감지 전부가능   |

  - 행위 별 인터페이스 우선 순위 타입
    - Interface Priority =        | StartFirst        | ResetFirst        | LastEvent
