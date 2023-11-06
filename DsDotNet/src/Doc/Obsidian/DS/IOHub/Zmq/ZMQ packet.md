- [[ZMQ]]를 이용한 통신은 Dealer / Router 패턴을 따르고, 전송 메시지는 multipart message 로 구현된다.
- DotNet 을 이용한 구현은 제공된 Client 를 이용하면 되고, DotNet 이외의 언어 구현에서는 다음을 참고하여 구현한다.

### [[Client]] -> [[Server]] 전송 packet 의 multipart 구성
1. Command
	1. 문자열 명령어. 
	2. 서버 등록 및 해제: "REGISTER", "UNREGISTER" 
	3. 읽기(`r`)/쓰기(`w`): 
		1. data type 설정자
			1. `x`: bit, boolean
			2. `b`: byte
			3. `w`: word, uint16
			4. `dw`: double word, uint32
			5. `lw`: long word, uint64
		2. 읽기 명령은 읽을 offset array (=`offsets`) 를, 쓰기 명령은 offset array (=`offsets`) 와 value array (=`values`) 를 제공한다.
		3. `offsets` 은 int type array 를 byte array 로 표현
		4. `values`:  data type 에 따라 다음과 같이 변환해서 제공
			1. `x` 나 `b` 이면 byte array 를
			2. `w` 이면 `uint16` array 를 byte array 로 변환한 array 를
			3. `dw` 이면 `uint32` array 를 byte array 로 변환한 array 를
			3. `lw` 이면 `uint64` array 를 byte array 로 변환한 array 를			
2. RequestId
	1. 서버로 메시지를 보낼 때마다 1 씩 증가하는 uniq 한 `int32` type request id 를 생성해서 byte array 로 변환.   1부터 시작해서 양수만 사용한다.
3. 예제
	1. 등록 메시지 packet
		1. "REGISTER" 문자열에 해당하는 bytes
		2. 고유 req id 의 int32 에 해당하는 bytes
		3. 위의 두 part 들을 multipart message 로 server 에 전송하고 응답 메시지를 확인한다.
	2. Read
		1. 읽을 tag 종류에 따른 명령어 선정 후 이를 bytes 로 
			1. e.g `double word` type 을 읽으려 한다면 `rdw`
		2. 고유 req id 의 int32 에 해당하는 bytes
		3. 읽을 offset 값을 int32 array 에 저장한 후, 이에 해당하는 bytes
		4. 위의 part 들을 multipart message 로 server 에 전송하고 응답 메시지를 확인한다.
	3. Write
		1. 쓸 tag 종류에 따른 명령어 선정 후 이를 bytes 로 
			1. e.g `long word` type 을 읽으려 한다면 `wlw`
		2. 고유 req id 의 int32 에 해당하는 bytes
		3. 쓸 offset 값을 int32 array 에 저장한 후, 이에 해당하는 bytes
		4. 쓸 values 값을 쓸 type 에 맞게 array 에 저장한 후, 이에 해당하는 bytes
		5. 위의 part 들을 multipart message 로 server 에 전송하고 응답 메시지를 확인한다.
### [[Server]] -> [[Client]] 전송 메시지의 multipart 구성
1. Client 에서 보낸 req 의 응답인 경우
	1. Response ID: 보낸 req id 를 갖는 메시지에 대한 응답일 경우, 동일 id 값을 가진다.
	2. OK/NG
		1. 처리 결과의 성공 여부를 "OK", "NG" 로 구분하여 반환
	3. Details
		1. read request 인 경우
			1. request 로 보낸 `offsets` 에 해당하는 `values` 값이 해당 type 의 array 에 해당하는 `bytes` 값이 들어 있다.
			2. 추후 multipart 는 확인을 위한 Name, ContentsBitLength, Offsets 에 해당하는 `bytes`의 값들이 들어 있다.
		2. write request 인 경우는 추후 packet 무시
2. Client 에서 보낸 req 가 아닌, 서버에서 임의로 보낸 공지인 경우
	1. Response ID: 음의 값을 id 로 가진다.
	2. 공지 명령: Notify 인 경우 "NOTIFY" 
	3. values
	4. Name
	5. ContentBitLength
	6. Offsets
