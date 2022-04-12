- extension/dsvs 폴더 아래에 extension 관련 소스 코드 생성하기
```
    $ choco install nodejs
    $ choco install yarn
    $ npm install -g yo generator-code
    $ npm install typescript
        :: tsc (typescript compiler) 설치

    $ cd <my-repository>
    $ yo code
        ...
        ? What type of extension do you want to create? New Extension (TypeScript)
        ? What's the name of your extension? DSVSE
        ? What's the identifier of your extension? dsvse
        ? What's the description of your extension? VS extension for DS
        ? Initialize a git repository? Yes
        ? Bundle the source code with webpack? No
        ? Which package manager to use? yarn    
        ...
    $ cd dsvse
    $ code dsvs
```

- language 추가
    - yo code 실행 시, type 을 `New Language Support` 로 선택한다.
        ```
        ? What type of extension do you want to create? New Language Support
        Enter the URL (http, https) or the file path of the tmLanguage grammar or press ENTER to start with a new grammar.
        ? URL or file to import, or none for new:
        ? What's the name of your extension? ds
        ? What's the identifier of your extension? ds
        ? What's the description of your extension? DS language
        Enter the id of the language. The id is an identifier and is single, lower-case name such as 'php', 'javascript'
        ? Language id: ds
        Enter the name of the language. The name will be shown in the VS Code editor mode selector.
        ? Language name: DS Language
        Enter the file extensions of the language. Use commas to separate multiple entries (e.g. .ruby, .rb)
        ? File extensions: .ds
        Enter the root scope name of the grammar (e.g. source.ruby)
        ? Scope names: source.ds
        ? Initialize a git repository? Yes
        ```        
    - `ds` folder 생성됨을 확인

- 실행 및 debug
    - vscode 내에서 debug 실행
    - 새로운 vscode 창이 열리는데, 여기서 command pallette 실행 (Shift Ctrol P)
        - Hello world
    - cursor 위치 올려 놓고, pallete 에서 `Inspect Editor Tokens and Scope`
    - RELOAD : 실행 중인 vscode 에서 `CTRL + R`
    

