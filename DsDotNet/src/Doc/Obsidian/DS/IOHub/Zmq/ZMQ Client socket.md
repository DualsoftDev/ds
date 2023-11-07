1. 최초 서버에 등록 "REGISTER"
2. 등록 이후,
	1. [[Server]]로부터 받는 [[Notification]] message handling
		1. message id 가 -1.
	2. 자신이 보낸 reqeust 에 대한 응답 message
		1. 자신이 보낸 reqId 와 동일한지 확인
		2. 음의 값이면 [[Server]]로부터의 [[Notification]]
3. Note
		1. Client 는 자신이 보낸 message 에 대해 비동기적으로 message 를 수신해야 한다.
			1. e.g 스레드로 socket 을 계속 검사하면서,  [[Notification]] 은 바로 처리하고, 양의 수신 id 를 갖는 메시지는 `Queue` 구조에 enque 했다가 request 보낸 후, deque 해서 reply 를 처리
		2. Server 에서 [[Notification]]이 언제 발생해서 날아올 지 모르기 떄문이다.
		3. [[Notification]]과 자신이 보낸 메시지에 대한 응답을 구분하기 위해서는 전송 message id 와 수신 message id 를 비교하여야 한다.  (수신 message id 가 -1 이면 [[Notification]])

		