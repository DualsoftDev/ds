.ONESHELL: # Applies to every targets in the file!

tsc:
	cd server
	tsc -b
	cd ../client
	tsc -b	


clean:
	rm -rf client/out
	rm -rf server/out
	rm -rf node_modules
	rm -rf client/node_modules
	rm -rf server/node_modules

all:
	npm install
	make g4
	make tsc

g4:
	pushd ds.syntax/syntaxes
	cp ds.JSON-tmLanguage ds.tmLanguage.json
	
esbuild:
	npm run esbuild

pre:
	vsce publish --pre-release

publish:
	vsce publish

tmlanguage:
	pushd ds.syntax/syntaxes
	npm run build:grammar


	
