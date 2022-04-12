## 인과 기반 시스템 모델링

#### 기존 방식 (제어모델링)
<!-- 
$
    \fbox{행위} ~~\color{red}\overrightarrow{인과}~~ \color{black}\fbox{조건} ~~\overrightarrow{제어}~~ 
    \fbox{행위} ~~\color{red}\overrightarrow{인과}~~ \color{black}\fbox{조건} ~~\overrightarrow{제어}~~ 
    \fbox{행위}$
 -->
$    \fbox{Actuator} ~~\color{red}\overrightarrow{인과}~~ \color{black}\fbox{Sensor} ~~\overrightarrow{제어}~~ 
    \fbox{Actuator} ~~\color{red}\overrightarrow{인과}~~ \color{black}\fbox{Sensor} ~~\overrightarrow{제어}~~ 
    \fbox{Actuator}$


$\color{red}\overrightarrow{인과} $ : 기존 제어 프로그램 작성에서의 missing link


PLC 를 비롯한 기존 제어 프로그램은 조건에 따른 제어 행위만을 기술하고,
행위의 인과, 즉 해당 행위의 결과로 발생하는 다른 조건의 변경에 대한 지식은 제어 프로그램에 표현하지
못하고 제어 프로그램 작성자의 머릿속에만 존재한다.

이는 전체 시스템을 제어 관점에서만 바라 보는 방식이며, 시스템을 제어하는 것 자체는 
문제가 없으나, 전체 시스템에 대한 동작 특성을 이해하고, 제어 이외의 시뮬레이션, 트윈 등으로 
기능을 확장하는 데 있어서는 인과를 해석할 수 없어서 직접 사용할 수 없는 모델이다.

#### 제안 방식 (시스템 모델링)
<!-- 
- 인과 모델링 (인과 사전 정의)
    $\fbox{행위} ~~\overrightarrow{인과}~~ \color{black}\fbox{조건}$ 
- 행위 모델링
    $\fbox{행위} \rightarrow \fbox{행위}$ -->



- 인과 모델링 (인과 사전 정의)
    $\fbox{행위} = \fbox{Actuator} ~~\overrightarrow{인과}~~ \color{black}\fbox{Sensor}$ 
- 행위 모델링
    $\fbox{행위} \rightarrow \fbox{행위}$


#### 청구항
- 시스템 제어에 있어서 부가적인 정보인 인과를 추가적으로 모델링한 시스템 모델링으로부터 제어 모델을 생성
- 동일 시스템 모델로부터 시뮬레이션, 트윈 등의 기능 확장


목적 : 시스템 모델링기반 제어
효과 : 제어 이외의 부수 효과
구성 : 
