


ANTLR (ANother Tool for Language Recognition) 

[An Example of ANTLR - programador clic](https://programmerclick.com/article/2015159159/)

### grun
- grun 설치: [antlr4/getting-started.md at master · antlr/antlr4](https://github.com/antlr/antlr4/blob/master/doc/getting-started.md)
    - /usr/local/lib 에 복사해서 동일 환경으로 설정.
- 운용
    - *.ds 파일있는 위치에서
    $ antlr4 ds.g4
    $ javac *.java
    $ echo "A || B, C > D, E || F;" | grun ds causal -gui

[http://courses.missouristate.edu/anthonyclark/333/lectures/06-antlr-vscode.pdf](http://courses.missouristate.edu/anthonyclark/333/lectures/06-antlr-vscode.pdf)

- [Implementing Code-Completion for VS Code with ANTLR](https://neuroning.com/post/implementing-code-completion-for-vscode-with-antlr/)

- LSP : [Overview](https://microsoft.github.io/language-server-protocol/overviews/lsp/overview/)
    - [GitHub - microsoft/vscode-languageserver-node: Language server protocol implementation for VSCode. This allows implementing language services in JS/TS running on node.js](https://github.com/microsoft/vscode-languageserver-node)

- [GitHub - mike-lischke/antlr4-c3: A grammar agnostic code completion engine for ANTLR4 based parsers](https://github.com/mike-lischke/antlr4-c3)
    .g4 files


- [Starting out with ANTLR | My Memory](http://putridparrot.com/blog/starting-out-with-antlr/)    
    - [ANTLR in C# | My Memory](https://putridparrot.com/blog/antlr-in-c/)


[GitHub - ptr1120/Antlr4.CodeGenerator.Tool: ANTLR 4 parser generator command line tool](https://github.com/ptr1120/Antlr4.CodeGenerator.Tool)
```
    | ~/devdoc$ dotnet tool install --global Antlr4CodeGenerator.Tool
    다음 명령을 사용하여 도구를 호출할 수 있습니다. antlr4-tool
    'antlr4codegenerator.tool' 도구('1.2.0' 버전)가 설치되었습니다.
    ~~dotnet~~ antlr4-tool -Dlanguage=CSharp MyGrammar.g4
```

- 모든 programming 언어 .g4 
    - [GitHub - antlr/grammars-v4](https://github.com/antlr/grammars-v4)
## antlr4ts
- [GitHub - tunnelvisionlabs/antlr4ts: Optimized TypeScript target for ANTLR 4](https://github.com/tunnelvisionlabs/antlr4ts)