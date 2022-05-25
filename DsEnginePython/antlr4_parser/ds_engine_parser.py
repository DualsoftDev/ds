import sys
from antlr4 import *
from dsLexer import dsLexer
from dsParser import dsParser
from dsListener import dsListener

class Listner(dsListener):
    # Enter a parse tree produced by dsParser#program.
    def enterSystem(self, ctx:dsParser.SystemContext):
        print(ctx.getText())
    def enterTask(self, ctx:dsParser.TaskContext):
        print(ctx.getText())
    def enterFlow(self, ctx:dsParser.FlowContext):
        print(ctx.getText())
    def enterSegment(self, ctx:dsParser.SegmentContext):
        print(ctx.getText())
    def enterCall(self, ctx: dsParser.CallContext):
        print(ctx.getText())

def main(argv):
    input_stream = FileStream(argv[1])
    lexer = dsLexer(input_stream)
    stream = CommonTokenStream(lexer)
    parser = dsParser(stream)
    tree = parser.program()
    listner = Listner()
    walker = ParseTreeWalker()
    walker.walk(listner, tree)

if __name__ == '__main__':
    main(sys.argv)