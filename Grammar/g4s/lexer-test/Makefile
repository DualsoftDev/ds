.ONESHELL: # Applies to every targets in the file!


antlr4=java -Xmx500M -cp /usr/local/lib/antlr-4.10.1-complete.jar org.antlr.v4.Tool
grun=java -Xmx500M -cp /usr/local/lib/antlr-4.10.1-complete.jar:. org.antlr.v4.gui.TestRig

# pp. 225
grun:
	@java -version
# must be done first to get ModeTagsLexer.tokens
	@$(antlr4) quotedEscape.g4
	@javac *.java

test:
	echo '"A.B" > C."D#E.F"."Hello" > X.Y;' | $(grun) quotedEscape phrase -gui

gui:
	$(grun) quotedEscape phrase -gui < input.txt
cat:
	$(grun) quotedEscape phrase -gui < cat.txt


clean:
	@rm -rf CSharp/Generated
	@rm -rf *.interp *.tokens
	rm -rf *.java *.class

