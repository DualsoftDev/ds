- All HMI tags for data transfer (via REST)
- `Client\Pages\Hmis\PageHmiTags.razor` 에서 요청
	- REST HttpGet : `api/model/tag`
- `Server\Controllers\ModelController.cs`
	- `TagWeb[] GetAllHmiTags()`
- `TagWebORM` 구조로 변경한 후, `DxGrid` 에 표출


- `Engine.Info` project 의 `ORMTagKind` 를 이용해서 `TagKind` int 값을 읽기 쉬운 문자열로 변환
- 