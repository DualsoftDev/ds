.ONESHELL: # Applies to every targets in the file!
# .PHONY common

p:
	dotnet pack Web/Dual.Web.sln --configuration Release --output nupkgs

t:
	dotnet pack Web/Dual.Web.Blazor/Dual.Web.Blazor.csproj --configuration Release --output nupkgs	

cn:
	-@rm -rf $(shell cygpath $(USERPROFILE))/.nuget/packages/dualsoft-*
	-@dotnet restore Dual.Nuget.sln

clean-nuget:
	-@dotnet nuget locals all --clear
	-@dotnet restore Dual.Nuget.sln
	-@dotnet restore Dual.Nuget.MsBuild.sln

ramdisk:
	@rm -rf /r/bin /r/build /r/packages ./bin ./packages
	@mkdir /r/bin
	@ln -s /r/bin
	@mkdir /r/packages
	@ln -s /r/packages
	@if [ ! -f Directory.Build.props ]; then cp Directory.Build.props.ramdisk Directory.Build.props; fi
	@mkdir -p nupkgs


# Dual.Nuget.sln 에서 정규식 치환
# \[assembly: AssemblyVersion\("\d+(\.\d+)*"\)\]	==>		[assembly: AssemblyVersion("0.3.3")]
# \[<assembly: AssemblyVersion\("\d+(\.\d+)*"\)>\] ==>		[<assembly: AssemblyVersion("0.3.3")>]
# <Version>.*</Version>				==> <Version>0.3.3</Version>



# Ds.sln 에서 DualSoft-* nuget package 일괄 upgrade
# 정규식 치환
# (<PackageReference Include="DualSoft-[^"]+" Version=")[^"]+(" />)		==>		$1+++0.3.3" />
# 일반 치환
# Version="+++0.3.3" 	==>	 Version="0.3.3"


GROUPS := Common Others PLC Tests
GARBAGES := nupkgs packages
clean: clean-nuget
	-@echo "Removing all subdriectory named [bin] or [obj] or [.vs] on [`pwd`].."
	-@find . -type d \( -name "bin" -o -name "obj" -o -name ".vs" \) -exec rm -rf {} +
	@rm -rf /c/Users/kwak/.nuget/packages/fody/
	@for gr in . $(GROUPS); do \
		for gb in $(GARBAGES); do \
			echo "Removing $$gr/$$gb"; \
			rm -rf $$gr/$$gb; \
		done \
	done

	@echo "Removing all subdriectory named [bin] or [obj] or [.vs] on [`pwd`].."
	@find . -type d \( -name "bin" -o -name "obj" -o -name ".vs" \) -exec rm -rf {} +

	@(cd Tests/FeatureTests; make clean)
	-@mkdir nupkgs

# SOLUTIONS := \
#      Common/Dual.Common.sln \
#      Common/Dual.Common.Etc.sln \
#      Common/Dual.Common.Net48.sln \
#      PLC/Dual.PLC.sln \
#      Web/Dual.Web.sln \
#      Others/Dual.Others.sln

SOLUTIONS := Dual.Nuget.sln
# MSBUILD := "/c/Program\ Files/Microsoft\ Visual\ Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe"
MSBUILD := "/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe"

# dotnet build 로 build 가 안되는 project 만 따로 msbuild package 생성.  DotnetSDK 8.0.401 부터는 COM 포함한 것도 build 됨.
msbuild:
	@mkdir -p bin/net48/net48
	@(cd bin/net48/net48; rm -f Dual.PLC.LS.XgCOM.FS.xml; ln -s ../Dual.PLC.LS.XgCOM.FS.xml)

	@(echo "Building PLC/Dual.PLC.LS.XgCOM.FS"; 
		cd PLC/Dual.PLC.LS.XgCOM.FS &&	$(MSBUILD) \
			-verbosity:minimal -t:pack -p:Configuration=Release -p:SolutionDir="..\.." -p:PackageOutputPath=../../nupkgs Dual.PLC.LS.XgCOM.FS.fsproj)

	# Dirty hacks
	# ------------ "pack" 대상이 없습니다. 오류
	# @(echo "Building Dual.Common.Winform.DevX.24.1.4"; 
	# 	cd Windows/Dual.Common.Winform.DevX.24.1.4 &&	$(MSBUILD) \
	# 		-verbosity:minimal -t:pack -p:Configuration=Release -p:SolutionDir="../.." -p:PackageOutputPath=../../nupkgs Dual.Common.Winform.DevX.24.1.4.csproj)

	# @(echo "Building Dual.Common.Windows.csproj"; 
	# 	cd Windows/Dual.Common.Windows &&	$(MSBUILD) \
	# 		-verbosity:minimal -t:pack -p:Configuration=Release -p:SolutionDir="../.." -p:PackageOutputPath=../../nupkgs Dual.Common.Windows.csproj)

pack:
	# Dirty hacks: bin/netstandard2.0/netstandard2.0 폴더를 요구
	@(mkdir -p bin/netstandard2.0; cd bin/netstandard2.0; ln -s . netstandard2.0)
	@(cd PLC/Dual.PLC.LS.XgCOM.FS; dotnet restore Dual.PLC.LS.XgCOM.FS.fsproj)
	
	@rm -rf /c/Users/kwak/.nuget/packages/fody/
	@for s in $(SOLUTIONS); do \
		echo processing $$s; \
		dotnet pack $$s --configuration Release --output nupkgs; \
	done
	@make msbuild


etc:
	dotnet pack Common/Dual.Common.Etc.sln --configuration Release --output nupkgs;


cmn:
	dotnet pack Common/Dual.Common.sln --configuration Release --output nupkgs;

publish: pack
	# Since we specify the source as nuget.org URL, this will push our package to nuget.org.
	# For pushing the packages to a private feed, we have to specify the corresponding URL as a source.
	rm -f nupkgs/All.Nuget.Loader.*.nupkg
	dotnet nuget push nupkgs/*.nupkg --api-key oy2jp6pj3jzjwc46qe5qgpxdjcok6kpefb4dw6m3fywyvq --source https://api.nuget.org/v3/index.json --skip-duplicate

### sample
