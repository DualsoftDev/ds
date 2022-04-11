- [GitHub - inkle/ink-tmlanguage: TextMate grammar files for Ink. (VS Code, Sublime Text, and Atom)](https://github.com/inkle/ink-tmlanguage)
    - TextMate grammars can only match one line at a time,
    - [GitHub - disco0/YAML-tmLanguage-convert: Process YAML TextMate grammar files into the more compatible JSON format—includes support for variable expansions and watch mode (the default)](https://iboxshare.com/disco0/YAML-tmLanguage-convert)

    ```
    # Before we dive into the meat, a bit of syntax to remember:
    #
    # (?<=): Positive lookbehind, non capturing unless it's within a group
    # (?<!): Negative lookbehind, non capturing unless it's within a group
    # (?=): Positive lookahead, non capturing unless it's within a group
    # (?!): Negative lookahead, non capturing unless it's within a group
    # (?:): Non capturing group
    # (?x): Ignore subsequent spaces (useful for multiline-formatted regex)
    #
    # >-: YAML multi-line string, turning line breaks into spaces
    ```


- CONVERT : YAML -> JSON : 잘 동작함..  TextMate Languages extension
[How to convert a TextMate Grammar (XML flavor) to either YAML or JSON flavor](https://stackoverflow.com/questions/61283282/how-to-convert-a-textmate-grammar-xml-flavor-to-either-yaml-or-json-flavor)   

- [Language Grammars — TextMate 1.x Manual](https://macromates.com/manual/en/language_grammars)
    - The format is the property list format and at the root level there are five key/value pairs:


- [Regular Expressions — TextMate 1.x Manual](https://macromates.com/manual/en/regular_expressions)
        ```
        (?:subexp)         not captured group
        ```

- Sample : [ink-tmlanguage/Ink.YAML-tmLanguage at master · inkle/ink-tmlanguage](https://github.com/inkle/ink-tmlanguage/blob/master/grammars/Ink.YAML-tmLanguage)        

- Microsoft 샘플 : [GitHub - microsoft/TypeScript-TmLanguage: TextMate grammar files for TypeScript for VS Code, Sublime Text, and Atom.](https://github.com/microsoft/TypeScript-TmLanguage)


