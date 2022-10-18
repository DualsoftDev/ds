lexer grammar dsProgram;

ToIsland: '<' '@' -> pushMode(Island), more;

mode Island;
ToSeq: '@' '>' -> popMode;      // -> mode(DEFAULT_MODE)
Eqn: 'A' -> more;
