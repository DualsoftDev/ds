import { CommonToken, ParserRuleContext } from "antlr4ts";
import * as fs from 'fs';
import { ErrorNode, ParseTree, ParseTreeWalker, TerminalNode } from "antlr4ts/tree";
import {
    logger,
    dsParser, ProgramContext, SysBlockContext, SystemContext, ImportPhraseContext, ImportSystemNameContext, ImportAliasContext, IdContext,
    enumerateChildren, ImportStatementContext, QuotedFilePathContext,
    parserFromDocument, getParseResult, findFirstChild, dsListener, getOriginalText
 } from './index';


export function preprocessDocument(text:string) {
    const imported = processImport(text);
    const macroExpanded = processMacro(imported);
    return macroExpanded;
}




function processMacro(text:string)
{
    return text;
}



/**
 * 
 * @param newSystemName 새로운 시스템 이름
 * @param text 
 * @param system import source 의 시스템 이름
 * @returns 새로운 system 이름으로 치환된 system 정의
 */
function replaceSystemName(newSystemName:string, text:string, system:SystemContext) : string
{
    const id = system.id().text;
    const sysBlock = getOriginalText(text, system.sysBlock());
    return `// imported from ${id}
[sys] ${newSystemName} = ${sysBlock}

`;
}

/** Import 부분 치환된 문자열 반환 */
function processImport(text:string) : string
{
    let result = '';
    const parser = parserFromDocument(text);
    const program = parser.program()
    for (const p of program.children)
    {
        if ( p instanceof ImportStatementContext)   // !#import { Cylinder as A} from "./cylinder.d.ts";
        {
            let filePath = findFirstChild(p, t => t instanceof QuotedFilePathContext).text.replaceAll('"', '').replaceAll("'", ''); // "./cylinder.d.ts"
            const phrases = enumerateChildren(p, false, t => t instanceof ImportPhraseContext)  // [{Cylinder as A}]
            
            const importText = fs.readFileSync(filePath, 'utf8');
            const importParser = parserFromDocument(importText);
            const importSystems = enumerateChildren(importParser.program(), false, t => t instanceof SystemContext).map(s => s as SystemContext);
            const sysDic = Object.fromEntries(importSystems.map(s => [s.id().text, s]));

            for (const imp of phrases )
            {
                const system = findFirstChild(imp, t => t instanceof ImportSystemNameContext).text; // Cylinder
                const alias = findFirstChild(imp, t => t instanceof ImportAliasContext).text;       // A
                const sys = sysDic[system];
                result += replaceSystemName(alias, importText, sys);
                logger?.debug(system, alias);
            }
        }
        else if (p instanceof SystemContext)
        {
            result += getOriginalText(text, p);
        }
        else if (p instanceof TerminalNode && p.symbol.type === dsParser.EOF)
        {
            logger?.debug('EOF=', p.text);
        }
        else
        {
            logger?.error('UNKNOWN=', p.text);
        }
    }

    return result;
}


//// test

if (process.env.NODE_ENV === 'development')
{
    const text =`
!#import {
    Cylinder as A,
    Cylinder as B,
    Cylinder as C,
} from "cylinder.ds";
    
[sys] mySystem = {
    [task] R = {
      GRIP = {QGRIP ~ IGRIP}
      RELEASE = {QRELEASE ~ IRELEASE}
      WELD = {QWELD ~ _}
    }
    [flow] F = {
        ADV = {A.ADV > B.ADV > C.ADV; }
        RET = {C.RET > B.RET > A.RET; }
        ADV <||> RET;
        R.GRIP <||> R.RELEASE;
        ADV > R.GRIP > @selfr(R.WELD) > R.RELEASE > RET;
    }
}
`;
      
    const x = preprocessDocument(text);
    console.log(x)
}
