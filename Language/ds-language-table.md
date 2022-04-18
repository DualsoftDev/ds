# DS text language table
## 1. Sequence
### 1.1 Causal(원인결과 정의)
|Id| Item | Unit |Example|   Desc |  GUI | 
|:---:|:----|:--:|:---:|:----|:---|
|SEQ1|Start Causal|`>`| `A > B > C` |Action B is caused by action A, action C is caused by action B  <p>`B행위는 A행위으로, C행위는 B행위으로 인해 수행`| ![AAA](./png/Seq1.dio.png)|
|SEQ2|Reset Causal| \|> | ```A > B <| C```|Action B is caused by action A, B is initialized(reset) to action A <p>`B행위는 A행위으로 인해 발생 하며 B행위는 A행위으로 복귀`| ![AAA](./png/Seq2.dio.png)|
|SEQ3|And Causal|`,`|`A,B,C > D` | D be caused by action (A & B & C) <p>`D행위는 A행위, B행위, C행위에 의해 수행`|  ![AAA](./png/Seq3.dio.png)|
|SEQ4|Or Causal|`;`| `A,B > D;C > D` | D be caused by A and B, or C <p>`D행위는 A행위, B행위에 의해 수행하거나, C 행위에 의해 수행`| ![AAA](./png/Seq4.dio.png)|

</BR>

### 1.2 Call(행위 부르기)

|Id| Item | Unit | Example | Desc |   GUI | 
|:---:|:----|:--:|:----|:---|:---|
|SEQ5|Call | `~` |`C = {A ~ B}` | Action C indicates the end state of B by executing A<p>`C행위는 A를 수행시킴으로 B의 종료상태를 관찰`| ![AAA](./png/Seq5.dio.png)|
|SEQ6|And Call|`,`| `F = {A,B,C ~ D,E}`|Action F indicates the end state of D, E by executing A, B, C<p>`F행위는 A, B, C를 수행시킴으로 D, E의 종료상태를 관찰`| ![AAA](./png/Seq6.dio.png)|

</BR>

### 1.3 Parent(행위 부모 할당)

|Id| Item | Unit | Example | Desc |   GUI | 
|:---:|:----|:--:|:----|:---|:---|
|SEQ7|System Parent | `[Sys]=` |  `[Sys]D = { A > B <| C }`| System C processes that causality concurrently <p>` 시스템 C는 해당 인과를 동시적으로 처리`  | ![AAA](./png/Seq7.dio.png)|
|SEQ8|Segement Parent| `=` |  `D = { A > B <| C }`| Action C processes its causal relationship sequentially <p>` 행위 C는 해당 인과를 순차적으로 처리` | ![AAA](./png/Seq8.dio.png)|
</BR>


### 1.4 Causal Extension (행위 확장)

|Id| Item | Unit | Example | Desc |   GUI | 
|:---:|:----|:--:|:----|:---|:---|
|SEQ9| mutual interlock | <\|\|> |  `A <||> B` <p>is equal to `A <| B ; A |> B`| Action A and Action B are mutually interlocked <p>` A 행위와 B 행위는 상호 인터락`  | ![AAA](./png/Seq9.dio.png)|
|SEQ10| resetStart | \|>> |  `A |>> B` <p>is equal to `A > B ; A |> B`| Action C processes its causal relationship sequentially <p>` 행위 C는 해당 인과를 순차적으로 처리` | ![AAA](./png/Seq10.dio.png)|

</BR>


## 2. Data


### 2.1 Value operation (행위 값)

|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:--:|:---:|:----|:---|
|OP1|End  Value | () | `(Seg) > B`  | B be caused by Seg End Port(Sensor OUT) value <p>` 행위 B는 Seg의 End Port(sensor) 값이 'True' 일 경우 인해 수행`    |
|OP2|End Rising Value | @RISING()| `@RISING(Seg) > B` | B be caused by Seg End Port rising value <p>` 행위 B는 Seg의 End Port(sensor) 값이 'RISING' 일 경우 인해 수행`      |
|OP3|End Falling Value |@FALLING() | `@FALLING(Seg) > B` | B be caused by Seg Reset Port falling value <p>` 행위 B는 Seg의 End Port(sensor) 값이 'FALLING' 일 경우 인해 수행`    |
|OP4|Going Status|@G() |`@G(Seg) > B`| B be caused by Seg Going Value<p>` 행위 B는 Seg가 Going 경우 인해 수행`      |
|OP5|Homing Status|@H() |`@H(Seg) > B` | B be caused by Seg Homing Value <p>` 행위 B는 Seg가 Homing 경우 인해 수행`     |



</BR>

### 2.2 Comparision operation (비교연산) - system level only -

|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:--:|:---:|:----|:---|
|OP6|Equals|==|(B == 3) > A| A be caused by if B EQ(equal) 3. | (B = 3) \| (C > D) > A| 
|OP7|Not equals |!=|(B != 3) > A| A be caused by if B NE(not equal) 3. |(B != 3) \| (C > D) > A|
|OP8|Greater than |>|(B > 3) > A| A be caused by if B GT(greater than) 3. |(B > 3) \| (C > D) > A| 
|OP9|Less than|<|(B < 3) > A| A be caused by if B LT(less than) 3. |(B < 3) \| (C > D) > A| 
|OP10|Greater Equals than |>=|(B >= 3) > A| A be caused by if B GE(greater than or equal ) 3. |(B >= 3) \| (C > D) > A|
|OP11|Less Equals than|<=|(B <= 3) > A| A be caused by if B LE(less than or equal ) 3. |(B <= 3) \| (C > D) > A|

</BR>

</BR>

### 2.3 Arithmetic operation(산술연산)

|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:--:|:---:|:----|:---|
|OP12|Addition | + | (B + 3)  | B plus 3. |(C <- (B + 3)) > A|
|OP13|Subtraction|- |(B - 3)| B minus 3. | |
|OP14|Multiplication | * | (B * 3)  | B multiplied by 3. |((A + 3) * 3)|
|OP15|Division|/ |(B / 3)| B divided by 3. | |

</BR>



### 2.4 Logical operation(논리연산)

|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:--:|:---:|:----|:---|
|OP16| And | & | (A&B) > C | C be caused by A end  & B end |
|OP17| Or | \| | (A\|B) > C | C be caused by A end or B end | 
|OP18| Not | ! | (!A) > B | B be caused by not end A | (!A \|> B) |
|OP19| XOR | @XOR() | (XOR B, C) > A | A is exclusive or (B end, C end) |
|OP20| NXOR | @NXOR() | (NXOR B, C) > A | A is NXOR (B end, C end) |
|OP21| NAND | @NAND() | (NAND B, C) > A | A is NAND (B end, C end) |
|OP22| NOR | @NOR() | (NOR B, C) > A | A is NOR (B end, C end) |
</BR>


### 2.5 Data operation(데이터 처리)


|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:--:|:---:|:----|:---|
|OP23|Copy | `<-` | `C <- B`  | Copy B to C. |(C <- 0)|
|OP24|Initialize|`=` |`A = 65`| Initialize A. |[Sys]A = 65 //초기화 |

</BR>


### 2.6 Time operation(시간연산)

|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:--:|:---:|:----|:---|
|OP25|On Delay(Start Edge Only) | @ms, @s| A > @500ms > B  | B be caused by A finish 500 msec delay    |A @5ms> B|
|OP26|Off Delay |None || Use On Delay    ||

</BR>

### 2.7 Data conversion(값 형식 변환)

|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:--:|:---:|:----|:---|
|OP27| Numeric  | @NUM()  | C <- @NUM(B)  | C converts B to Numeric.  | |
|OP28| String  |@STR()  | C <- @STR(B)  | C converts B to String.  |  |
|OP29| BCD  | @BCD()  | C <- @BCD(B)  | C converts B to BCD.  |
|OP30| BIN  | @BIN()  | C <- @BIN(B)  | C converts B to BIN.  |

</BR>






## 3. Application

### 3.1 Calculation operation

|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:--:|:---:|:----|:---|
|FUN1|Abs | @ABS() | @ABS (A)  | Calculate the absolute value of A. |
|FUN2|Sin| @SIN()|@SIN (A)| Calculate the Sin of A. | 
|FUN3|Round | @ROUND()| @ROUND (A) | Calculate the rounding of A.  | 



## 4. Interface  

### 4.1 Priority operation

|Id| Item | Unit | Example| Desc | GUI |
|:---:|:----|:--:|:---:|:----|:---|
|IF1|Start Priority | (StartFirst SegX)<p> or (IF1 SegX) | A > (StartFirst B) <\|C  | The B start value overrides the B reset value. | A > B <p> C,(!A) \|> B |<div class="mermaid">flowchart LR;A((A)) --> B((B)); C((C)) & NotA[!A] .->B((B))</div>
|IF2|Last Priority  | (LastFirst  SegX)<p> or (IF2 SegX)  | A >  (LastFirst B) <\|C | During startup/reset, last occurrence takes precedence | C > CT <\| A  <p> A > B <\| (CT) | <div class="mermaid">flowchart LR;A((A)) --> B((B)); A((A)) .-> CT((CT)); C((C)) --> CT((CT)); CT2[CT] .->B((B))</div>


### 4.2  Sustain operation

|Id| Item | Unit | Example| Desc | GUI |
|:---:|:----|:--:|:---:|:----|:---|
|IF3|Start Sustain | (SusS SegX)<p> or (IF3 SegX)| A > (SusS B)  | Sustain until B is Homing | A > (SusS B) <\| C | <div class="mermaid">flowchart LR; A((A)) --> M1[SusS B];C((C)) .-> M1[SusS B]</div>
|IF4|Reset Sustain |(SusR SegX)<p> or (IF4 SegX) |A > (SusR B)| Sustain until B is Going | A > (SusR B) <\| C | <div class="mermaid">flowchart LR; A((A)) --> M1[SusR B];C((C)) .-> M1[SusR B]</div> 
|IF5|SR Sustain | (SusSR SegX) <p> or (IF5 SegX)| A > (SusSR B) | Start/Reset Sustain  | A > (SusSR B) <\| C | <div class="mermaid">flowchart LR; A((A)) --> M1[SusSR B];C((C)) .-> M1[SusSR B]</div>


### 4.3 Single  operation

|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:--:|:---:|:----|:---|
|IF6|Start Single  | (OnlyS SegX) <p> or (IF6 SegX)| A > (OnlyS B) | The B reset value is B Start not | A > B <\| (!A) | 
|IF7|Reset Single  | (OnlyR SegX)<p> or (IF7 SegX) | A > (OnlyR B) | The B start value is B reset not | A \|> B < (!A) | 
|IF8|Self Reset  | (SelfR SegX)<p> or (IF8 SegX) | A > (SelfR B) | The B reset value is B end Value | A > (SusR B) <\| (B.E) |


## 5. System

### 5.1  Constain

|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:--:|:---:|:----|:---|
|SYS1|Numeric |   | (3 + B) > A  | A be caused by B add 56 | #3 = ~ Numeric.Bit0, Numeric.Bit1 |
|SYS2|String |' ' | ('C' = B) > A| A be caused by B Equal to 'A' | $A = ~ String.Bit0, String.Bit6 |


### 5.2  System Bit

|Id| Item | Unit | Example| Desc |  GUI |
|:---:|:----|:--:|:---:|:----|:---|
|SYS3|Always On | _On | _On > A  | A be caused by Always On | Numeric.Bit0 > On |
|SYS4|Always Off |_Off | _Off > A| A be caused by Always Off | (! Numeric.Bit0) > Off |
|SYS5|Running Flag _Run | _Run > A| A be caused by System Run | (SystemRoot.S) > (OnlyS Run) |
|SYS6|Stop Flag |_Run | _Stop > A| A be caused by System Stop | (SystemRoot.R) > (OnlyS Stop) | 
|SYS7|Running Rising |_RisingRun | _RisingRun > A | A be caused by System Run Rising | (SystemRoot.S) > (OnlyS Run) | 


### 5.3  System timer

|Id| Item | Unit | Example| Desc | GUI |
|:---:|:----|:--:|:---:|:----|:---|
|SYS8|toggle #s | _T | _T50ms > A  | On/Off occurs at periodic intervals of 50msec. | T1 <\|> T2; T1 (50ms)> T2 ; T2 (50ms)> T1; (T2.E) > A |