# DS Extention publish 가이드

## publisher ID & Token
* 현재 vscode extension Marketplace에서 계정 dskim9752@dualsoft.com 이 'dualsoft'를 publisher로 등록했다.

|ID|Token Number|
|:--|:--|
|dualsoft|cdw5qklm7qabvfetdumj6j4mcr2pjv4zxwjkrmxsyzi7md7x3rqa|


## 필요 작업 - vsce login
Node.js 프롬프트에서 vsce 로그인 수행
```
vsce login dualsoft
```
```
https://marketplace.visualstudio.com/manage/publishers/
Personal Access Token for publisher 'dualsoft': [여기에 Token 번호 입력]

cdw5qklm7qabvfetdumj6j4mcr2pjv4zxwjkrmxsyzi7md7x3rqa


The Personal Access Token verification succeeded for the publisher 'dualsoft'.
```


## Publish

처음 배포할때는 반드시 [vsce 로그인](#필요-작업---vsce-login)을 먼저 수행해야 한다.

```
    vsce publish [배포할 버전]

    vsce publish  // 최신 버전 배포
```


## 기타
- vsix file 로부터 설치
    - command pallete 에서 Install from VSIX 검색 > 클릭