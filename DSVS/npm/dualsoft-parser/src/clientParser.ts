/*
 * DS language parsing/traversing 을 위한 코드
 */

import { ANTLRInputStream, CommonTokenStream } from 'antlr4ts';
import { ParseTree } from 'antlr4ts/tree';
import { dsLexer, dsParser } from './index';


/**
 * DS 문서로부터 parser 객체를 생성해서 반환
 * @param text DS 문서
 */
export function parserFromDocument(text:string) {
	// Create the lexer and parser
	const inputStream = new ANTLRInputStream(text);
	const lexer = new dsLexer(inputStream);
	const tokenStream = new CommonTokenStream(lexer);
	return new dsParser(tokenStream);
}



export function enumerateChildren(from:ParseTree, includeMe=true, predicate:(t:ParseTree) => boolean = null ) : ParseTree[]
{
    const result:ParseTree[] = [];
    enumerateChildrenHelper(result, from, includeMe, predicate);
    return result;
}

function enumerateChildrenHelper(result:ParseTree[], from:ParseTree, includeMe, predicate:(t:ParseTree) => boolean)
{
    function ok(t:ParseTree) {
        if (predicate) return predicate(t);
        return true;
    }

    if (includeMe && ok(from))
        result.push(from);
    for (let index = 0; index < from.childCount; index++)
        enumerateChildrenHelper(result, from.getChild(index), true, ok);
}

export function *enumerateParents(from:ParseTree, includeMe=true, predicate:(t:ParseTree) => boolean = null) : Generator<ParseTree, void, undefined>
{
    const ok = (t:ParseTree) => {
        if (predicate) return predicate(t);
        return true;
    };

    if (includeMe && ok(from))
        yield from;
    yield* enumerateParents(from.parent, true, ok);
}


export function findFirstChild(from:ParseTree, predicate: (exp:ParseTree) => boolean, includeMe=true)
{
    for (const c of enumerateChildren(from, includeMe))
    {
        if (predicate(c))
            return c;
    }

    return null;
}

export function findFirstAncestor(from:ParseTree, predicate: (exp:ParseTree) => boolean, includeMe=true)
{
    for (const c of enumerateParents(from, includeMe))
    {
        if (predicate(c))
            return c;
    }

    return null;
}


