.ONESHELL: # Applies to every targets in the file!

jar = antlr4-4.13.0-complete.jar
jarPath = /usr/local/lib/$(jar)

antlr4=java -Xmx500M -cp $(jarPath) org.antlr.v4.Tool
# grun=java -Xmx500M -cp $(jarPath):. org.antlr.v4.gui.TestRig
grun=antlr4-parse
grun:
	java -version
	$(antlr4) dsLexer.g4
	$(antlr4) ds.g4
	$(antlr4) fqdn.g4
	$(antlr4) expr.g4
	javac *.java
	@echo 'echo "A || B, #g(X), C > D, E || F;" | grun ds causals -gui'

tokens:
	$(grun) ds model -tokens < g4test/prop.ds

cli:
	echo "[sys] A = {}" | $(grun) ds model -tokens
gui:
	$(grun) ds.g4 system -gui g4test/prop.ds

udt:
	$(grun) expr.g4 toplevels -gui g4test/udt.ds
udt2:
	$(grun) expr.g4 udtStorageName -gui g4test/udt-storage.ds
udt3:
	$(grun) expr.g4 toplevels -gui g4test/udt-member-assign.ds
udt4:
	$(grun) expr.g4 toplevels -gui g4test/udt-member-referece.ds
udt5:
	$(grun) expr.g4 toplevels -gui g4test/udt-copy.ds

version:
	$(grun) ds.g4 versionDef -gui g4test/version.ds
versions:
	$(grun) ds.g4 versionsBlock -gui g4test/versions.ds
version2:
	echo 1.2.3.4 | $(grun) ds.g4 version  -gui

lambda:
	$(grun) expr.g4 toplevels -gui g4test/lambda.ds

proc:
	$(grun) expr.g4 toplevels -gui g4test/proc.ds

test:
	@export CLASSPATH=".:$(jarPath):$$CLASSPATH"
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
	java -jar $(jarPath) -package NameSpace -Dlanguage=CSharp -visitor -o CSharp/Generated g4s/ds.g4

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

antlrjar = https://www.antlr.org/download/$(jar)
java-install:
	# choco install python
	pip install antlr4-tools
	mkdir -p /usr/local/lib
	cd /usr/local/lib
	curl -O $(antlrjar)
	# https://download.oracle.com/java/20/latest/jdk-20_windows-x64_bin.exe

	# test it
	# antlr4-parse ds.g4 system -gui g4test/flow.ds
