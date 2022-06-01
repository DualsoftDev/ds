import sys
from pathlib import Path
from antlr4 import *
from edgeVisitor import *
from parserUtil import parserFromDocument
from edgeVisitor import getElements

def main(argv):
    f = argv[1]
    text = Path(f).read_text()
    parser = parserFromDocument(text)
    ds_engine:ds_consumer_builder = getElements(parser)
    ds_engine.execute_system("ds_engine", "localhost:9092", 0)

if __name__ == '__main__':
    main(sys.argv)