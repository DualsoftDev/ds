.ONESHELL: # Applies to every targets in the file!


antlr4=java -Xmx500M -cp /usr/local/lib/antlr-4.10.1-complete.jar org.antlr.v4.Tool
grun=java -Xmx500M -cp /usr/local/lib/antlr-4.10.1-complete.jar:. org.antlr.v4.gui.TestRig

# pp. 225
grun:
	@java -version
#	@$(antlr4) Tags.g4
	@$(antlr4) ModeTagsLexer.g4 	# must be done first to get ModeTagsLexer.tokens
	@$(antlr4) ModeTagsParser.g4
	@javac *.java
	@echo "Hello <name>John</name>" | $(grun) ModeTags file -tokens

tokens:
	echo "Hello <name>John</name>" | $(grun) ModeTags file -tokens
gui:
	echo "Hello <name>John</name>" | $(grun) ModeTags file -gui


clean:
	@rm -rf CSharp/Generated
	@rm -rf *.interp *.tokens
	rm -rf *.java *.class

