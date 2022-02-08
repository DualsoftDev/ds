# Model

- [DsSystem](DsSystem.md) 들을 포함하는 container $\approx$ C# 전체 program
- C# proram 이 여러 class 들로 구성 되듯이, model 은 여러 system 으로 구성된다.
- main 함수가 존재하는 class -> active system
- main 함수 내에서 다른 class 의 member 함수 호출 관계를 정의하듯이 active system 은 다른 passive system 의 기능 단위를 조합해서 인과 순서를 정의할 수 있다.