- Independances
	- [[PLC, Field IO]] : <font style="color:yellow">임의의 third party vendor 의 PLC, Field IO 에 대해 대응 가능해야 한다.</font>
	- OS (Windows/Linux/Mac/..)
	- Process (Client 구동 process 와 Server 구동 process 는 독립)
	- Network (clients 및 server 는 서로 다른 ip 상에서 구동 가능)
	- Language (C#/F#/C++/...)
	- 1 [[Server]], n [[client]]s (n >= 0)
	- ⚠️ ⛔ CPU architecture: x64/x86, ARM, ..
- Locking mechanism
	- 다중 [[Client]] 가 write request 시, 순서에 따른 lock mechanism 제공
- Persistency
	- <font style="color:yellow">File 에 write 되어 system crash 에 대응해야 한다.</font> 
- Efficiency
	- 변경 tag 감지를 위한 file/memory scan 허용 안 함 (No polling)
		- Push 기반 변경 [[Notification]]
		- 변경 주체 (client or server) 가 변경한 내용은, 변경 주체를 제외한 나머지 주체에게 [[Notification]]으로 제공되어야 한다.
	- Server 생성 process 는 socket을 통하지 않고 직접 read/write


---
### 참고 사항

- 📝 CPU architecture
	- Client 와 Server 가 동일 CPU architecture 이면 문제 없음
	- 서로 다를 경우, [[Endian]]이 서로 달라서 bit, byte 의 data type 만 지원.
		- ⛔ word, double word, long word 는 지원 불가
	- c.f.
		- 현존 LS PLC 는 little endian
		- 추후 ARM 기반 linux PLC 는 big endian

