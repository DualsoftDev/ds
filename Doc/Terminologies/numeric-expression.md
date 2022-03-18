#### Expression

###### - Numeric segments
- $\mathbb{R}$
    - 온도, 습도, 압력 등의 sensor 감지
    - No incoming, No outgoing
    - 항시 sensor 값을 읽어 value update
- $\mathbb{N}$
    - counter, incrementor, decrementor
    - No outgoing
    - set/reset 은 존재할 수 있음
- $\mathbb{B}$
    - 기존에 논하던 일반 sensor segment.
    - incoming/outgoing 존재

###### - Term segments
- expression 에 Numeric segments 에 연산 결합
- 재사용 편의 용도
- e.g `Cond := Temp.Value + Pressure.Value/2`

###### - Compare segments
- expression 에 equation (=, !=, <, >, ..) 적용
- 최종 값은 $\mathbb{B}$ type
- e.g `Ok := Cond > 3.14 || Cond < 2.0`
- Outgoing 존재


