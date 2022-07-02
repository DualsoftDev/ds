.ONESHELL: # Applies to every targets in the file!

ant:
	java org.antlr.v4.Tool -Dlanguage=Python3 -visitor -o src ../../Grammar/g4s/ds.g4

installPip:
	pip install antlr4-python3-runtime

clean:
	cd src
	rm -f ds.interp ds.tokens dsLexer.interp sLexer.py 
	rm -rf dsLexer.tokens dsListener.py dsParser.py dsVisitor.py
