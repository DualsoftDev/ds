- Yeoman generator
    $ yo code

- [VSCode Extension 만들기 - 1](https://wearee .tistory.com/88)
    - 시작하기

    리액트의 CRA처럼 익스텐션을 만드는 키트 같은 게 존재합니다.
    터미널에 아래 명령어를 입력해주세요.

```
    $ npm install -g yo generator-code
    $ yo code
```

- [VSCode Extension 만들기](https://gwanduke.tistory.com/entry/VSCode-Extension-%EB%A7%8C%EB%93%A4%EA%B8%B0)


- [Vscode Extension (플러그인) 만들기_1](https://kdinner.tistory.com/6?category=308458)

- [Domain-Specific Languages in Theia and VS Code](https://www.typefox.io/blog/domain-specific-languages-in-theia-and-vs-code)
    Stage 3: Add a Language Server With Xtext

    This article shows how to build a VS Code language extension for your DSL. We will use:

    - VS Code or the fully open-source VS Codium as the base platform and IDE framework
    - Eclipse Xtext to define the textual DSL and generate a Language Server (LS) for it,
    - Eclipse Sprotty to visualize the DSL in diagrams, and
    - the Eclipse Layout Kernel for auto-layouting these diagrams.    

- [GitHub - eclipse/sprotty: A diagramming framework for the web](https://github.com/eclipse/sprotty)    
- [GitHub - TypeFox/vscode-xtext-sprotty-example: A VS Code extension for a DSL implemented in Xtext with Sprotty diagrams](https://github.com/TypeFox/vscode-xtext-sprotty-example) : not working


[microsoft vscode-extension-samples](https://github.com/microsoft/vscode-extension-samples)

## 동영상
- [Beyond LSP: Getting Your Language into Theia and VS Code](https://www.youtube.com/watch?v=ESRk7NmCDFA)
    - using Xtext to generate LS
    - sprotty 를 이용해서 diagram 그리기 포함
    - [GitHub - TypeFox/vscode-xtext-sprotty-example: A VS Code extension for a DSL implemented in Xtext with Sprotty diagrams](https://github.com/TypeFox/vscode-xtext-sprotty-example)
    - [https://www.lowcomote.eu/data/workshop1220/Text-first_DSLs_in_the_Cloud.pdf](https://www.lowcomote.eu/data/workshop1220/Text-first_DSLs_in_the_Cloud.pdf)
    

- [How To Create And Deploy A VSCode Extension](https://www.youtube.com/watch?v=q5V4T3o3CXE)

    - publishing (18:51)
    ```
        $ npm install -g vsce
        $ cd myExtension
        $ vsce package
        $ vsce publish
    ```
- [Creating A Simple VSCode Extension](https://www.youtube.com/watch?v=srwsnNhiqv8)

- [Creating a VS Code Extension - Crash Course (Developing Visual Studio Code Extensions)](https://www.youtube.com/watch?v=xgkDVUL0MxM)