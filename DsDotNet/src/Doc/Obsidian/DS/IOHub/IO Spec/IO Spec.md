[[Third party vendor]] 에서 [[PLC, Field IO]]를 원활히 처리 할 수 있도록 API 수준에서 제공

1. Third party vendor 는 tag 주소를 어떻게 해석해야 하는지 해석 방법을 Dotnet dll 을 이용해서 구현하여야 한다.
	1. vendor dll 작성
		1. Dotnet project 생성
		2. `IO.Spec.dll` 을 참조로 추가 (`DsDotNet/src/IOHub/IO.Spec/IO.Spec.csproj`)
		3. `IO.Spec.IAddressInfoProvider` interface 를 구현한 dotnet class 작성;
		4. 위의 과정은 샘플 프로젝트 `/DsDotNet/src/IOHub/ThirdParty.AddressInfo.Provider/ThirdParty.AddressInfo.Provider.csproj` 를 참고하도록 한다.
	2. vendor.dll 등록 및 [[IO settings json]] 파일 설정
