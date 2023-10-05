dotnet-restore:
	@dotnet restore

clean: dotnet-restore
	@echo "Removing all subdriectory named [bin] or [obj] or [.vs] on [`pwd`].."
	@find . -type d \( -name "bin" -o -name "obj" -o -name ".vs" \) -exec rm -rf {} +

clean-nuget: dotnet-restore
	@rm -rf $(shell cygpath $(USERPROFILE))/.nuget/packages/dualsoft-*


nuget-feed:
	@nuget sources add -name DevExpress -source https://nuget.devexpress.com/BdOUvsNXVt4bl2W97d8nTSstfBviZAR3IHZcjOglqI8dgjsJtu/api

linux-target-sln:
	@rm -f Ds.linux.sln
	@cp Ds.sln Ds.linux.sln
	@dotnet sln Ds.linux.sln remove src/IOMap/IOMapViewer/IOMapViewer.csproj
	@dotnet sln Ds.linux.sln remove src/Diagram/Diagram.View.MSAGL.csproj
	@dotnet sln Ds.linux.sln remove Apps/HMI/AppHMI/AppHMI.csproj
	@dotnet sln Ds.linux.sln remove Apps/DSModeler/DSModeler.csproj
	@dotnet sln Ds.linux.sln remove Apps/OfficeAddOn/PowerPointAddInForDS/PowerPointAddInForDS.csproj
	@dotnet sln Ds.linux.sln remove Apps/HMI/WebHMI.Server/WebHMI.Server.csproj
	@dotnet sln Ds.linux.sln remove Apps/HMI/WebHMI.Client/WebHMI.Client.csproj
linux:	
	@dotnet build Ds.linux.sln

# -- 다음 project 에서 net48 target 제거
# src/Engine/Engine.Parser/Engine.Parser.csproj
# src/Engine/Engine.Parser.FS/Engine.Parser.FS.fsproj

# -- post-build : copy 를 cp 로 변경
# src/PLC/Dual.PLC.LS.FS/PLC.LS.FS.fsproj

