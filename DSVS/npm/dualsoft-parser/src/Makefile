.ONESHELL: # Applies to every targets in the file!

antlr4=java -Xmx500M -cp /usr/local/lib/antlr-4.10.1-complete.jar org.antlr.v4.Tool
grun=java -Xmx500M -cp /usr/local/lib/antlr-4.10.1-complete.jar:. org.antlr.v4.gui.TestRig

grun:
	export CLASSPATH=".:/usr/local/lib/antlr-4.10.1-complete.jar:$CLASSPATH"
	@cd ../../../../Grammar/g4s
	@java -version
	@$(antlr4) dsLexer.g4
	@$(antlr4) dsParser.g4
	@$(antlr4) expr.g4
	@$(antlr4) fqdn.g4
	@javac *.java
	@echo 'echo "A || B, #g(X), C > D, E || F;" | grun ds causals -gui'

g4:
	@cd ..
	@yarn antlr4ts
#	@cp src/ds*.ts ../client/src/server-bundle/

test:
	@export CLASSPATH=".:/usr/local/lib/antlr-4.10.1-complete.jar:$$CLASSPATH"
#	for f in g4test/*; do
#		echo $$f
#		$(grun) ds causals < g4test/causals.txt
#	done

	cd ../../../../Grammar/g4s

	@echo causals
	$(grun) ds causals < g4test/causals.txt
	@echo imports
	$(grun) ds importStatements < g4test/imports.txt
	@echo problem
	$(grun) ds program < g4test/problematic.ds
	@echo expression
	$(grun) ds expressions < g4test/expressions.txt
	@echo proc
	$(grun) ds program < g4test/proc.ds
	@echo taskFlow
	$(grun) ds program < g4test/taskFlow.ds
	@echo call
	$(grun) ds calls < g4test/calls.txt
	@echo elevator
	$(grun) ds program < g4test/elevator_newexample.ds


clean:
	@rm -rf *.interp *.tokens dsLexer.ts dsListener.ts dsParser.ts dsVisitor.ts
	@cd ../../../../Grammar/g4s
	@rm -rf *.java *.class 


all: g4 grun test
