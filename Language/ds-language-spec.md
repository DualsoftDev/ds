# DS text language spec

DS 언어는 dualsoft® 이름으로 2019년에 상표등록(40-1609932)하여 시스템 인과를 정의하는 언어 입니다.


## 주요스펙 

### 1 모델링 기본 유닛
 - Start 인과
    - A > B // A 행위후 B 행위  
 - Reset 인과 
    - B |> C  //C 행위는 B의해 복귀
 - End 값 
    - #(D) //D의 true, false 값 

  ```
     /* DS Language Unit
          1. '>'  is Start
          2. '|>' is Reset
          3. Action value(True/False) is End
     */

     //example 
    
    [sys]mySystem  = {

        [task] t = { start; reset; A; B; C;
          A = {exSys.Action1 ~ exSys.Action2};
          B = {exSys.Action3 ~ exSys.Action5};
          C = {exSys.Action4 ~ exSys.Action6};
        }

        [flow] f = {
              #(Start) > Segment <| #(Reset);
              Segment = { A > B > C };
        }

    }
  ```

|Id| Item |  Example| 
|:---:|:----|:----|
|1|모든 이름 및 시스템 예약어 대소문자 구분     |      test <> Test, [sys] <> [SYS] 서로 다름|
|2|띄어쓰기 대신 '_' 사용                       |      가공 작업 => 가공_작업|
|3|이름시작에 '_' 및 숫자 금지                  |      _test (X), 1cycle (X), "1cycle" (0)|
|4|기호는 이름에 사용금지                       |      A+, A- 대신 Ap, Am or "A+", "A-" |
|5|라인 종료시에 ';' 작성                       |      R1 =  {A > B};|
|6|함수 행위는 @XX() 규격사용                   |      A > @pushs (B) > C (인과 사용)|
|7|조건 처리는 #YY() 규격사용                   |      #xor   (A, B) > C  (조건 전용)|
|8|사용자 금칙 기호 | !@#$%^&*()-+=\|.,[]{}<>?/:;'"|

- 모델 예시 
![language-table](./png/spec.dio.png)
```ex)

      [sys]mySystem = {
          [task]T = {
              A = {O1 ~ I1};
              B = {O2 ~ I2};
              C = {O3 ~ I3};
              D = {O4 ~ I4};
          }
          [flow of T]F = {R1 > R2;
              R1 =  {A > B};
              R2 =  {C > D};
          }
      }
```


### 2 모델링 작성 규칙


- 2.1  Segment Edges 정의 
      - brace '{}' 내부의 child 구분자는 `;` or line break
      - child segment 하부의 segment 는 위 목록에 기술하지 않음.  해당 child segment 정의에서 기술
    - indent 후 하부에 child segment 에 대한 세부 정의 및 속성 정의
    - indent 는 스페이스만 사용할 것을 권장.  TAB 혼재될 경우, 하나의 TAB 은 항상 space 4개로 해석한다.
  - (Child) Segment Edges (Real Segment는 Root에만 존재 가능)
    - SegmentName = {children segments edge List} (edge없을시 ';' 로 구분하여 행위만 나열)
    - CallSegment : <타시스템 이름>.<타시스템 대상 root flow segment>
    
  </p>
 - 2.2 Properties
    - [bracket 내부에 속성 명 정의] = {속성 세부 사항}
    - 현재 정의된 속성명
      - accxyz : XYZ 는 S/R/E 의 조합.  e.g accs, accsre, accRE, ... 등 총 7가지
      - arrg : Going 인과 : 행위 Start Edge / 행위 Reset Edge 혼용  [arrG] 생략가능
      - arrh : Homing 인과(안전복귀인과)  : 행위 Start Edge 전용
        - Homing시에 Segment의 기본 Start Point로 동시 호출
        - 예시 : [arrH]R1 = { Z > X };  // [arrH] 정의시 Z 다음 X 순서 처리하면 정의 이유는 주로 행위 간섭 안전관련

  </p>

 - 2.3 인과 정의
      - `>` or `<` 인과 순서
      - `|>` or `<|` or `<||>` reset 인과
      - `,` 는 and(&) 관계.  인과 방향성보다 우선순위가 높음
        - e.g `Sys1.A, Sys2.A > X;`
      - 인과 정의의 마지막은 semicolon(`';'`) 로 끝나야 한다.
      
  </p>

 - 2.4 주석
   -   `//` 로 시작하는 line comment or `/*` 와 `*/` 의 block comment
