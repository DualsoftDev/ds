.ONESHELL: # Applies to every targets in the file!

doxy:
	doxygen

classes:
	$(eval cwd=$(shell pwd))
# 	echo CWD is $(cwd)
	$(eval dos=$(shell cygpath -d $(cwd)))
#	echo DOS path = $(dos)
	"C:\Program Files\Mozilla Firefox\firefox.exe" "file:$(dos)\html\index.html"

install-paket-tool:
	dotnet tool install -g Paket

isntall-package:
	paket install

clean:
	rm -rf latex html
	rm -rf */bin */obj

cloc:
	cloc Engine*
