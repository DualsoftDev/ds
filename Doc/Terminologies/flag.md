# Flag

## 용법

- [sre.pptx](./ppt/SRE.pptx) 참고

- Port
  - Call/Real Segment 의 S/R/E Port 에 해당
  - SP  : 자신을 시작 시키려는 TAG들 값을 'or' 연산 한다.
  - SR  : 자신을 시작 시키려는 TAG들 값을 'or' 연산 한다.
  - SE  : 자신이 ON/OFF 되면 참조 segment 의 모든 TAG들을 ON/OFF

- Tag
  - Port 와 Relay 간의 I/F 매개체 (시스템에 따라 다양한 값의 형태를 가질 수 있음)
- Relay
  - 자식 행위를 인과대로 처리하기위한 부모 Segment의 SR, RR, ER bit memory flag 기능
  - Source, Target Segment에 각 자식들을 명령 및 관찰 가능한 시작, 리셋, 끝에 해당하는 릴레이를 부여
