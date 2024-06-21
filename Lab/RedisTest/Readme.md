# Redis

## Redis 설치
- Docker 나 WSL 에 정식 버젼을 설치하거나, 아래 windows 용 binary 호환 버젼 설치
- [Windows 용 Redis 설치](https://github.com/microsoftarchive/redis/releases/download/win-3.0.504/Redis-x64-3.0.504.msi)
    - 설치 후, 실행 파일 위치: `C:\Program Files\Redis\redis-server.exe`
    - Redis 서버가 실행되며, 기본 포트 6379에서 대기 상태가 됩니다.
    - 윈도우 서비스에서 Redis 구동 확인

## Redis C++
- vcpkg 를 이용해서 설치
    ```dos
    vcpkg install hiredis
    ```
- build 시 설치된 hiredis 의 include, lib 폴더 project 에 추가 후, bin 으로부터 필요 dll 복사 (hiredis.dll)