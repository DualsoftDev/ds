.ONESHELL: # Applies to every targets in the file!


antlr4=java -Xmx500M -cp /usr/local/lib/antlr-4.10.1-complete.jar org.antlr.v4.Tool
grun=java -Xmx500M -cp /usr/local/lib/antlr-4.10.1-complete.jar:. org.antlr.v4.gui.TestRig

grun:
	@java -version
	@$(antlr4) dsLexer.g4
	@$(antlr4) dsParser.g4
	@$(antlr4) fqdn.g4
	@$(antlr4) expr.g4
	@javac *.java
	@echo 'echo "A || B, #g(X), C > D, E || F;" | grun ds causals -gui'

tokens:
	$(grun) ds model -tokens < g4test/prop.ds

cli:
	echo "[sys] A = {}" | $(grun) ds model -tokens
gui:
	$(grun) ds model -gui < g4test/prop.ds

test:
	@export CLASSPATH=".:/usr/local/lib/antlr-4.10.1-complete.jar:$$CLASSPATH"
#	for f in g4test/*; do
#		echo $$f
#		$(grun) ds causals < g4test/causals.txt
#	done

	@echo quoted
	$(grun) ds model < g4test/quoted.ds
	@echo diamond
	$(grun) ds model < g4test/diamond.ds
	@echo call
	$(grun) ds model < g4test/call.ds
	@echo prop
	$(grun) ds model < g4test/prop.ds
	@echo ppt
	$(grun) ds model < g4test/ppt.ds

	@echo safety
	$(grun) ds model < g4test/safety.ds

	@echo strong
	$(grun) ds model < g4test/strong.ds

	@echo fqdn
	$(grun) fqdn fqdns < g4test/fqdn.fqdn

	@echo "---------- with errors"

	@echo causals
	$(grun) ds causals < g4test/causals.txt
	@echo proc
	$(grun) ds model < g4test/proc.ds
	@echo taskFlow
	$(grun) ds model < g4test/taskFlow.ds
	@echo ppt2
	$(grun) ds model < g4test/ppt2.ds

	@echo imports
	$(grun) ds importStatements < g4test/imports.txt
	@echo problem
	$(grun) ds model < g4test/problematic.ds
	@echo expression
	$(grun) ds expressions < g4test/expressions.txt

cs:
	java -jar antlr-4.10.1-complete.jar -package NameSpace -Dlanguage=CSharp -visitor -o CSharp/Generated g4s/ds.g4

clean:
	@rm -rf CSharp/Generated
	@rm -rf *.interp *.tokens
	rm -rf *.java *.class


all: g4 grun test

xxx:
	dotnet new -i Antlr4BuildTasks.Templates
	dotnet new antlr
	dotnet build
	dotnet run

java-install:
	choco install javaruntime 8.0.231
	choco install jdk8 8.0.211
	choco install jre8 8.0.321
