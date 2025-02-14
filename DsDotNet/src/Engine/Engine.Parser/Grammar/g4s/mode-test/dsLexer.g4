lexer grammar dsLexer;

CODEBEGIN: '<{' -> mode(MODECODE), skip;

SYS: 'sys';
IP: 'ip';
HOST: 'host';
COPY_SYSTEM: 'copy_system';
LAYOUTS: 'layouts';
ADDRESSES: 'addresses';
PROP: 'prop';
SAFETY: 'safety';
FLOW: 'flow';
INTERFACES: 'interfaces';
ALIASES: 'aliases';
EMG_IN: 'emg_in';
EMG: 'emg';
AUTO_IN: 'auto_in';
AUTO: 'auto';
START_IN: 'start_in';
START: 'start';
RESET_IN: 'reset_in';
RESET: 'reset';

BLOCK_COMMENT : '/*' (BLOCK_COMMENT|.)*? '*/' -> channel(HIDDEN) ;
LINE_COMMENT  : '//' .*? ('\n'|EOF) -> channel(HIDDEN) ;

fragment Identifier: ValidIdStart ValidIdChar*;
   // lexical rule for hangul characters
    fragment HangulChar: [\uAC00-\uD7A3]+;

    fragment ValidIdStart
    : ('a' .. 'z') | ('A' .. 'Z') | '_' | HangulChar
    ;

    fragment ValidIdChar
    : ValidIdStart | ('0' .. '9') | HangulChar
    ;


fragment QuotedStringLiteral : '"' (~('"' | '\\' | '\r' | '\n') | '\\' ('"' | '\\'))* '"';

fragment Compo: Identifier|QuotedStringLiteral;

IDENTIFIER1: Compo;
IDENTIFIER2: Compo '.' Compo;
IDENTIFIER3: Compo '.' Compo '.' Compo;
IDENTIFIER4: Compo '.' Compo '.' Compo '.' Compo;

IPV4: [1-9][0-9]*'.'('0'|[1-9][0-9]*)'.'('0'|[1-9][0-9]*)'.'('0'|[1-9][0-9]*);
// IPV4: (INTEGER)(DOT) INTEGER DOT INTEGER DOT INTEGER;

TAG_ADDRESS: ValidTagStart ValidTagChar*;
   fragment ValidTagStart
      : '%'   // | ('a' .. 'z') | ('A' .. 'Z') | '_' | HANGUL_CHAR
      ;
   fragment ValidTagChar
      : DOT | ValidIdChar | ('0' .. '9') | HangulChar
      ;


SQUOTE: '\'';
DQUOTE: '"';
LBRACKET: '[';
RBRACKET: ']';
LBRACE: '{';
RBRACE: '}';
LPARENTHESIS: '(';
RPARENTHESIS: ')';
EQ: '==';
SEIMCOLON: ';';
DOT: '.';
TILDE: '~';
COMMA: ',';
AND: '&';
EXCLAMATION: '!';
OR: '|';
AT: '@';
POUND: '#';
PLUS: '+';
MINUS: '-';
MUL: '*';
DIV: '/';
MOD: '%';

GTE: '>=';
LTE: '<=';

OR2: '||';
EQ2: '==';
NEQ: '!=';
//NEWLINE: '\r'? '\n';

INTEGER: [1-9][0-9]*;
FLOAT: [1-9][0-9]*('.'[0-9]+)?;


// Close Angle Bracket
Cab: '>';
CabCab: '>>';

// Open Angle Bracket
Oab: '<';
OabOab: '<<';

PipeCab: '|>';
CabPipe: '<|';
OabPipePipeCab: '<||>';
CabOabPipe: '><|';
EqualCab: '=>';
CabPipeCab: '>|>';
PipeCabCab: '|>>';
OabOabPipe: '<<|';
OabPipeOab: '<|<';
PipeCabOab: '|><';
PipePipeOab: '||>';
CabPipePipe: '<||';
OabOabPipePipeCabCab: '<<||>>';


QUESTION: '?';

WS: [ \t\r\n]+ -> skip;



// ----------------------------------------------------------------------------
mode MODECODE;

CODE_WS: [ \t\r\n]+ -> skip;
CODE_BLOCK_COMMENT : '/*' (CODE_BLOCK_COMMENT|.)*? '*/' -> channel(HIDDEN) ;
CODE_LINE_COMMENT  : '//' .*? ('\n'|EOF) -> channel(HIDDEN) ;
ENDCODE : '}>'     -> mode(DEFAULT_MODE), skip ; // back to SEA mode 

INTTYPE: 'int';
WORDTYPE: 'word';
FLOATTYPE: 'float';
VOIDTYPE: 'void';
INT :   [0-9]+ ;

CODE: 'code';
IF: 'if';
THEN: 'then';
ELSE: 'else';
RETURN: 'return';
CODE_EQUAL: '==';
CODE_SEMICOLON: ';';
CODE_PLUS: '+';
CODE_MINUS: '-';
CODE_LPARENTHESIS: '(';
CODE_RPARENTHESIS: ')';
CODE_LBRACE: '{';
CODE_RBRACE: '}';


IDENTIFIER: Identifier;




// TOKEN
//    : ('0' .. '9' | 'a' .. 'z' | 'A' .. 'Z' | '-' | ' ' | '/' | '_' | ':' | ',')+
//    ;