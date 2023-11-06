Tag 값에 변경 발생시, 변경을 유발한 개체를 제외한 나머지 개체에게 변경 내용을 공지한다.
- [[Server]]를 통한 값 변경시
	- 모든 client 에게 변경 내용이 [[ZMQ client build]] 을 통해 공지된다.
- [[Client]] 에서 값 변경시
	- 서버에서 file write 이후, [[Server]]의 Subject 를 통해서 server application에 공지
	- 나머지 모든 client 에게 [[ZMQ client build]]을 통한 공지
