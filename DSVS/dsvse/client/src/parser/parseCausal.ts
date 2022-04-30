/*
    CausalContext : causal 한문장.  'A > B;'
*/


/*
A, B, C || D, E || F, G, H || I			CausalExpressionContext
	A, B, C || D, E || F, G, H || I		SegmentContext
		A, B, C || D, E || F, G, H || I	SegmentsDNFContext
			A, B, C	SegmentsCNFContext
				A   SegmentContext
                ,   TerminalNode
				B   SegmentContext
                ,   TerminalNode
				C   SegmentContext
			||    	TerminalNode
			D, E    SegmentsCNFContext
                D   SegmentContext
                ,   TerminalNode
                E   SegmentContext
            ||    	TerminalNode
            F, G, H SegmentsCNFContext
                F   SegmentContext
                ,   TerminalNode
                G   SegmentContext
                ,   TerminalNode
                H   SegmentContext
            ||    	TerminalNode
            I    	SegmentsCNFContext
                I   SegmentContext
*/

import { ParserRuleContext } from 'antlr4ts';
import { ParseTree } from 'antlr4ts/tree';
import { assert } from 'console';
import { CausalContext, SegmentContext, SegmentsDNFContext, ExpressionContext, ProcContext, ProgramContext, SystemContext, ProcSleepMsContext, SegmentsCNFContext } from '../server-bundle/dsParser';

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


// export function* enumerateChildren(from:ParseTree, includeMe=true, predicate:(t:ParseTree) => boolean = null ) : Generator<ParseTree, void, undefined>
// {
//     const ok = (t:ParseTree) => {
//         if (predicate) return predicate(t);
//         return true;
//     };

//     if (includeMe && !ok || ok(from))
//         yield from;
//     for (let index = 0; index < from.childCount; index++)
//         yield* enumerateChildren(from.getChild(index), true, ok);
// }

// export function collectCNFs(exp:ParseTree) : SegmentsCNFContext[]
// {
//     assert(exp instanceof CausalContext
//         // || exp instanceof CausalExpressionContext
//         || exp instanceof SegmentContext
//         || exp instanceof SegmentsDNFContext);
//     return Array.from(helper(exp));

//     function *helper(exp:ParseTree)
//     {
//         if (exp instanceof CausalContext)
//         {
//             yield* helper(exp.children[0]);
//             yield* helper(exp.children[2]);
//         }
//         else
//         {
//             const terminal = findFirstChild(exp, (e => e instanceof SegmentsDNFContext));
//             yield* Array.from(enumerateChildren(terminal))
//                 .filter(c => c instanceof SegmentsCNFContext)
//                 .map(c => c as SegmentsCNFContext)
//                 ;    
//         }
//     }
// }

