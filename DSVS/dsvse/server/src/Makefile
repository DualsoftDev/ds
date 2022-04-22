.ONESHELL: # Applies to every targets in the file!

antlr4=java -Xmx500M -cp /usr/local/lib/antlr-4.10.1-complete.jar org.antlr.v4.Tool
grun=java -Xmx500M -cp /usr/local/lib/antlr-4.10.1-complete.jar:. org.antlr.v4.gui.TestRig

grun:
	@java -version
	@$(antlr4) ds.g4
	@javac *.java
	@echo 'echo "A || B, #g(X), C > D, E || F;" | grun ds causals -gui'

g4:
	@cd ..
	@yarn antlr4ts -visitor -o src src/ds.g4
	@cp src/ds*.ts ../client/src/server-bundle/

test:
	@export CLASSPATH=".:/usr/local/lib/antlr-4.10.1-complete.jar:$$CLASSPATH"
	@$(grun) ds causals < g4test/causals.txt


clean:
	@rm -rf *.java *.class *.interp *.tokens
	@rm -rf dsLexer.ts dsListener.ts dsParser.ts dsVisitor.ts


all: g4 grun test
