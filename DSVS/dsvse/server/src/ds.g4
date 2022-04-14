// https://www.youtube.com/watch?v=bfiAvWZWnDA&t=157s

// https://github.com/tunnelvisionlabs/antlr4ts
// ds-master/dsvs/dsvse/server$ yarn antlr4ts src/ds-language.g4

grammar ds;

program: system* EOF;


system: syshdr segname '=' sysBlock;


syshdr: '[Sys]';
sysBlock
    : simpleSysBlock        #simpleSysBlock
    | complexSysBlock       #complexSysBlock
    ;
simpleSysBlock: '{' segname (';' segname)* '}';
complexSysBlock: '{' (acc|macro|causal)* '}';

acc: '[' 'acc' ('SRE'|'SR'|'RE'|'SE'|'S'|'R'|'E') ']' '=' '{' segname (';' segname)* '}';
macro: '[macro]' '=' '{' segname (';' segname)* '}';
causal: segname causalOperator segname;
causalOperator: '<' | '>' | '<|>';

WS: [ \t\r\n]+ -> skip;

IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_]*;
segname: IDENTIFIER;


TOKEN
   : ('0' .. '9' | 'a' .. 'z' | 'A' .. 'Z' | '-' | ' ' | '/' | '_' | ':' | ',')+
   ;