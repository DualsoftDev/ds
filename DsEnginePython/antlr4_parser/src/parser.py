import sys
from pathlib import Path
from antlr4 import *
from dsLexer import dsLexer
from dsParser import dsParser
from edgeVisitor import *
from parserUtil import parserFromDocument
from edgeVisitor import getElements

def main(argv):
    f = '../input.ds'   # argv[1]
    text = Path(f).read_text()
    parser = parserFromDocument(text)
    elements = getElements(parser)


    # input_stream = FileStream(argv[1])
    # lexer = dsLexer(input_stream)
    # stream = CommonTokenStream(lexer)
    # parser = dsParser(stream)

    tree = parser.program()
    print(tree.getText())
 
if __name__ == '__main__':
    main(sys.argv)