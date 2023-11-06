- 다음 json sample 파일은 I/O 처리를 담당하기 위한 third party vendor 의 설정 sample 이다.
- `ServicePort`: [[Server]] 의 tcp socket 서비스 포트 지정
- `TopLevelLocation`: I/O 저장 파일의 최상위 위치
- `Vendors`: 제공할 [[PLC, Field IO]] 의 목록.   본 예제에서는 LsXGI 와 Paix 두개의 field io 를 제공
	- `Name` :  Vendor 의 name.  comment 성격
	- `Location`:
		- 여러 vendor 가 제공 될 때, 이를 서로 구분하기 위한 구분자
		- 물리적 파일의 저장 위치를 결정.  `TopLevelLocation\Location`
			- e.g `Paix` 의 `Location` 이 "p" 이므로 물리적 파일 저장 위치는 "/tmp/iomaps/p/" 폴더에 위치하게 된다.
			- `LsXGI` 의 `Location` 이 "" 이므로 "/tmp/iomaps/" 폴더에 위치한다.
		- `Location` 에 따라 [[DS runtime]]에서 사용하는 tag 의 prefix 도 결정된다.
			- e.g `Paix` 의 `o`utput `b`yte tag 는 `p/ob` 등으로 시작한다.
			- `LsXGI` 의 `o`utput `b`yte tag 는 prefix 없이 `ob` 등으로 시작한다.
	- `Dll`: Tag 주소를 어떻게 해석하는지 방법을 구현한 dll 의 경로
	- `ClassName`: 해당 dll 내에서 `IAddressInfoProvider` 를 구현한 class 의 namespace
	- `Files`: tag 종류 별 저장 file
		- `Name`: 파일 이름
		- `Length`: byte 환산 허용 길이
		- 
```json
{
  "ServicePort": 5555,
  "TopLevelLocation": "/tmp/iomaps",
  "Vendors": [
    {
      "Name": "LsXGI",
      "Location": "",
      "Dll": "F:\\Git\\ds\\DsDotNet\\src\\IOHub\\ThirdParty.AddressInfo.Provider\\bin\\Debug\\net7.0\\ThirdParty.AddressInfo.Provider.dll",
      "ClassName": "ThirdParty.AddressInfo.Provider.AddressInfoProviderLsXGI",
      "Files": [
        {
          "Name": "I",
          "Length": 65536
        },
        {
          "Name": "Q",
          "Length": 1024
        },
        {
          "Name": "M",
          "Length": 2048
        }
      ]
    },
    {
      "Name": "Paix",
      "Location": "p",
      "Dll": "F:\\Git\\ds\\DsDotNet\\src\\IOHub\\ThirdParty.AddressInfo.Provider\\bin\\Debug\\net7.0\\ThirdParty.AddressInfo.Provider.dll",
      "ClassName": "ThirdParty.AddressInfo.Provider.AddressInfoProviderPaix",
      "Files": [
        {
          "Name": "I",
          "Length": 65536
        },
        {
          "Name": "O",
          "Length": 1024
        }
      ]
    }
  ]
}

```