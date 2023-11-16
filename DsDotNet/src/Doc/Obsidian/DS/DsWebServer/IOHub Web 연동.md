- Browser 상의 Web client 에서 tcp://localhost:5555 등의 <font style="color:yellow">socket 통신은 지원되지 않는다.</font>
- [[IOHub]] 과 연동하기 위해서는 REST api 를 사용할 수 밖에 없다.

- [x] Server -> Client : tag 변경 공지 수신 ✅ 2023-11-16
- [ ] Client -> Server: tag 변경 요청
- [ ] Notify format 에 대한 고려
	-  메모리 name (e.g "p/o"), content length, offsets, values 로 해결 가능한가?
	- 아니면, tagNames, values 로 변환해서 client 에서 받아야 하는가?


