- [[Server]]와 [[Client]]를 모두 포함한다.
- runtime engine 에서는 [[server]] 를 통해서만 값을 read/write 한다.
- [[Server]]와 [[PLC, Field IO]]  의 sync 는 [[Client]]에서 수행한다.




- [ ] Runtime 과 server 연동 🔺
	- [ ] Counter / Timer 에 대한 persistency ⏫
		- Vertex 구조에 내재된 임의의 이름으로 생성되는 timer / counter 를 Vertex 의 FQDN 기준으로 작성해야 함.
		- timer / counter struct 의 내부 변수들에 대한 persistency 제공
	- [ ] PC target 변수들에 대한 persistency



