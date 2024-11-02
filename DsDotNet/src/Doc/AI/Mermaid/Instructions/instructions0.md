공정 순서를 mermaid flow diagram 를 이용해서 그린다.

- 공정 모델링 요소로는 device 와 work 가 있으며 이들 간 연결로 connector 를 사용한다.
- device
	- 실물 device 에 해당하는 개념으로 API 가 제공된다.
	- 가령 double acutuating cylinder 는 ADV 와 RET 두개의 API 가 제공된다.
	- robot 은 작업 차수에 따라서 구분한다.   3가지 행위를 갖는 robot 이라면 ACTION1, ACTION2, ACTION3 의 API 를 갖는다.
	- 각각의 API 는 reset 개념이 존재해서 cylinder 의 경우, ADV 와 RET 은 상호 reset 개념이다.  즉 ADV 가 수행되면 RET 이 해제되고, RET 이 수행되면 ADV 가 꺼진다.
	- device 는 원형으로 표현하고, 내부 텍스트는 device 명 + '.' + API 명으로 구성된다.  e.g Cylinder1.ADV
	- 한번 실행되면 reset 에 해당하는 device API 가 수행되기 전까지는 다시 실행할 수 없다.
- work (공정)
	- device API 호출 순서를 정의해서 하나의 독립된 공정을 만든다.
	- 내부에는 device API 들의 호출 순서가 순차적으로 정의된다.   화살표를 이용해서 순서를 나타낸다.
	- 내부에 다른 work(사각형, subgroup) 은 올 수 없다.  device API 의 순서 나열로만 표현된다.
	- incoming edge 가 없는 device API 는 첫 시작 API 를 나타낸다.
	- 준비, 진행중, 완료, 리셋중 4개의 상태를 갖는다.
		- 준비 상태에서만 공정을 시작할 수 있으며, 
		- 진행 중일 때에는 시작 신호를 받아도 재시작할 수 없다.
		- 진행이 완료되면 완료 상태가 된다.   진행 완료는 내부의 device API 호출이 모두 끝난 시점이다.
		- 완료 상태일 때에만 리셋명령을 받아서 리셋중 상태가 된다.
	- work 는 mermaid 의 subgroup 을 이용한다.
- connector(연결)
	- 허용 되는 연결
		- 하나의 work 내에서 device API 끼리의 연결
		- work 와 work 간의 연결
		- 어떤 work 에도 포함되지 않는 device API 와 work 간 연결
	- 허용되지 않는 연결
		- work 에 포함된 device API 와 그 work 에 포함되지 않은 device API 간의 연결
	- 기본 연결은 순서를 의미한다.
	- 상호 리셋은 화살표의 text 에 R 로 표기한다.
	- 동시에 실행해야 하는 device API 들의 연결은 화살표 없는 직선 연결로 가능


