https://www.nuget.org/packages?q=dualsoft

## dotnet restore 문제 발생시 check point
- Dual nuget package 를 사용하는 모든 project 에 대해서 전부 다음 형태로 갖추어져 있어야 함.
```xml
    <PropertyGroup>
		<UseNugetProjectReference Condition="'$(UseNugetProjectReference)' == ''">false</UseNugetProjectReference>
	</PropertyGroup>

	<ItemGroup Condition="'$(UseNugetProjectReference)' == 'true'">
		<ProjectReference Include="..\..\..\..\Submodules\nuget\Common\Dual.Common.Core.FS\Dual.Common.Core.FS.fsproj" />
	</ItemGroup>

	<ItemGroup Condition="'$(UseNugetProjectReference)' != 'true'">
		<PackageReference Include="DualSoft-Common-Core-FS" Version="0.4.4" />
	</ItemGroup>
```