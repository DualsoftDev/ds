# DS text language table
## 1. Sequence
### 1.1 Causal
|Id| Item | Unit |Example|   Desc |  GUI | 
|:---:|:----|:--:|:---:|:----|:---|
|Seq1|Start Causal|>| A > B > C |B be caused by A | <div class="mermaid">flowchart LR;A((A)) --> B((B)) --> C((C));</div>
|Seq2|Reset Causal| \|> | A > B <\| C|B is initialized to A | <div class="mermaid">flowchart LR;A((A)) --> B((B)); C((C)) .-> B((B));</div>
|Seq3|And Causal|,|A,B,C > D | C be caused by A & B | <div class="mermaid">flowchart LR;A((A)) & B((B)) & C((C)) --> D((D));</div>
|Seq4|Or Causal|\\n| A,B>D<p>C>D | C be caused by A or B | <div class="mermaid">flowchart LR;A((A)) & B((B)) --> D((D)); C((C)) --> D2((D))</div>

</BR>

### 1.2 Call

|Id| Item | Unit | Example | Desc |   GUI | 
|:---:|:----|:--:|:----|:---|:---|
|Seq5|Call | ~ |A ~ B |  B be called by A |<div class="mermaid">flowchart LR;A((A)) --o call([Call]) --> B((B));</div>
|Seq6|And Call|,| A,B,C ~ D,E|D & E be Called by A & B & C |<div class="mermaid">flowchart LR;A((A)) & B((B)) & C((C)) --o call([Call]) --> D((D));call([Call]) --> E((E))</div>

</BR>

## 2. Data

</BR>

### 2.1 Comparision operation

|Id| Item | Unit | Example| Desc | Extension | Extension GUI | 
|:---:|:----|:--:|:---:|:----|:---|:---|
|Op1|Equals|[macro]=|(B = 3) > A)| A be caused by if B EQ 3. | (B = 3) \| (C > D) > A| <div class="mermaid">flowchart LR;M1[B = 3] --> A((A));M2[C > D] --> A2((A))</div>
|Op2|Not equals |[macro]!=|(B != 3) > A)| A be caused by if B NE 3. |(B != 3) \| (C > D) > A| <div class="mermaid">flowchart LR;M1[B != 3] --> A((A));M2[C > D] --> A2((A))</div>
|Op3|Greater than |[macro]>|(B > 3) > A)| A be caused by if B GT 3. |(B > 3) \| (C > D) > A| <div class="mermaid">flowchart LR;M1[B > 3] --> A((A));M2[C > D] --> A2((A))</div>
|Op4|Less than|[macro]<|(B < 3) > A)| A be caused by if B LT 3. |(B < 3) \| (C > D) > A| <div class="mermaid">flowchart LR;M1[B < 3] --> A((A));M2[C > D] --> A2((A))</div>
|Op5|Greater Equals than |[macro]>=|(B >= 3) > A)| A be caused by if B GE 3. |(B >= 3) \| (C > D) > A| <div class="mermaid">flowchart LR;M1[B >= 3] --> A((A));M2[C > D] --> A2((A))</div>
|Op6|Less Equals than|[macro]<=|(B <= 3) > A)| A be caused by if B LE 3. |(B <= 3) \| (C > D) > A| <div class="mermaid">flowchart LR;M1[B <= 3] --> A((A));M2[C > D] --> A2((A))</div>


</BR>

### 2.2 Data transfer

</BR>

|Id| Item | Unit | Example| Desc | Extension | Extension GUI | 
|:---:|:----|:--:|:---:|:----|:---|:---|
|Op7|Copy | [macro]<- | (C <- B)  | Copy B to C. |(C <- 0)| <div class="mermaid">flowchart LR;M[C <- 0]</div>
|Op8|Initialize|[macro]= |(A = 65)| Initialize A. |[Sys]A = 65 //초기화 |<div class="mermaid">flowchart LR;Sys[A = 65]</div>

</BR>

### 2.3 Arithmetic operation

|Id| Item | Unit | Example| Desc | Extension | Extension GUI | 
|:---:|:----|:--:|:---:|:----|:---|:---|
|Op9|Addition | [macro]+ | (B + 3)  | B plus 3. |(C <- (B + 3)) > A|
|Op10|Subtraction|[macro]- |(B - 3)| B minus 3. | |
|Op11|Multiplication | [macro]* | (B * 3)  | B multiplied by 3. |((A + 3) * 3)|
|Op12|Division|[macro]/ |(B / 3)| B divided by 3. | |

</BR>

### 2.4 Data conversion

|Id| Item | Unit | Example| Desc | Extension | Extension GUI | 
|:---:|:----|:--:|:---:|:----|:---|:---|
|Op13| Numeric  | [macro]NUM  | (C <- (NUM B))  | C converts B to Numeric.  | B = 65 //초기화 |
|Op14| String  | [macro]STR  | (C <- (STR B))  | C converts B to String.  | [Sys]C <- STR(B) //C에 'A' Setting |
|Op15| BCD  | [macro]BCD  | (C <- (BCD B))  | C converts B to BCD.  |
|Op16| BIN  | [macro]BIN  | (C <- (BIN B))  | C converts B to BIN.  |

</BR>



## 3. Application

</BR>

### 3.1 Logical operation

|Id| Item | Unit | Example| Desc | Extension | Extension GUI | 
|:---:|:----|:--:|:---:|:----|:---|:---|
|Op17| And | [macro]& | (A&B) > C | C be caused by A end  & B end |
|Op18| Or | [macro]\| | (A\|B) > C | C be caused by A end or B end | 
|Op19| Not | [macro]! | (!A) > B | B be caused by not end A | (!A \|> B) |
|Op20| XOR | [macro]XOR | (XOR B, C) > A | A is exclusive or (B end, C end) |
|Op21| NXOR | [macro]NXOR | (NXOR B, C) > A | A is NXOR (B end, C end) |
|Op22| NAND | [macro]NAND | (NAND B, C) > A | A is NAND (B end, C end) |
|Op23| NOR | [macro]NOR | (NOR B, C) > A | A is NOR (B end, C end) |

</BR>

### 3.2 Time operation

|Id| Item | Unit | Example| Desc | Extension | Extension GUI | 
|:---:|:----|:--:|:---:|:----|:---|:---|
|Op24|On Delay | [macro]#s> | A (5s)> B  | B be caused by A 5sec delay    |A (5ms)> B|
|Op25|Off Delay|[macro]#s!> |(!A) (5s)> B| B be caused by not End A 5sec delay    |(!A) (5ms)> B |

</BR>

### 3.3 Value operation

|Id| Item | Unit | Example| Desc | Extension | Extension GUI | 
|:---:|:----|:--:|:---:|:----|:---|:---|
|Op26|End Value |[macro].E | (A.E)> B  | B be caused by A End Value    |A> (Start First _A) <\| (Reset A);  _A > B ||
|Op27|Start Value |[macro].S | (A.S)> B  | B be caused by A Start Value    |||
|Op28|Reset Value |[macro].R | (A.R)> B  | B be caused by A Reset Value    |||
|Op29|Going Status|[macro].G |(A.G)> B | B be caused by A Going Value     |||
|Op30|Homing Status|[macro].H |(A.H)> B | B be caused by A Homing Value     |||



</BR>

### 3.4 Calculation operation

|Id| Item | Unit | Example| Desc | Extension | Extension GUI | 
|:---:|:----|:--:|:---:|:----|:---|:---|
|Op31|Abs | [macro]ABS | (ABS A)  | Calculate the absolute value of A. |
|Op32|Sin|[macro]SIN |(SIN A)| Calculate the Sin of A. | 
|Op33|Round | [macro]ROUND | (ROUND A) | Calculate the rounding of A.  | 
|Op##|...|




## 4. Interface

### 4.1 Priority operation

|Id| Item | Unit | Example| Desc | Extension | Extension GUI | 
|:---:|:----|:--:|:---:|:----|:---|:---|
|If1|Start Priority | [macro]StartFirst | A > (StartFirst B) <\|C  | The B start value overrides the B reset value. | A > B <p> C,(!A) > B |<div class="mermaid">flowchart LR;A((A)) --> B((B)); C((C)) & NotA[!A] .->B2((B))</div>
|If2|Last Priority  | [macro]LastFirst  | A >  (LastFirst B) <\|C | During startup/reset, last occurrence takes precedence | A > <\| C <p> (C) > B |<div class="mermaid">flowchart LR;A((A)) --> B((B));A((A)) .-> C((C)); C.value[C] .->B2((B))</div> 


### 4.2  Sustain operation

|Id| Item | Unit | Example| Desc | Extension | Extension GUI | 
|:---:|:----|:--:|:---:|:----|:---|:---|
|If3|Start Sustain | [macro]SusS | A > (SusS B)  | Sustain until B is Homing | A > B.TS > B<p> C \|> B.Ts , B | <div class="mermaid">flowchart LR;A((A)) --> B.TS((B.TS)) --> B((B));C((C)) .->  B.TS((B.TS)) & B((B));</div> 
|If4|Reset Sustain |[macro]SusR |A > (SusR B)| Sustain until B is Going | 
|If5|SR Sustain | [macro]SusSR | A > (SusSR B) | Start/Reset Sustain  | 


### 4.3 Single  operation

|Id| Item | Unit | Example| Desc | Extension | Extension GUI | 
|:---:|:----|:--:|:---:|:----|:---|:---|
|If6|Start Single  | [macro]OnlyS | A > (OnlyS B) | The B reset value is B Start not | A > B <\| (!A) | <div class="mermaid">flowchart LR;A((A)) --> B((B));NotA[!A] .-> B((B));</div> 
|If7|Reset Single  | [macro]OnlyR | A > (OnlyR B) | The B start value is B reset not | A \|> B < (!A) | <div class="mermaid">flowchart LR;A((A)) .-> B((B));NotA[!A] --> B((B));</div> 



## 5. System

### 5.1  Constain

|Id| Item | Unit | Example| Desc | Extension | Extension GUI | 
|:---:|:----|:--:|:---:|:----|:---|:---|
|Sys1|Numeric | [macro]# | (#3 + B) > A  | A be caused by B add 56 | #3 = ~ Numeric.Bit0, Numeric.Bit1 |
|Sys2|String |[macro]$ | ($A = B) > A| A be caused by B Equal to 'A' | $A = ~ String.Bit0, String.Bit6 |


### 5.2  System Bit

|Id| Item | Unit | Example| Desc | Extension | Extension GUI | 
|:---:|:----|:--:|:---:|:----|:---|:---|
|Sys3|Always On | [macro]_On | (_On) > A  | A be caused by Always On | Numeric.Bit0 > On |
|Sys4|Always Off |[macro]_Off | (_Off) > A| A be caused by Always Off | (! Numeric.Bit0) > Off |
|Sys5|Running Flag |[macro]_Run | (_Run) > A| A be caused by System Run | (SystemRoot.S) > (OnlyS Run) |
|Sys6|Stop Flag |[macro]_Run | (_Stop) > A| A be caused by System Stop | (SystemRoot.R) > (OnlyS Stop) |


### 5.3  System timer

|Id| Item | Unit | Example| Desc | Extension | Extension GUI | 
|:---:|:----|:--:|:---:|:----|:---|:---|
|Sys7|toggle #s | [macro]_T | (_T 50ms) > A  | A occurs at periodic intervals of 50 msec | T1 <\|> T2; T1 (50ms)> T2 ; T2 (50ms)> T1; (T2.E) > A |

