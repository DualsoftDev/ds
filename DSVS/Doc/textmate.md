- [TextMate 1.x Manual](https://macromates.com/manual/en/)

- [Draft: TextMate2 Manual](https://macromates.com/textmate/manual/)

- [Language Grammars — TextMate 1.x Manual](https://macromates.com/manual/en/language_grammars)

- [Regular Expressions — TextMate 1.x Manual](https://macromates.com/manual/en/regular_expressions)

- [Writing a TextMate Grammar: Some Lessons Learned](https://www.apeth.com/nonblog/stories/textmatebundle.html)


- [vscode-textmate](https://www.npmjs.com/package/vscode-textmate)
    `npm install vscode-textmate`

- foldingStartMarker
    - [Code Folding · Cloud9 SDK](https://cloud9-sdk.readme.io/docs/code-folding)
    ```
    this.foldingStartMarker = /(\{|\[)[^\}\]]*$|^\s*(\/\*)/;
    this.foldingStopMarker = /^[^\[\{]*(\}|\])|^[\s\*]*(\*\/)/;
    ```
    These regular expressions identify various symbols {, [, // to pay attention to. getFoldWidgetRange matches on these regular expressions, and when found, returns the range of relevant folding points. For more information on the Range object, see the Ace API documentation.    


## tmlanguage sample
- https://nest.pijul.com/nicoty/sprak-language-extension:main/NHRV5JP3IOPM2.WAKAA
- https://gitcode.net/mirrors/qjebbs/vscode-plantuml/-/blob/v2.14.0/syntaxes/plantuml.yaml-tmLanguage
- https://git.wfxlabs.com/sen/tm-lang/src/branch/master/sehn.tmLanguage.yaml
- https://gitee.com/haimadongli001/vue-lua-syntax-highlight/blob/master/vue.YAML-tmLanguage


# regular expression
- [Regular Expressions Quick Start](https://www.regular-expressions.info/quickstart.html)
   (?i) - ignore case
   (?=subexp) - look-ahead : https://www.regular-expressions.info/lookaround.html
        match 는 체크하고 값을 return 하나, match 후 stream 은 그대로..