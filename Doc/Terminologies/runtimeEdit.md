# Run 중 Edit

- Child segment 에 대해 추가 / 삭제가 존재하는 parent segment
  - 해당 parent segment 가 정적 상태(Ready or Finish) 일 때는 바로 교체 적용
  - 해당 parent segment 가 정적 상태일 때까지 기다린 후, 정적 상태에서 교체 적용
    (즉 현재의 going/homing 과정은 일단 끝 마친 후.. 변경 적용)
  - Topdown, 재귀적으로 적용됨

- top level 에서 추가 / 삭제되는 Root segment 도 해당 root segment 의 정적/동적 상태를 따져 동일 규칙 적용



- 수정으로 인한 변경없이 동일하게 유지되는 segment 는
관련된 Relay / Tag / Port 값을 그대로 유지

- 새로 추가되는 segment 의 Relay / Tag / Port 등은 신규 생성 값을 가짐

- 변경에 대한 고려는 따로 없음 : 삭제 + 추가로 해석
  - A 를 B 로 변경하였다면, A 가 삭제되고 B 가 추가된 것고 동일한 효과로 적용함.
  - 따라서 원래 A 가 가지고 있던 relay 의 값이 B 로 이전되는 것이 아니라, 
    A 는 소멸되고, 새로 생성된 B 에 relay 도 새로운 값을 가짐
