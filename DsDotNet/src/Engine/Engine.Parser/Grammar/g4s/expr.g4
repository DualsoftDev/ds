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
    storage: '$' (storageName | udtMember);
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

toplevels: toplevel*;
    toplevel: expr|statement;


statement: (
    // semicolon 을 필요로하는 문장
    (   assign | varDecl | timerDecl | counterDecl | copyStatement
        | udtDef | copyStructStatement
        | lambdaDecl | procCallStatement
        | udtDecl ) ';')       // C++ 의 struct 정의는 마지막 brace 닫힘 이후 semicolon 필요로 함.  C# 은 아님.
    // semicolon 을 필요로하지 않는 문장
    | procDecl;

    assign:
        structMemberAssign      # CtxStructMemberAssign
        | normalAssign          # CtxNormalAssign
        | risingAssign          # CtxRisingAssign
        | fallingAssign         # CtxFallingAssign
        ;
    lambdaDecl: type lambdaName '(' argDecls? ')' '=>' lambdaBodyExpr;
        lambdaName: IDENTIFIER;
        argDecls: argDecl (',' argDecl)*;
        argDecl: type argName;
        argName: IDENTIFIER;
        lambdaBodyExpr: expr;
    procDecl: 'void' procName '(' argDecls? ')' '{' procBodies '}';
        procBodies: statement*;
        procName: IDENTIFIER;
    returnStatement: 'return' expr;


    normalAssign: '$' storageName '=' expr;
    structMemberAssign: '$' structStorageName  '=' expr;
        structStorageName: IDENTIFIER (ARRAYDECL)? '.' IDENTIFIER;      // myTon.Q, people[10].name
    risingAssign: 'ppulse' '(' '$' storageName ')' '=' expr;
    fallingAssign: 'npulse' '(' '$' storageName ')' '=' expr;
    varDecl:   type storageName ('=' expr)?;
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
    // ton mytimer = ton(1000ms, rungInCondition);
    timerDecl: timerType storageName '=' expr;  //timerType '(' arguments? ')';
        timerType: 'ton' | 'tof' | 'rto';
        timerName: IDENTIFIER;

    // ctu mycounter = createCTU(1000ms, rungInCondition);
    counterDecl: counterType storageName '=' expr;  //counterType '(' arguments? ')';
        counterType: 'ctu' | 'ctd' | 'ctud' | 'ctr';
        counterName: IDENTIFIER;
    copyStatement: 'copyIf' '(' copyCondition ',' copySource ',' copyTarget ')';
        copyCondition: expr;
        copySource: expr;
        copyTarget: storage | tag;
    copyStructStatement: 'copyStructIf' '(' copyCondition ',' udtInstanceSource ',' udtInstanceTarget ')';
        udtInstanceSource: '$' udtInstance;
        udtInstanceTarget: '$' udtInstance;

    udtDecl: 'struct' udtType '{' varDecl (';' varDecl)* ';' '}';
        udtType: IDENTIFIER;
    udtDef: udtType udtInstance;
        arrayDecl:ARRAYDECL;
        udtInstance: udtVar (arrayDecl)?;
        udtVar: IDENTIFIER;
        udtMember: udtInstance '.' udtVar;
    procCallStatement: funcCall;

// https://stackoverflow.com/questions/41017948/antlr4-the-following-sets-of-rules-are-mutually-left-recursive
// https://github.com/antlr/antlr4/blob/master/doc/parser-rules.md#alternative-labels
expr: funcCall                                        # FunctionCallExpr    // func call like f(), f(x), f(1,2)
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
    | '(' expr ')'                                    #ParenthesisExpr
    ;

    funcCall: functionName '(' arguments? ')';
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
    binaryOperatorEquality:'==' | '!=' | '<>';
    binaryOperatorBitwiseAnd: '&' | '&&&';   // bitwise and   (C++/F# style)
    binaryOperatorBitwiseXor: '^' | '^^^';   // bitwise xor
    binaryOperatorBitwiseOr: '|' | '|||';   // bitwise or

    binaryOperatorLogicalAnd: '&&';     // logical and
    binaryOperatorLogicalOr: '||';     // logical or

    // 참/거짓을 판정할 수있는 boolean 수식
    predicate:
         'true' | 'false'
        | storage | tag    // identifier with boolean...
        |   expr binaryOperatorLogicalAnd expr
        |   expr binaryOperatorLogicalOr expr
        |   expr binaryOperatorEquality expr
        ;


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
fragment VALID_ID_CHAR: VALID_ID_START | ('0' .. '9') | HangulChar;
fragment DIGIT: ('0' .. '9');
fragment DIGITS: DIGIT+;
fragment HangulChar: [\u3131-\u314E\u314F-\u3163|\uAC00-\uD7A3]+;
fragment SPACES: [ \t\r\n]+;

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

ARRAYDECL: LBRACKET SPACES? DIGITS SPACES? RBRACKET;

//fragment UNSIGNED_INTEGER: ('0' .. '9')+;

CHAR: QuotedCharLiteral;
    fragment QuotedCharLiteral : '\'' (~('\'' | '\\' | '\r' | '\n') | '\\' ('\'' | '\\'))+ '\'';

fragment E : 'E' | 'e' ;
fragment SIGN : ('+' | '-') ;
STRING: QuotedStringLiteral;
    fragment QuotedStringLiteral : '"' (~('"' | '\\' | '\r' | '\n') | '\\' ('"' | '\\'))* '"';

LPAREN : '(' ;
RPAREN : ')' ;
LBRACKET : '[' ;
RBRACKET : ']' ;
PLUS : '+' ;
MINUS : '-' ;
TIMES : '*' ;
DIV : '/' ;
GT : '>' ;
LT : '<' ;
EQ : '==' ;
POINT : '.' ;
POW : '^' ;

