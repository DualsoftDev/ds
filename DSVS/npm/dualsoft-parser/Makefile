.ONESHELL: # Applies to every targets in the file!

all:
	npm run all
#	npm run antlr4ts && tsc -b
publish:
	npm publish --access public

unpublish:
	npm unpublish @dualsoft/parser@1.0.0

intall:
	npm install

# requires npm install
ant:
	yarn antlr4ts

clean:
	rm -f *.js *.js.map
	rm -f ds.interp ds.tokens
	cd src
	rm -f ds.interp ds.tokens
	rm -f dsLexer.ts dsListener.ts dsParser.ts dsVisitor.ts	dsLexer.interp dsLexer.tokens
