// grun expr toplevels < g4test/expr/expr.expr
// https://github.com/antlr/grammars-v4/blob/master/arithmetic/arithmetic.g4
grammar expr;

WS: [ \t\r\n]+ -> skip;
BLOCK_COMMENT : '/*' (BLOCK_COMMENT|.)*? '*/' -> channel(HIDDEN) ;
LINE_COMMENT  : '//' .*? ('\n'|EOF) -> channel(HIDDEN) ;

comment: BLOCK_COMMENT | LINE_COMMENT;
identifier : IDENTIFIER;
    tag: TAG;
        TAG: '%' IDENTIFIER;
    storage: '$' storageName;
    functionName: identifier | binaryOperator;

terminal: storage | tag | literal;
    literal:
        /* -  */   literalSingle
        /* .  */ | literalDouble         // 'double' 이름 그대로 사용 불가 : symbol double conflicts with generated code in target language or runtime
        /* y  */ | literalSbyte
        /* uy */ | literalByte
        /* s  */ | literalInt16
        /* us */ | literalUint16
        /* -  */ | literalInt32
        /* u  */ | literalUint32
        /* L  */ | literalInt64
        /* UL */ | literalUint64
        /*    */ | literalChar
        /*    */ | literalString
        /*    */ | literalBool
    ;
        literalSingle :SINGLE;
        literalDouble :DOUBLE;
        literalSbyte  :SBYTE;
        literalByte   :BYTE;
        literalInt16  :INT16;
        literalUint16 :UINT16;
        literalInt32  :INT32;
        literalUint32 :UINT32;
        literalInt64  :INT64;
        literalUint64 :UINT64;
        literalChar   :CHAR;
        literalString :STRING;
        literalBool   :'true' | 'false';

toplevels: toplevel (';' toplevel)* (';')?;
    toplevel: expr|statement;

statement: assign | varDecl | timerDecl | counterDecl;
    assign: '$' storageName ':=' expr;
    varDecl:   type storageName ('=' expr)? ;//';';
        storageName: IDENTIFIER;
    type:
        'int8' | 'sbyte'
        | 'uint8' | 'byte'
        | 'int16' | 'short' | 'word'
        | 'uint16'| 'ushort'
        | 'int32' | 'int'   | 'dword'
        | 'uint32'| 'uint'
        | 'int64' | 'long'
        | 'uint64'| 'ulong'
        | 'double' | 'float64'
        | 'single' | 'float32' | 'float'
        | 'char'
        | 'string'
        | 'bool' | 'boolean'
    ;
    // ton mytimer = ton(rungInCondition, 1000ms);
    timerDecl: timerType timerName '=' timerType '(' arguments? ')';
        timerType: 'ton' | 'tof' | 'rto';
        timerName: IDENTIFIER;

    // ton mycounter = ton(rungInCondition, 1000ms);
    counterDecl: counterType counterName '=' counterType '(' arguments? ')';
        counterType: 'ctu' | 'ctd' | 'ctud';
        counterName: IDENTIFIER;

// https://stackoverflow.com/questions/41017948/antlr4-the-following-sets-of-rules-are-mutually-left-recursive
// https://github.com/antlr/antlr4/blob/master/doc/parser-rules.md#alternative-labels
expr: functionName '(' arguments? ')'                 # FunctionCallExpr    // func call like f(), f(x), f(1,2)
    | '(' type ')' expr                               # CastingExpr
    | storage ('[' expr ']')+                         # ArrayReferenceExpr  // array index like $a[i], $a[i][j]
    | unaryOperator expr                              # UnaryExpr           // unary minus, boolean not
    // priority 순서대로 나열되어야 함.  https://learn.microsoft.com/en-us/cpp/c-language/precedence-and-order-of-evaluation?view=msvc-170
    // equality 의 경우, 위 문서가 잘못된 듯 함..
    | expr binaryOperatorMultiplicative expr          #BinaryExprMultiplicative
    |   expr binaryOperatorAdditive expr              #BinaryExprAdditive
    |   expr binaryOperatorBitwiseShift expr          #BinaryExprBitwiseShift
    |   expr binaryOperatorRelational expr            #BinaryExprRelational
    |   expr binaryOperatorBitwiseAnd expr            #BinaryExprBitwiseAnd
    |   expr binaryOperatorBitwiseXor expr            #BinaryExprBitwiseXor
    |   expr binaryOperatorBitwiseOr expr             #BinaryExprBitwiseOr

    |   expr binaryOperatorLogicalAnd expr            #BinaryExprLogicalAnd
    |   expr binaryOperatorLogicalOr expr             #BinaryExprLogicalOr
    |   expr binaryOperatorEquality expr              #BinaryExprEquality

    | terminal                                        #TerminalExpr
    | '(' expr ')'                                    #ParenthesysExpr
    ;

    arguments: exprList;
    exprList : expr (',' expr)* ;   // arg list
    unaryOperator:
            // '-'|
             '!'
            | '~' | '~~~'                   // bitwise negation (C++/F# style)
            ;
    binaryOperatorMultiplicative: '*'|'/'|'%';
    binaryOperatorAdditive:'+'|'-';
    binaryOperatorBitwiseShift: '<<' | '<<<' | '>>' | '>>>';   // bitwise shift
    binaryOperatorRelational:'>' | '>=' | '<' | '<=';
    binaryOperatorEquality:'=' | '!=' | '<>';
    binaryOperatorBitwiseAnd: '&' | '&&&';   // bitwise and   (C++/F# style)
    binaryOperatorBitwiseXor: '^' | '^^^';   // bitwise xor
    binaryOperatorBitwiseOr: '|' | '|||';   // bitwise or

    binaryOperatorLogicalAnd: '&&';     // logical and
    binaryOperatorLogicalOr: '||';     // logical or


    binaryOperator:
          binaryOperatorMultiplicative
        | binaryOperatorAdditive
        | binaryOperatorBitwiseShift
        | binaryOperatorRelational
        | binaryOperatorEquality
        | binaryOperatorBitwiseAnd
        | binaryOperatorBitwiseXor
        | binaryOperatorBitwiseOr
        | binaryOperatorLogicalAnd
        | binaryOperatorLogicalOr
        ;




//INTEGER: SIGN? DIGITS;
IDENTIFIER: VALID_ID_START VALID_ID_CHAR*;
fragment VALID_ID_START: ('a' .. 'z') | ('A' .. 'Z') | '_';
fragment VALID_ID_CHAR: VALID_ID_START | ('0' .. '9');
fragment DIGIT: ('0' .. '9');
fragment DIGITS: DIGIT+;

fragment SCIENTIFIC_NUMBER: SIGN? NUMBER (E SIGN? DIGITS)?;
fragment NUMBER: ((DIGITS)? ('.' DIGITS)) | ( (DIGITS) '.' (DIGITS)?);

    SINGLE: SCIENTIFIC_NUMBER 'f';
    DOUBLE: SCIENTIFIC_NUMBER;
    SBYTE: SIGN? DIGITS 'y';
    BYTE: DIGITS 'uy';
    INT16:SIGN? DIGITS 's';
    UINT16:DIGITS 'us';
    INT32: SIGN? DIGITS;
    UINT32:DIGITS 'u';
    INT64:SIGN? DIGITS 'L';
    UINT64:DIGITS 'UL';

//fragment UNSIGNED_INTEGER: ('0' .. '9')+;

CHAR: QuotedCharLiteral;
    fragment QuotedCharLiteral : '\'' (~('\'' | '\\' | '\r' | '\n') | '\\' ('\'' | '\\'))+ '\'';

fragment E : 'E' | 'e' ;
fragment SIGN : ('+' | '-') ;
STRING: QuotedStringLiteral;
    fragment QuotedStringLiteral : '"' (~('"' | '\\' | '\r' | '\n') | '\\' ('"' | '\\'))* '"';

LPAREN : '(' ;
RPAREN : ')' ;
LBACKET : '[' ;
RBACKET : ']' ;
PLUS : '+' ;
MINUS : '-' ;
TIMES : '*' ;
DIV : '/' ;
GT : '>' ;
LT : '<' ;
EQ : '=' ;
POINT : '.' ;
POW : '^' ;

