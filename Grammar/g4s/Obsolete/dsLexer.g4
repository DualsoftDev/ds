lexer grammar dsLexer;


SYS: ('sys'|'system');
EXTERNAL_SYSTEM: 'external';
IP: 'ip';
HOST: 'host';
FILE: 'file';
DEVICE: 'device';
COPY_SYSTEM: 'copy_system';
LAYOUTS: 'layouts';
ADDRESSES: 'addresses';
PROP: 'prop';
SAFETY: 'safety';
FLOW: 'flow';
INTERFACES: 'interfaces';
ALIASES: 'aliases';
JOBS: 'jobs';
BUTTONS: 'buttons';
LAMPS: 'lamps';
EMG_IN: 'emg_in';
EMG: 'emg';
AUTO_IN: 'auto_in';
AUTO: 'auto';
DRIVE_IN: 'drive_in';
DRIVE: 'drive';
CLEAR_IN: 'clear_in';
CLEAR: 'clear';
MANUAL_IN: 'manual_in';
MANUAL: 'manual';
STOP_IN: 'stop_in';
STOP: 'stop';
TEST_IN: 'test_in';
TEST: 'test';
HOME_IN: 'home_in';
HOME: 'home';
READY_IN: 'ready_in';
READY: 'ready';
VARIABLES: 'variables';
COMMANDS: 'commands';
OBSERVES: 'observes';
WORDTYPE: 'word';
DWORDTYPE: 'dword';
INTTYPE: 'int';
FLOATTYPE: 'float';
FUNC: 'func';


WS: [ \t\r\n]+ -> skip;
BLOCK_COMMENT : '/*' (BLOCK_COMMENT|.)*? '*/' -> channel(HIDDEN) ;
LINE_COMMENT  : '//' .*? ('\n'|EOF) -> channel(HIDDEN) ;

fragment CODE_BLOCK_START: '<@{';
fragment CODE_BLOCK_END: '}@>';
CODE_BLOCK: CODE_BLOCK_START (BLOCK_COMMENT|LINE_COMMENT|CODE_BLOCK|.)*? CODE_BLOCK_END;

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
FNC: '$';
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

INTEGER: '0'|[1-9][0-9]*;
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


// TOKEN
//    : ('0' .. '9' | 'a' .. 'z' | 'A' .. 'Z' | '-' | ' ' | '/' | '_' | ':' | ',')+
//    ;