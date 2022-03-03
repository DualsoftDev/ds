# Relay

## 용법

- Port
  - 피참조/Real Segment 의 S/R/E relay 에 해당
  - RealyS Port : 자신이 속한 segment 을 참조하는 참조 segment 에 속한 RelayS 중 하나라도 ON 이면 ON 이고, 모두 OFF 이면 OFF 값을 가짐
    - RelayR Port 도 개념 동일
  - RelayE Port : 자신이 ON/OFF 되면 참조 segment 의 모든 RelayE port 를 ON/OFF

- Tag : Port 를 켜기 위한 여러 조건 중의 하나
- Flag
    H/T relay 에 해당.  bit memory flag 기능
