.ONESHELL:

dotnet-restore:
	@dotnet restore Ds.sln

use-nuget-source:
	@cp Directory.Packages.props.template Directory.Packages.props
	@cp Directory.Build.props.template Directory.Build.props

use-nuget-package:
	@rm -f Directory.Packages.props Directory.Build.props


# powerpoint addin unittest 실패시 임시 대처
tmp-link:
	@cd ./src/UnitTest/UnitTest.PowerPointAddIn/bin/Debug/net7.0-windows
	ln -s ../../../../UnitTest.Engine/bin/Debug/net7.0/dsLib


clean:
	@echo "Removing all subdriectory named [bin] or [obj] or [.vs] on [`pwd`].."
	-@rm -rf .vs *.idea
	-@rm -rf /c/Users/kwak/.nuget/packages/dual*
	-@find . -type d \( -name "bin" -o -name "obj" \) -exec rm -rf {} +
	make dotnet-restore

# copy-template if not exists:
TEMPLATES := ./Apps/CommonAppSettings.json.template \
			 ./Apps/DSPilot/App.config.template \
			 ./Apps/HMI.Obsolete/AppHMI/App.config.template \
			 ./Apps/OfficeAddIn/PowerPointAddInHelper/App.config.template \
			 ./src/IOHub/IO.Core/zmqsettings.json.template \
			 ./src/IOHub/ThirdParty/LS/IOClient.Xgi/zmqhw.json.template \
			 ./src/UnitTest/Engine.TestSimulator/App.config.template \
			 ./src/Web/DsWebApp.Server/appsettings.json.template


TARGETS := $(TEMPLATES:.template=)

copy-template: $(TARGETS)

$(TARGETS): % : %.template
	@if [ ! -f $@ ]; then \
		cp $< $@; \
		echo "Copied $< to $@"; \
	fi




aa:
	rm -rf /c/Users/kwak/.nuget/packages/dual*

e:
	echo $(shell cygpath $(USERPROFILE))
clean-nuget: dotnet-restore
	echo $(shell cygpath $(USERPROFILE))
	@rm -rf $(shell cygpath $(USERPROFILE))/.nuget/packages/dualsoft-*
	@rm -rf ./packages/dualsoft-*

Clean: clean clean-nuget

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

	@(cd linux-build; rm -f test-engine.sln; dotnet new sln -n test-engine; dotnet sln add ../src/UnitTest/UnitTest.Engine/UnitTest.Engine.fsproj)

linux:	
	@rm -f src/Engine/Engine.Parser/g4s
	@(cd src/Engine/Engine.Parser; ln -s ../../../../Grammar/g4s)
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
