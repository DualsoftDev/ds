- [zmq](https://zeromq.org/)

### DotNet 권장
- Sample 참고
### Linux g++ build
```
# apt-get update
# apt-get install build-essential gdb
# apt install libzmq3-dev
# g++ CppClient.cpp -o cppClient -lzmq
```
### VC++ build
- vcpkg 를 이용
- cppzmq 설치