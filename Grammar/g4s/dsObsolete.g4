grammar dsObsolete;
import dsLexer;

quotedFilePath
    : DQUOTE (~DQUOTE)* DQUOTE
    | SQUOTE (~SQUOTE)* SQUOTE
    ;


importStatements: importStatement+ ;
importStatement : importFinal SEIMCOLON;

importFinal
    : '!#import' importAs 'from' quotedFilePath
    ;


importAs
    : importPhrase
    | LBRACE importPhrase
        (COMMA importPhrase)* (COMMA)?
        RBRACE
    ;

importPhrase: importSystemName 'as' importAlias;
importSystemName: identifier;
importAlias: identifier;


acc: LBRACKET ACCESS_SRE RBRACKET EQ LBRACE identifier (SEIMCOLON identifier)* SEIMCOLON? RBRACE;    // [accsre] = { A; B }
ACCESS_SRE: ('accsre'|'accsr'|'accre'|'accse'|'accs'|'accr'|'acce');
