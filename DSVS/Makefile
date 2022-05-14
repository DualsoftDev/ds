.ONESHELL: # Applies to every targets in the file!


# DIR=/c/Users/kwak/.vscode/extensions
# copy: g4
# 	cp ds/syntaxes/ds.JSON-tmLanguage ds/syntaxes/ds.tmLanguage.json
# 	cp -r ds $(DIR)/
# 	rm -f ds/syntaxes/ds.tmLanguage.json

# 	cp -r dsvse $(DIR)/

clean:
	cd dsvse
	make clean
	
all:
	cd dsvse
	make all

tsc:
	cd dsvse
	make tsc

g4:
	cd dsvse
	make g4
	
esbuild:
	cd dsvse
	make esbuild

choco:
	choco install nodejs.install
	choco install yarn
	choco install jdk8
	npm install typescript
	npm install antlr4ts
	npm install antlr4ts-cli




#	cd dsvse/server
#	yarn antlr4ts -visitor -o src src/ds.g4
#	mkdir ../client/src/server-bundle/
#	cp src/ds*.ts ../client/src/server-bundle/

