.ONESHELL: # Applies to every targets in the file!

all:
	npm run antlr4ts && tsc -b

ant:
	yarn antlr4ts

clean:
	rm -f allVisitor.js*  clientParser.js*  cytoscapeVisitor.js*  dsLexer.js*  dsListener.js*  dsParser.js*  dsVisitor.js*
	rm -f ds.interp ds.tokens
	cd src
	rm -f ds.interp ds.tokens dsLexer.ts dsListener.ts dsParser.ts dsVisitor.ts
