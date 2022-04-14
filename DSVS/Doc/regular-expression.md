
- multiline 가능함 (begin/end)

  testDef:
    patterns:
      - comment: macro # [macro=A]={}
      - name: punctuation.definition.xxx.ds
        begin: (\[)
        beginCaptures:
          '1': {name: punctuation.definition.list.begin.sehn}
        end: (\])
        endCaptures:
          '1': {name: punctuation.definition.list.end.sehn}
        patterns:
          - name: keyword.control.ds
            match: (xxx)
