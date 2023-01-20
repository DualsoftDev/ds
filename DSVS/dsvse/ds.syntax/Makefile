.ONESHELL: # Applies to every targets in the file!

all:
	npm install
	make tmlanguage
	make tsc

	
esbuild:
	npm run esbuild

pre:
	vsce publish --pre-release

publish:
	vsce publish

tmlanguage:
	cd syntaxes
	npm run build:grammar


	
