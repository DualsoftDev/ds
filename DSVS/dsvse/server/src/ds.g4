// https://www.youtube.com/watch?v=bfiAvWZWnDA&t=157s

// https://github.com/tunnelvisionlabs/antlr4ts
// ds-master/dsvs/dsvse/server$ yarn antlr4ts src/ds-language.g4

grammar ds;

IDENTIFIER: VALID_ID_START VALID_ID_CHAR*;
fragment VALID_ID_START
   : ('a' .. 'z') | ('A' .. 'Z') | '_'
   ;


fragment VALID_ID_CHAR
   : VALID_ID_START | ('0' .. '9')
   ;

program: (system|comment)* EOF;


system: sysHdr segment '=' sysBlock;


sysHdr: LBRACKET sys RBRACKET;
sysBlock
    : simpleSysBlock        #caseSimpleSysBlock
    | complexSysBlock       #caseComplexSysBlock
    ;
simpleSysBlock: '{' segment (';' segment)* '}';
complexSysBlock: '{' (acc|macro|causal)* '}';

acc: '[' 'acc' ('SRE'|'SR'|'RE'|'SE'|'S'|'R'|'E') ']' '=' '{' segment (';' segment)* '}';
macro: '[macro]' '=' '{' segment (';' segment)* '}';
causal: segment causalOperator segment;
causalOperator: '<' | '>' | '<|>';

segment: WS* IDENTIFIER WS*;

comment: BLOCK_COMMENT | LINE_COMMENT;
BLOCK_COMMENT : '/*' (BLOCK_COMMENT|.)*? '*/' -> channel(HIDDEN) ;
LINE_COMMENT  : '//' .*? ('\n'|EOF) -> channel(HIDDEN) ;


// COMMENT
//     : '/*' .*? '*/' -> skip
// ;

// LINE_COMMENT
//     : '//' ~[\r\n]* -> skip
// ;

sys: 'Sys';
LBRACKET: '[';
RBRACKET: ']';


WS: [ \t\r\n]+ -> skip;

// TOKEN
//    : ('0' .. '9' | 'a' .. 'z' | 'A' .. 'Z' | '-' | ' ' | '/' | '_' | ':' | ',')+
//    ;