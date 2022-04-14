.ONESHELL: # Applies to every targets in the file!


DIR=/c/Users/kwak/.vscode/extensions
copy:
	cp ds/syntaxes/ds.JSON-tmLanguage ds/syntaxes/ds.tmLanguage.json
	cp -r ds $(DIR)/
	rm -f ds/syntaxes/ds.tmLanguage.json


# https://github.com/tunnelvisionlabs/antlr4ts
antlr:
	cd dsvse/server
	npm install antlr4ts --save
	yarn add antlr4ts
	npm install antlr4ts-cli --save-dev
	yarn add -D antlr4ts-cli


g4:
	cd dsvse/server
	yarn antlr4ts -visitor -o src src/ds.g4

