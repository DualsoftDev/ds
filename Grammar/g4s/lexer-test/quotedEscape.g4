grammar quotedEscape;


WS: [ \t\r\n]+ -> skip;

fragment Identifier: ValidIdStart ValidIdChar*;
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

identifier1: IDENTIFIER1;
identifier2: IDENTIFIER2;
identifier3: IDENTIFIER3;
identifier4: IDENTIFIER4;

identifier12: identifier1 | identifier2;
identifier123: identifier12 | identifier3;

phrase: identifier123 (op identifier123)+ ';';
op: '>';



// echo '"A.B">C;' | grun quotedEscape phrase -gui
// echo '"A.B" > C."D#E.F"."Hello";' | grun quotedEscape phrase -gui