- 아직 tag 항목이 결정되지 않았고, table 이 바라보는 context 에 의해서 결정된다.
- e.g 현재 table 의 row item 이 `HMIFlow` 이고, 이 row 에서 Drive 에 해당하는 column 이라면 Tag 항목은 동적으로 다음과 같이 결정될 수 있다.
```
@* CompHmiButtonColumn *@
    protected override void OnInitialized() {
        base.OnInitialized();
        Width = (ForceWidth == null) ? "80px" : $"{ForceWidth}px";
        HeaderTemplate = context => @<span>@Caption</span>;
        CellDisplayTemplate = context =>
        {
            object row = context.DataItem;            
            return@<CompWgtPb Text="@Caption" DataItem="@row" TagGetter="@TagGetter" />    ;
        };
    }
    // Tag getter 는 다음과 같이 설정
    // TagGetter="@((dataItem) =>((HMIFlow)dataItem).DrivePush)
```
-  
