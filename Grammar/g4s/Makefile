.ONESHELL: # Applies to every targets in the file!


antlr4=java -Xmx500M -cp /usr/local/lib/antlr-4.10.1-complete.jar org.antlr.v4.Tool
grun=java -Xmx500M -cp /usr/local/lib/antlr-4.10.1-complete.jar:. org.antlr.v4.gui.TestRig

grun:
	@java -version
	@$(antlr4) ds.g4
	@javac *.java
	@echo 'echo "A || B, #g(X), C > D, E || F;" | grun ds causals -gui'


test:
	@export CLASSPATH=".:/usr/local/lib/antlr-4.10.1-complete.jar:$$CLASSPATH"
#	for f in g4test/*; do
#		echo $$f
#		$(grun) ds causals < g4test/causals.txt
#	done

	@echo quoted.ds
	$(grun) ds program < g4test/quoted.ds
	@echo causals
	$(grun) ds causals < g4test/causals.txt
	@echo proc
	$(grun) ds program < g4test/proc.ds
	@echo taskFlow
	$(grun) ds program < g4test/taskFlow.ds
	@echo call
	$(grun) ds calls < g4test/calls.txt
	@echo ppt
	$(grun) ds program < g4test/ppt.ds

	@echo ppt2
	$(grun) ds program < g4test/ppt2.ds

	@echo safety
	$(grun) ds program < g4test/safety.ds

	@echo flowTask
	$(grun) ds program < g4test/flowTask.ds

	@echo strong
	$(grun) ds program < g4test/strong.ds

	@echo "---------- with errors"
	@echo imports
	$(grun) ds importStatements < g4test/imports.txt
	@echo problem
	$(grun) ds program < g4test/problematic.ds
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
