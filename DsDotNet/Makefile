dotnet-restore:
	@dotnet restore

clean: dotnet-restore
	@echo "Removing all subdriectory named [bin] or [obj] or [.vs] on [`pwd`].."
	@find . -type d \( -name "bin" -o -name "obj" -o -name ".vs" \) -exec rm -rf {} +

clean-nuget: dotnet-restore
	@rm -rf $(shell cygpath $(USERPROFILE))/.nuget/packages/dualsoft-*

