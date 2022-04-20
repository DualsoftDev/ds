# DS text language table
## 1. Sequence
### 1.1 Causal(원인결과 정의)
|Id| Item | Unit |Example|   Desc |  GUI | 
|:---:|:----|:--:|:---:|:----|:---|
|SEQ1|Start Causal|`>`| `A > B > C` |Action B is caused by action A, action C is caused by action B  <p>`B행위는 A행위으로, C행위는 B행위으로 인해 수행`| ![AAA](./png/Seq1.dio.png)|
|SEQ2|Reset Causal| \|> | A > B <\| C|Action B is caused by action A, B is initialized(reset) to action A <p>`B행위는 A행위으로 인해 발생 하며 B행위는 C행위으로 복귀`| ![AAA](./png/Seq2.dio.png)|
|SEQ3|And Causal|`,`|`A,B,C > D,E` | D, E be caused by action (A and B and C) <p>`D, E행위는 A행위, B행위, C행위에 의해 수행`|  ![AAA](./png/Seq3.dio.png)|
|SEQ4|Or Causal|\|\|| A, B \|\| C > D | D be caused by (A and B) or C <p>`D행위는 A행위, B행위에 의해 수행하거나, C 행위에 의해 수행`| ![AAA](./png/Seq4.dio.png)|
|SEQ5|Causal Split|`;`| `A,B > D;C > D` | D be caused by A and B<p> D be caused by C <p>`D행위는 A행위, B행위에 의해 수행하거나, C 행위에 의해 수행`| ![AAA](./png/Seq5.dio.png)|

</BR>

### 1.2 Call(행위 부르기)

|Id| Item | Unit | Example | Desc |   GUI | 
|:---:|:----|:--:|:----|:---|:---|
|SEQ6|Call | `~` |`C = {A ~ B}`<p> `C = {_ ~ B}` <p> empty key is `_` | Action C indicates the end state of B by executing A<p>`C행위는 A를 수행시킴으로 B의 종료상태를 관찰`| ![AAA](./png/Seq6.dio.png)|
|SEQ7|And Call|`,`| `F = {A,B,C ~ D,E}`|Action F indicates the end state of D, E by executing A, B, C<p>`F행위는 A, B, C를 수행시킴으로 D, E의 종료상태를 관찰`| ![AAA](./png/Seq7.dio.png)|
|SEQ8|Reset Call|`~ ~`| `H = {A,B,C ~ D,E ~ F,G}`|Action H indicates the end state of D, E by executing A, B, C and by reset F, G, C<p>`F행위는 A, B, C를 수행시킴으로 D, E의 종료상태를 관찰하며 F, G를 수행시켜 값을 리셋`| ![AAA](./png/Seq8.dio.png)|

</BR>

### 1.3 Parent(행위 부모 할당)

|Id| Item | Unit | Example | Desc |   GUI | 
|:---:|:----|:--:|:----|:---|:---|
|SEQ9|system Parent | `[sys]=` |  [sys]D = { A > B <\| C } | system D processes that causality concurrently <p>` 시스템 D는 해당 인과를 동시적으로 처리`  | ![AAA](./png/Seq9.dio.png)|
|SEQ10|Segement Parent| `=` |  D = { A > B <\| C } | Action D processes its causal relationship sequentially <p>` 행위 D는 해당 인과를 순차적으로 처리` | ![AAA](./png/Seq10.dio.png)|
</BR>


### 1.4 Causal Extension (행위 확장)

|Id| Item | Unit | Example | Desc |   GUI | 
|:---:|:----|:--:|:----|:---|:---|
|SEQ11| mutual interlock | <\|\|> |  A <\|\|> B <p>is equal to A <\| B ; A \|> B| Action A and Action B are mutually interlocked <p>` A 행위와 B 행위는 상호 인터락`  | ![AAA](./png/Seq11.dio.png)|
|SEQ12| resetStart | \|>> |  A \|>> B <p>is equal to A > B ; A \|> B| Action B is caused by action A, B is initialized(reset) to action A <p>`B행위는 A행위으로 인해 수행 하며 B행위는 A행위으로 복귀` | ![AAA](./png/Seq12.dio.png)|

</BR>


## 2. Data


### 2.1 Value operation (행위 값)

|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:-------:|:---:|:----|:---|
|OP1|End  Value | ( ) | `(Seg), A > B`  | B be caused by action A when the Seg End Port (sensor) value is 'True'. <p>` 행위 B는 Seg의 End Port(sensor) 값이 'True' 일 경우에서 행위 A가 수헹되었을때 수행`    |![AAA](./png/Op1.dio.png)|
|OP2|End Set Value | #set| `#set (Seg) > B` | B be caused by Seg End Port latch value(auto reset by #g(B)) <p>` 행위 B는 Seg의 End Port(sensor) 값이 'True' 면 값 유지(B행위 Going 시에 자동 값 리셋)`      |![AAA](./png/Op2.dio.png)|
|OP3|End  Value | #latch( , )| `#latch((SegA), #g (SegB)) > B` | B be caused by Seg End Port latch value(auto reset by #g(B)) <p>` 행위 B는 Seg의 End Port(sensor) 값이 'True' 면 값 유지(설정 값에 의한 리셋)`  |![AAA](./png/Op3.dio.png)|
|OP4|Going Status|#g |`#g(Seg) > B`| B be caused by Seg Going Value<p>` 행위 B는 Seg가 Going 경우 인해 수행`      |![AAA](./png/Op4.dio.png)|
|OP5|Homing Status|#h |`#h(Seg) > B` | B be caused by Seg Homing Value <p>` 행위 B는 Seg가 Homing 경우 인해 수행`     |![AAA](./png/Op5.dio.png)|



</BR>

### 2.2 Comparision operation (비교연산) 

|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:--:|:---:|:----|:---|
|OP6|Equals|#( == )|#(B == 3) > A| A be caused by if B EQ(equal) 3. |    ![AAA](./png/Op6.dio.png)|
|OP7|Not equals |#( != )|#(B != 3) > A| A be caused by if B NE(not equal) 3. |    ![AAA](./png/Op7.dio.png)|
|OP8|Greater than |#( > )|#(B > 3) > A| A be caused by if B GT(greater than) 3. |    ![AAA](./png/Op8.dio.png)|
|OP9|Less than|#( < )|#(B < 3) > A| A be caused by if B LT(less than) 3. |    ![AAA](./png/Op9.dio.png)|
|OP10|Greater Equals than |#( >= )|#(B >= 3) > A| A be caused by if B GE(greater than or equal ) 3.|    ![AAA](./png/Op10.dio.png)|
|OP11|Less Equals than|#( <= )|#(B <= 3) > A| A be caused by if B LE(less than or equal ) 3. |    ![AAA](./png/Op11.dio.png)|

</BR>


### 2.3 Arithmetic operation(산술연산)

|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:--:|:---:|:----|:---|
|OP12|Addition | + | #(C != B + 3) | C != B plus 3. | ![AAA](./png/Op12.dio.png)|
|OP13|Subtraction|- |#(C > B - 3)| C != B minus 3. |  ![AAA](./png/Op13.dio.png)|
|OP14|Multiplication | * | @(C =  B * 3)  | B multiplied by 3 to assign to C| ![AAA](./png/Op14.dio.png)|
|OP15|Division|/ | #(C ==  B / 3) | C == B divided by 3. | ![AAA](./png/Op15.dio.png)|

</BR>



### 2.4 Logical operation(논리연산)

|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:--:|:---:|:----|:---|
|OP16| And | & | #(A&B) > C | C be caused by A end  & B end | ![AAA](./png/Op16.dio.png)|
|OP17| Or | \| | #(A\|B) > C | C be caused by A end or B end | ![AAA](./png/Op17.dio.png)|
|OP18| Not | ! | #(!A) > B | B be caused by not end A | ![AAA](./png/Op18.dio.png)|
|OP19| XOR | #xor( , ) | #xor(B, C) > A | A is exclusive or (B end, C end) |![AAA](./png/Op19.dio.png)|
|OP20| NXOR | #nxor( , ) | #nxor(B, C) > A | A is NXOR (B end, C end) |![AAA](./png/Op20.dio.png)|
|OP21| NAND | #nand( , ) | #nand(B, C) > A | A is NAND (B end, C end) |![AAA](./png/Op21.dio.png)|
|OP22| NOR | #nor( , ) | #nor(B, C) > A | A is NOR (B end, C end) |![AAA](./png/Op22.dio.png)|
</BR>


### 2.5 Data operation(데이터 처리)


|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:--:|:---:|:----|:---|
|OP23|Copy | `=` | `@(C = B)`  | Copy B to C. |![AAA](./png/Op23.dio.png)|
|OP24|Initialize|`=` |`#(A < 65) > @(A = 65)`| Initialize A. |![AAA](./png/Op24.dio.png)|

</BR>


### 2.6 Time operation(시간연산)

|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:--:|:---:|:----|:---|
|OP25|On Delay(Start Edge Only) | @ms, @s| A > @ms (500) > B  | B be caused by A finish 500 msec delay    |![AAA](./png/Op25.dio.png)|
|OP26|Off Delay |None || Use On Delay    ||

</BR>

### 2.7 Data conversion(값 형식 변환)

|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:--:|:---:|:----|:---|
|OP27| Numeric  | #num ()   |` @(C = #num (B)) ` | C converts B to Numeric.  | |
|OP28| String  |#str ()   | ` @(C = #str (B)) `  | C converts B to String.  |  |
|OP29| BCD  | #bcd ()   |` @(C = #bcd (B)) `   | C converts B to BCD.  |
|OP30| BIN  | #bin ()  |` @(C = #bin (B)) ` | C converts B to BIN.  |

</BR>






## 3. Application

### 3.1 Calculation operation

|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:--:|:---:|:----|:---|
|FUN1|Abs | #abs  | #abs (A)  | Calculate the absolute value of A. |
|FUN2|Sin| #sin|#sin (A)| Calculate the Sin of A. | 
|FUN3|Round | #round | #round (A) | Calculate the rounding of A.  | 



## 4. Interface  

### 4.1 Priority operation

|Id| Item | Unit | Example| Desc | GUI |
|:---:|:----|:--:|:---:|:----|:---|
|IF1|Start Priority | @sf | A > @sf (B) <\|C  | The B start value overrides the B reset value. | ![AAA](./png/IF1.dio.png)|
|IF2|Last Priority  |  @lf  | A > @lf (B) <\|C | During startup/reset, last occurrence takes precedence | ![AAA](./png/IF2.dio.png)|
</BR>

### 4.2  Sustain operation

|Id| Item | Unit | Example| Desc | GUI |
|:---:|:----|:--:|:---:|:----|:---|
|IF3|Start Sustain | @pushs ( ) | A > @pushs (B)  | B start signal Sustain until B is Finish |  ![AAA](./png/IF3.dio.png)|
|IF4|Reset Sustain | @pushr ( ) | A > @pushr (B)  | B reset signal Sustain until B is Ready |  ![AAA](./png/IF4.dio.png)|
|IF5|SR Sustain | @pushsr | A > @pushsr (B)  <\| C | B start signal Sustain until B is Finish and <p>  B reset signal Sustain until B is Ready  |  ![AAA](./png/IF5.dio.png)|

</BR>



### 4.3 Single  operation

|Id| Item | Unit | Example| Desc | GUI |
|:---:|:----|:--:|:---:|:----|:---|
|IF6|Start Single | @onlys  ( )| A > @onlys (B)  | The B reset value is B start not |  ![AAA](./png/IF6.dio.png)|
|IF7|Reset Single | @onlyr ( )| A > @onlyr (B)  | The B start value is B reset not |  ![AAA](./png/IF7.dio.png)|
|IF8|Self Reset | @selfr ( )| A > @selfr (B)    | The B reset value is B end Value |  ![AAA](./png/IF8.dio.png)|

</BR>


## 5. system

### 5.1  Constain

|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:--:|:---:|:----|:---|
|SYS1|Numeric |   | #(C = 3) > A  | A be caused by B Equal to 3 ||
|SYS2|String |' ' | #('C' = B) > A| A be caused by B Equal to 'C'||


### 5.2  system Bit

|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:--:|:---:|:----|:---|
|SYS3|Always On | _on | _on > A  | A be caused by Always On ||
|SYS4|Always Off |_off | _off > A| A be caused by Always Off ||
|SYS5|Running Flag |_run | _run > A| A be caused by system Run ||
|SYS6|Stop Flag |_stop | _stop > A| A be caused by system Stop||
|SYS7|Running Rising |_rr | _rr > A | A be caused by system Run Rising||
|SYS8|Running Falling |_rf | _rf > A | A be caused by system Run Rising ||



### 5.3  system timer

|Id| Item | Unit | Example| Desc | GUI |
|:---:|:----|:--:|:---:|:----|:---|
|SYS9|Toggle #s | _t | _t50ms > A  | On/Off occurs at periodic intervals of 50msec. | |