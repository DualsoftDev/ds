import { parserFromDocument, getParseResult } from './index';


export function preprocessDocument(text:string) {
    const parser = parserFromDocument(text);

    const pr = getParseResult(parser);
    return pr;
}

const x = preprocessDocument("hello");
console.log(x)