grammar dsObsolete;
import dsLexer;

/*
[cpus] AllCpus = {
    [cpu] Cpu = {
        L.F;
    }
}
 */

cpus: '[' 'cpus' ']' (identifier1)? '==' cpusBlock;
cpusBlock
    : LBRACE (cpu)* RBRACE
    ;

cpu: cpuProp identifier1 '==' cpuBlock;    // [cpu] Cpu = {..}
cpuProp: '[' 'cpu' ']';
cpuBlock
    : LBRACE flowPath (SEIMCOLON flowPath)* SEIMCOLON? RBRACE
    ;



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


/*

sysTask
    : taskProp id '==' LBRACE (listing|call)* RBRACE
    ;
taskProp: '[' 'task' ']';

// flow 내에 정의되는 task.  id 를 갖지 않는다.
flowTask: taskProp EQ LBRACE (listing|call)* RBRACE;


// [macro=T] = { (call)* }
macro: LBRACKET macroHeader RBRACKET EQ LBRACE (call)* RBRACE;
macroHeader
    : simpleMacroHeader
    | namedMacroHeader
    ;
simpleMacroHeader: 'macro';
namedMacroHeader: 'macro' EQ identifier;
 */
