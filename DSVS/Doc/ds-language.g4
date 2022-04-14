// https://www.youtube.com/watch?v=bfiAvWZWnDA&t=157s

grammar DS;

program: system* EOF;


system: syshdr segname '=' (simpleSysBlock | complexSysBlock);

syshdr: '[Sys]';
simpleSysBlock: '{' segname (';' segname)* '}';
complexSysBlock: '{' (acc|macro|causal)* '}';

acc: '[' 'acc' [SRE|SR|RE|SE|S|R|E] ']' '=' '{' segname (';' segname)* '}';
macro: '[macro]' '=' '{' segname (';' segname)* '}';
causal: segname causalOperator segname;
causalOperator: '<' | '>' | '<\|>';

WS: [ \t\r\n]+ -> skip;

IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_]*;
segname: IDENTIFIER;


