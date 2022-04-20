:smile: Welcome to the DS world  :smile:
# DS Language FAQ 
`frequently asked question`

1. Causal (인과 정의)

| Num | Question :question:  | Answer     :exclamation: | Reference |
|:--:| :-----: | ---- |  ---- | 
|1.1 |A > B | After action A, action B <p>`A행위 후 B행위`|
|1.2 |(A) > B | Action B if A value is true <p>`A 값이 true인 경우 B 행위`|
|1.3 |@OnlyS(A, B) | Action B if A value is true, Reset B if A value is false  <p>`A 값이 true인 경우 B 행위실행, A 값이 false인 경우 B행위복귀`| IF6 -[4.3 Single  operation](../Language/ds-language-table.md/)|
|1.4 |A \|> B | Reset B at the start of action A <p>`A행위 시작시에 B리셋`|
|1.5 |(A) \|> B | Reset B if A value is true <p>`A 값이 true인 경우 B리셋`|
|1.6 |@OnlyR(A, B) | Action B if A value is false, Reset B if A value is true  <p>`A 값이 false인 경우 B 행위실행, A 값이 true인 경우 B행위복귀`| IF8 - [4.3 Single  operation](../Language/ds-language-table.md/)|

2. Call (행위 호출)

| Num | Question :question:  | Answer     :exclamation: 
|:--:| :-----: | ---- | 
|2.1 |C = {A ~ B} | Execution of action C starts A and completes when B ends  <p>`행위 C 실행은 A를 시작시켜 B가 종료되면 수행완료`|
|2.2 |C = {A, B ~ } | Execution of action C is completed when A and B are started  <p>`행위 C 실행은 A, B를 시작시키면 수행완료`|
|2.3 |C = { ~ A, B} | Execution of action C is completed when A and B are finished<p>`행위 C 실행은 A, B가 종료되면 수행완료`|
|2.4||
3. Children  (행위 자식)

| Num | Question :question:  | Answer     :exclamation: 
|:--:| :-----: | ---- | 
|3.1 |D = {A > B > C } | Action D executes actions A, B, and C once in sequence.<p>`행위 D는 행위 A, B, C를 차례로 한 번 실행`|
|3.2||
4. System Root Causal (시스템 최상위 인과)

| Num | Question :question:  | Answer     :exclamation: 
|:--:| :-----: | ---- | 
|4.1 |[sys]D = {A > B > C} | System D processes action B after action A and action C after action B.<p>`시스템 D는 A행위 후 B행위, B행위 후 C 행위를 각각 처리`|
|4.2||

5. System Macro
6. System Access
7. Function
8. ETC

