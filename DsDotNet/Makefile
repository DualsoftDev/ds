dotnet-restore:
	@dotnet restore

clean: dotnet-restore
	@echo "Removing all subdriectory named [bin] or [obj] or [.vs] on [`pwd`].."
	@find . -type d \( -name "bin" -o -name "obj" -o -name ".vs" \) -exec rm -rf {} +

clean-nuget: dotnet-restore
	@rm -rf $(shell cygpath $(USERPROFILE))/.nuget/packages/dualsoft-*


nuget-feed:
	@nuget sources add -name DevExpress -source https://nuget.devexpress.com/BdOUvsNXVt4bl2W97d8nTSstfBviZAR3IHZcjOglqI8dgjsJtu/api
	@cp linux-build/Nuget.Config ~/.nuget/Nuget/Nuget.Config

linux-target-sln:
	@rm -f Ds.linux.sln
	@cp Ds.sln Ds.linux.sln
	@dotnet sln Ds.linux.sln remove src/IOMap/IOMapViewer/IOMapViewer.csproj
	@dotnet sln Ds.linux.sln remove src/Diagram/Diagram.View.MSAGL.csproj
	@dotnet sln Ds.linux.sln remove Apps/HMI/AppHMI/AppHMI.csproj
	@dotnet sln Ds.linux.sln remove Apps/DSModeler/DSModeler.csproj
	@dotnet sln Ds.linux.sln remove Apps/OfficeAddOn/PowerPointAddInForDS/PowerPointAddInForDS.csproj

linux:	
	@dotnet build Ds.linux.sln

# -- 다음 project 에서 net48 target 제거
# src/Engine/Engine.Parser/Engine.Parser.csproj
# src/Engine/Engine.Parser.FS/Engine.Parser.FS.fsproj

# -- post-build : copy 를 cp 로 변경
# src/IOMap/IOMapForModeler/IOMapForModeler.fsproj
# src/PLC/Dual.PLC.LS.FS/PLC.LS.FS.fsproj
# src/UnitTest/UnitTest.IOMap/UnitTest.IOMap.fsproj

# -- linux 실행이 불가능한 항목들...
# dotnet /home/kwak/Git/ds/DsDotNet/bin/net7.0/IOMapService.dll
# Unhandled exception. System.PlatformNotSupportedException: ServiceController enables manipulating and accessing Windows services and it is not applicable for other operating systems.
#    at System.ServiceProcess.ServiceBase..ctor()
#    at DsMemoryService.ServiceImpl.IOMapService..ctor() in /home/kwak/Git/ds/DsDotNet/src/IOMap/IOMapService/Service.fs:line 48
#    at Program.main(String[] argv) in /home/kwak/Git/ds/DsDotNet/src/IOMap/IOMapService/Program.fs:line 17
# Aborted
