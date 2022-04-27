![A](release.dio.png)


# Todo list

## Engine
- cpu/plc, hmi configuration, 다중 CPU 처리
- runtime
    * origin check
    - [x] 인과 처리 (기본 relay)
    * logging
    * run 중 edit
- simulator
- 엘리베이터 키트

## Compiler
- [x] language parser
- macro expansion
- hmi generator
- ~~PLC cross compiler~~

## I/O server
- PAIX, PLC, OPC, ... wrapper

## HMI, 모니터
- Web based
- ~~unity3D~~


## Editor / IDE : local and/or web based
- GUI base
- text based
- vscode extension
    - [x] syntax highlighting/checking
    - [ ] code completion, snippet..
    - [x] readonly view

## REST server
- hmi, monitor 등이 local 망에서 접속 시, cloud 에 접속한 것과 동일 API 제공해야 함
- engine 에서 발생하는 logging data 를 cloud 에 전송

## Cloud



## 참고 일정 : ikshin
1. ds program loader(기본 parser - macro쪽 제외) ~ 20220429
2. origin check ~ 20220506
3. rest api server(cloud 연결 없이 pc들 끼리 통신) ~ 20220513
4. 엘리베이터 키트 예제 simulation ~ 20220520
5. 엘리베이터 키트 HW 컨트롤 ~ 20220527