# Flag

## 용법

- [sre.pptx](./ppt/SRE.pptx) 참고

- Port (SP, RP, EP)
  - Call/Real Segment 의 S/R/E Port 에 해당
  - SP  : 자신을 시작 시키려는 TAG들 값을 'or' 연산 한다.
  - RP  : 자신을 리셋 시키려는 TAG들 값을 'or' 연산 한다.
  - EP  : 자신이 ON/OFF 되면 자신 결과 TAG들을 ON/OFF

- Tag (ST, RT, ET)
  - Port 와 Relay 간의 I/F 매개체 (시스템에 따라 다양한 값의 형태를 가질 수 있음)
  - ST  : 시작 Relay의 On/Off 상태에 따라 ST의 상태가 결정된다.
  - RT  : 리셋 Relay의 On/Off 상태에 따라 RT의 상태가 결정된다.
  - ET  : 자신이 ON/OFF 되면 자신 결과 TAG들을 ON/OFF
  
- Relay (SR, RR, ER)
  - 자식 행위를 인과대로 처리하기위한 부모 Segment의 start, reset, end bit memory flag 기능
  - Source, Target Segment에 각 자식들을 명령 및 관찰 가능한 시작, 리셋, 끝에 해당하는 릴레이를 부여
  - Start에 의한 인과처리를 위한 Going  Relay (SR, RR, ER)
  - Reset에 의한 인과처리를 위한 Homing Relay (SH, RH, EH)
