grammar dsSample;

dummyTopRule: STRING;


// STRING: '"' .*? '"';

STRING: '"' (ESC|.)*? '"' ;
fragment ESC : '\\"' | '\\\\' ; // 2-char sequences \" and \\


LINE_COMMENT : '//' .*? '\r'? '\n' -> skip ; // Match "//" stuff '\n'
COMMENT : '/*' .*? '*/' -> skip ; // Match "/*" stuff "*/"

STUFF : ~'\n'+ -> skip ; // match and discard anything but a '\n'