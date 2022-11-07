namespace UnitTest.Engine

[<AutoOpen>]
module EntryPointModule =
    //빌드 경고 해제용도 :  warning FS0988: 프로그램의 주 모듈이 비어 있습니다. 프로그램 실행 시 아무 작업도 수행되지 않습니다.
    [<EntryPoint>]        
    let main argv = 
    
        pptTestModule.checkAll()
        0


        