Engine.Parser project file 은 paket 적용을 받지 말아야 한다.

최상위에서 paket install 등을 수행하였을 경우,
1. 아래의 paket restore 가 생성되는 데, 이를 삭제해 주어야 한다.
    <Import Project="..\..\.paket\Paket.Restore.targets" />
2. Engine.Parser\{bin, obj} 폴더를 삭제한 후에 다시 build 한다.
3. 1, 2 수행 결과, Dependencies 의 packages 아래 3개의 항목만 존재해야 한다.
    - Antlr4.Runtime.Standard
	- Antlr4BuildTasks
	- System.Runtime.CompilerServices.Unsafe

