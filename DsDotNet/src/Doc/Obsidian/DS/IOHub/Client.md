- 센서 값 변경 감지: 담당 [[PLC, Field IO]] 종류에 따라서 polling 수행
	- Scan loop 을 돌면서 PLC 로부터 변경 사항이 있는지 계속 검사해서 변경되면 [[server]] 로 전송
- Actuator write: 서버로부터 받은 [[Notification]] 값을 [[PLC, Field IO]] 로 write


- [ ] Major programming language (C++, python, javascript) 에 대한 client class 작성