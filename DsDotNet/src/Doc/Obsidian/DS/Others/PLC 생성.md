
# Ladder 생성 관련 처리
- [ ] `| DuAssign(condition, expr, target)` case 처리
	- `//assert(condition.IsNone)` 코멘트 풀고 동작할 수 있도록 수정해야 함.
	```
    member x.``Arithmetic test1`` () =  // XGK
        let code = "bool b0 = !(2.1 == 6.1);";


	// TODO : 여기 수정 @ LsPLCExport.Statement.fs
	DuAssign(Some fake1OnExpression, visitTop exp, var)

	```
	