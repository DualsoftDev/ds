# DS text language table



## 1. Sequence
### 1.1 Causal
|Id| Item | Unit |Example|   Desc |  GUI | 
|:---:|:----|:--:|:---:|:----|:---|
|Seq1|Start Causal|>| A > B > C |B be caused by A | <div class="mermaid">graph LR;A((A)) --> B((B)) --> C((C));</div> |
|Seq2|Reset Causal| \|> | A > B <\| C|B is initialized to A | |
|Seq3|And Causal|,|A,B,C > D | C be caused by A & B | |
|Seq4|Or Causal|\\n| A,B>D<p>C>D | C be caused by A or B ||

</BR>

### 1.2 Call

|Id| Item | Unit | Example | Desc |   GUI | 
|:---:|:----|:--:|:----|:---|:---|
|Seq5|Call | ~ |A ~ B |  B be called by A ||
|Seq6|And Call|,| A,B,C ~ D,E|D & E be Called by A & B & C ||

</BR>

## 2. Data

</BR>

### 2.1 Comparision operation

|Id| Item | Unit | Example| Desc | Extension | Extension GUI | 
|:---:|:----|:--:|:---:|:----|:---|:---|
|Op1|Equals|[macro]=|(B = 3) > A)| A be caused by if B EQ 3. | (B = 3) \| (C > D) > A|
|Op2|Not equals |[macro]!=|(B != 3) > A)| A be caused by if B NE 3. ||
|Op3|Greater than |[macro]>|(B > 3) > A)| A be caused by if B GT 3. ||
|Op4|Less than|[macro]<|(B < 3) > A)| A be caused by if B LT 3. ||
|Op5|Greater Equals than |[macro]>=|(B >= 3) > A)| A be caused by if B GE 3. ||
|Op6|Less Equals than|[macro]<=|(B <= 3) > A)| A be caused by if B LE 3. ||


</BR>

### 2.2 Data transfer

</BR>

|Id| Item | Unit | Example| Desc | Extension | Extension GUI | 
|:---:|:----|:--:|:---:|:----|:---|:---|
|Op7|Copy | [macro]<- | (C <- B)  | Copy B to C. |(C <- 0)|
|Op8|Initialize|[macro]= |(A = 65)| Initialize A. |[Sys]A = 65 //초기화 |

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

##### AND
(AND A B)
```mermaid
    graph LR;
        subgraph "(AND A B)"
        A("(A)")-->R((R));
        B("(B)")-->R;
        NA("(!A)") .->R3((R))
        NB("(!B)") .->R4((R))
        end
  ```

##### OR
(OR A B)
```mermaid
    graph LR;
        subgraph "(OR A B)"
        A("(A)")-->R1((R));
        B("(B)")-->R2((R));
        NA("(AND (!A) (!B))") .->R3((R))
        end
  ```

##### NOT
(NOT A)
```mermaid
    graph LR;
        subgraph "(NOT A)"
        A("(A)") .->NOT;
        ON("ON(VCC)")-->NOT((NOT));
        end
  ```

##### XOR
(XOR A B) : Ver1
```
= (OR
    (AND (A) (!B))
    (AND (!A) (B))) 
  ```    


```mermaid
    graph LR;
        subgraph "(XOR A B)"
        A("(A)")-->t1;
        NotB("(!B)")-->t1;

        NotA("(!A)")-->t2;
        B("(B)")-->t2;

        t1 --> R1(R);
        t2 --> R2(R);
        end
  ```


##### NXOR
(NXOR A B)
```mermaid
    graph LR;
        subgraph "(NXOR A B)"
        id("(! (XOR A B))")
        end
  ```


##### NAND
(NAND A B)
```mermaid
    graph LR;
        subgraph "(NAND A B)"
        id("(! (AND A B))")
        end
  ```

##### NOR
(NOR A B)
```mermaid
    graph LR;
        subgraph "(NOR A B)"
        id("(! (OR A B))")
        end
  ```

</BR>


### 3.x 시스템 콜
$f(x)$ 의 return type 이 T 일 경우 (void type 은 제외)
사전에 변수 등록
```
 [VAR] myInstance = ...
```
(SYSTEM_CALL($f(x)$)) 에서 
$f(x)$ 수행 결과 return type T 일때, VAR 로 사전에 정의되어 있어야 한다.
아래 예는 $f(x)$ 수행 결과를 myInstance 에 저장
```mermaid
    graph LR;
        subgraph "(System call)"
        x((prev))-->T((_T))-->X("(SYSTEM_CALL myInstance (f x))")-->y((next)).->T

        end
  ```
##### SYSTEM_CALL.SYSVALUE
```
    MYVAR <- (SYSVALUE myInstance)  // assign
  ```

### 3.2 Time operation

|Id| Item | Unit | Example| Desc | Extension | Extension GUI | 
|:---:|:----|:--:|:---:|:----|:---|:---|
|Op24|On Delay | [macro]#s> | A (5s)> B  | B be caused by A 5sec delay    |A (5ms)> B|
|Op25|Off Delay|[macro]#s!> |(!A) (5s)> B| B be caused by not End A 5sec delay    |(!A) (5ms)> B |

##### DELAY
(DELAY delay)
```mermaid
    graph LR;
        subgraph "(DELAY delay)"
        X("(SYSTEM_CALL _ (TIMER delay))")
        end
  ```

(ONDELAY A delay)
- A 수행 후, 값 관찰? or 수행하지 않고 값 관찰?
```mermaid
    graph LR;
        subgraph "(ONDELAY A delay) > B"
        id("(A)") --> R((_R)) --> X("(DELAY delay)") --> B((B)) .-> R
        end
  ```

(OFFDELAY A delay)
```mermaid
    graph LR;
        subgraph "(OFFDELAY A delay) > B"
        id("(! A)") --> R((_R)) --> X("(DELAY delay)") --> B((B)) .-> R
        end
  ```

</BR>

### 3.3 Value operation

|Id| Item | Unit | Example| Desc | Extension | Extension GUI | 
|:---:|:----|:--:|:---:|:----|:---|:---|
|Op26|Value |[macro].V | (A.V)> B  | B be caused by A End Value    |A> (Start First _A) <\| (Reset A);  _A > B ||
|Op27|Going|[macro].G |(A.G)> B | B be caused by A Going Value     |||
|Op28|Homing|[macro].H |(A.H)> B | B be caused by A Homing Value     |||

##### VALUE
(VALUE A)
- PRIMITIVE : A.End

##### GOING
(GOING A)
- A.{S, R, E} 가 read access 접근 가능할 때에 한함.
1. A가 Start 우선인 경우
    (AND (A.S) (!A.E))
1. A가 Reset 우선인 경우
    (AND (!A.R) (AND (A.S) (!A.E)))
       
##### HOMING
(HOMING A)
1. A가 Start 우선인 경우
    (AND (!A.S) (AND (A.R) (A.E)))
1. A가 Reset 우선인 경우
    (AND (A.R) (A.E))



</BR>

### 3.4 Calculation operation

|Id| Item | Unit | Example| Desc | Extension | Extension GUI | 
|:---:|:----|:--:|:---:|:----|:---|:---|
|Abs | [macro]ABS | (ABS A)  | Calculate the absolute value of A. |
|Op30|Sin|[macro]SIN |(SIN A)| Calculate the Sin of A. | 
|Op31|Round | [macro]ROUND | (ROUND A) | Calculate the rounding of A.  | 
|Op##|...|

##### ABS
(ABS A)

= (SYSTEM_CALL Result (ABS (A)))
  (SYSVALUE Result)


## 4. Interface
## 5. System


