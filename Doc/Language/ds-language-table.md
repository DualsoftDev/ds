# DS text language table

## 1. Sequence

### 1.1 Causal

| Item | Unit | Example| Desc | Extenstion | Extenstion GUI |
|:----|:--:|:---:|:----|:---|:---|
|Start Causal|>|A > B| B be caused by A | A > B > C > D|
|Reset Causal| \|> |A \|> B| B is initialized to A | A > B <\| C|
|And Causal|,|A,B > C| C be caused by A & B | |
|Or Causal|\\n|A>C<p>B>C| C be caused by A or B | |

</BR>

### 1.2 Call

| Item | Unit | Example| Desc | Extenstion | Extenstion GUI |
|:----|:--:|:---:|:----|:---|:---|
|Call | ~ | A ~ B | B be called by A ||
|And Call|,|A,B,C ~ D,E| D & E be Called by A & B & C ||

</BR>

## 2. Data

</BR>

### 2.1 Comparision operation

| Item | Unit | Example| Desc | Extenstion | Extenstion GUI |
|:----|:--:|:---:|:----|:---|:---|
|Equals|[macro]=|(B = 3) > A)| A be caused by if B EQ 3. | (B = 3) \| (C > D) > A|
|Not equals |[macro]!=|(B != 3) > A)| A be caused by if B NE 3. ||
|Greater than |[macro]>|(B > 3) > A)| A be caused by if B GT 3. ||
|Less than|[macro]<|(B < 3) > A)| A be caused by if B LT 3. ||
|Greater Equals than |[macro]>=|(B >= 3) > A)| A be caused by if B GE 3. ||
|Less Equals than|[macro]<=|(B <= 3) > A)| A be caused by if B LE 3. ||


</BR>

### 2.2 Data transfer

</BR>

| Item | Unit | Example| Desc | Extenstion | Extenstion GUI |
|:----|:--:|:---:|:----|:---|:---|
|Copy | [macro]<- | (C <- B)  | Copy B to C. |(C <- 0)|
|Initialize|[macro]= |(A = 65)| Initialize A. |[Sys]A = 65 //초기화 |

</BR>

### 2.3 Arithmetic operation

| Item | Unit | Example| Desc | Extenstion | Extenstion GUI |
|:----|:--:|:---:|:----|:---|:---|
|Addition | [macro]+ | (B + 3)  | B plus 3. |(C <- (B + 3)) > A|
|Subtraction|[macro]- |(B - 3)| B minus 3. | |
|Multiplication | [macro]* | (B * 3)  | B multiplied by 3. |((A + 3) * 3)|
|Division|[macro]/ |(B / 3)| B divided by 3. | |

</BR>

### 2.4 Data conversion

| Item | Unit | Example| Desc | Extenstion | Extenstion GUI |
|:----|:--:|:---:|:----|:---|:---|
| Numeric  | [macro]NUM  | (C <- (NUM B))  | C converts B to Numeric.  | B = 65 //초기화 |
| String  | [macro]STR  | (C <- (STR B))  | C converts B to String.  | [Sys]C <- STR(B) //C에 'A' Setting |
| BCD  | [macro]BCD  | (C <- (BCD B))  | C converts B to BCD.  |
| BIN  | [macro]BIN  | (C <- (BIN B))  | C converts B to BIN.  |

</BR>



## 3. Application

</BR>

### 3.1 Logical operation

| Item | Unit | Example| Desc | Extenstion | Extenstion GUI |
|:----|:--:|:---:|:----|:---|:---|
| And | [macro]& | (A&B) > C | C be caused by A end  & B end |
| Or | [macro]\| | (A\|B) > C | C be caused by A end or B end | 
| Not | [macro]! | (!A) > B | B be caused by not end A | (!A \|> B) |
| XOR | [macro]XOR | (XOR B, C) > A | A is exclusive or (B end, C end) |
| NXOR | [macro]NXOR | (NXOR B, C) > A | A is NXOR (B end, C end) |
| NAND | [macro]NAND | (NAND B, C) > A | A is NAND (B end, C end) |
| NOR | [macro]NOR | (NOR B, C) > A | A is NOR (B end, C end) |

</BR>

### 3.2 Time operation

| Item | Unit | Example| Desc | Extenstion | Extenstion GUI |
|:----|:--:|:---:|:----|:---|:---|
|On Delay | [macro]#s> | A (5s)> B  | B be caused by A 5sec delay    |A (5ms)> B|
|Off Delay|[macro]#s!> |(!A) (5s)> B| B be caused by not End A 5sec delay    |(!A) (5ms)> B |

</BR>

### 3.3 Calculation operation

| Item | Unit | Example| Desc | Extenstion | Extenstion GUI |
|:----|:--:|:---:|:----|:---|:---|
|Abs | [macro]ABS | (ABS A)  | Calculate the absolute value of A. |
|Sin|[macro]SIN |(SIN A)| Calculate the Sin of A. | 
|Round | [macro]ROUND | (ROUND A) | Calculate the rounding of A.  | 
|...|


