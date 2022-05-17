import { CommonToken, ParserRuleContext } from "antlr4ts";

export function getOriginalText(text:string, node:ParserRuleContext) {
    const [s, e] = [node._start as CommonToken, node._stop as CommonToken];
    const [sl, sc] = [s.line-1, s.charPositionInLine];
    const [el, ec] = [e.line-1, e.charPositionInLine];
    const lines = text.split('\n');

    function *generateText() {
        if (sl === el)
            yield lines[sl].substring(sc, ec+1)
        else {
            yield lines[sl].substring(sc)
            for (let i = sl + 1; i < el; i++)
                yield lines[i];
            yield lines[el].substring(0, ec+1)
        }
    }

    return Array.from(generateText()).join('\n');
}

