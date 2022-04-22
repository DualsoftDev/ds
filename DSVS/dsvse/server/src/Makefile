.ONESHELL: # Applies to every targets in the file!

antlr4=java -Xmx500M -cp /usr/local/lib/antlr-4.10.1-complete.jar org.antlr.v4.Tool
grun=java -Xmx500M -cp /usr/local/lib/antlr-4.10.1-complete.jar org.antlr.v4.gui.TestRig
grun:
	java -version
	$(antlr4) ds.g4
	javac *.java
	echo 'echo "A || B, #ms(500), C > D, E || F;" | grun ds causal -gui'

clean:
	rm -rf *.java *.class *.interp *.tokens
	rm -rf dsLexer.ts dsListener.ts dsParser.ts dsVisitor.ts



