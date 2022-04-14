
- 따라 해 볼 만한 vs code 공식 문서
    - [Example language server - vscode-docs](https://vscode-docs.readthedocs.io/en/stable/extensions/example-language-server/)
        - [GitHub - microsoft/vscode-languageserver-node: Language server protocol implementation for VSCode. This allows implementing language services in JS/TS running on node.js](https://github.com/microsoft/vscode-languageserver-node)
        - [vscode-extension-samples/lsp-sample at main · microsoft/vscode-extension-samples](https://github.com/microsoft/vscode-extension-samples/tree/main/lsp-sample)
            - 간단한 language client/server sample

- [Language Server Extension Guide](https://code.visualstudio.com/api/language-extensions/language-server-extension-guide)



- [Extending VSCode: Write Your Own Language Server in VSCode](https://www.youtube.com/watch?v=H0p7tcUuJm0)


ANTLR
    .g4
    ANTLR extension for vs
    cd

- [Create a Text Parser in C# with ANTLR](https://www.youtube.com/watch?v=lc9JlXyBG4E) : 뭔소린지??
- [Making a Programming Language with C# and ANTLR4](https://www.youtube.com/watch?v=bfiAvWZWnDA)


- [Creating a Lexer &amp; Parser](https://www.youtube.com/watch?v=70NVv0nVLlE)        

- [Building a Parser from scratch. Lecture [1/18]: Tokenizer | Parser](https://www.youtube.com/watch?v=4m7ubrdbWQU&list=PLGNbPb3dQJ_5FTPfFIg28UxuMpu7k0eT4)


[GitHub - microsoft/TypeScript-TmLanguage: TextMate grammar files for TypeScript for VS Code, Sublime Text, and Atom.](https://github.com/microsoft/TypeScript-TmLanguage)
.tmLanguage

- [Create Custom Syntax Highlighting in VS Code | Programming Language | Software Coding Tutorials](https://www.youtube.com/watch?v=5msZv-nKebI)
    - yo code 수행 시, language extension 설치 방법
    - Language server 없이 yo code 만을 이용해서 syntax highlight 까지만 수행하는 방법



As of VSCode 1.15, you *have to use* textmate grammars for syntax highlighting. There's an feature request open that tracks what you are after: https://github.com/Microsoft/vscode/issues/1967


- [Syntax Highlight Guide](https://code.visualstudio.com/api/language-extensions/syntax-highlight-guide)
VS Code's tokenization engine is powered by TextMate grammars. TextMate grammars are a structured collection of regular expressions and are written as a plist (XML) or JSON files. VS Code extensions can contribute grammars through the grammars contribution point.

Starting with release 1.43, VS Code also allows extensions to provide tokenization through a Semantic Token Provider. 




#### Sample
[GitHub - kaby76/uni-vscode: From an Antlr4 grammar to a VSCode extension in less than a minute.](https://github.com/kaby76/uni-vscode)


- Spell checker : client + server
    - [GitHub - streetsidesoftware/vscode-spell-checker: A simple source code spell checker for code](https://github.com/streetsidesoftware/vscode-spell-checker)