# ds extention build 가이드

## 필요 프로그램
- Node.js
- vsce
  
  Node.js 명령 프롬프트에서 vsce 패키지 설치
  ```
  npm install -g vsce
  ```

## ds vscode extension 빌드

1. 
   ```
   cd (~ds\DSVS\ds 경로)
   ```
2. ```
   vsce package [버전]
   ```
   * 프로그램 버전은 기존에 배포된 프로그램보다 상위 버전이어야 한다.
   * 버전은 0.0.x 으로 표기한다.
```
   ex) 0.0.3    >    0.0.?
    vsce package 0.0.4 (o)
    vsce package 0.0.1 (x)
    vsce package 0.0.4-1 (x)

```

3. 배포 

[publish.md](publish.md#ds-extention-publish-가이드) 참조    



