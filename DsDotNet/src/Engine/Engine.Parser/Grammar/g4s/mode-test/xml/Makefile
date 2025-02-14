.ONESHELL: # Applies to every targets in the file!


antlr4=java -Xmx500M -cp /usr/local/lib/antlr-4.10.1-complete.jar org.antlr.v4.Tool
grun=java -Xmx500M -cp /usr/local/lib/antlr-4.10.1-complete.jar:. org.antlr.v4.gui.TestRig

# pp. 225
grun:
	@java -version
# must be done first to get ModeTagsLexer.tokens
	@$(antlr4) XMLLexer.g4
	@$(antlr4) XMLParser.g4
	@javac XML*.java
	@echo "Hello <name>John</name>" | $(grun) XML document -tokens

tokens:
	echo "Hello <name>John</name>" | $(grun) XML document -tokens

gui:
	$(grun) XML document -gui < input.txt
cat:
	$(grun) XML document -gui < cat.txt


clean:
	@rm -rf CSharp/Generated
	@rm -rf *.interp *.tokens
	rm -rf *.java *.class

