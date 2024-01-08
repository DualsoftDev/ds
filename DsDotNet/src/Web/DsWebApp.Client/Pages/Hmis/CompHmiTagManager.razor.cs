namespace DsWebApp.Client.Pages.Hmis;

/// <summary>
/// HMI tag 정보를 관리하기 위한 component.
/// <br/>
/// Tag 변경을 서버로 전송하고(REST), 서버에서 변경된 Tag 정보를 수신한다(SignalR).
/// <br/>
/// - ClientGlobal.TagChangedSubject 를 구독하여 Tag 변경을 수신할 수 있도록 singnalR 과 연동한다.
/// <br/>
/// - CascadingParameter 로 sub component 들에 전달되므로, 시작 page 에서 CascadingValue 로 전달해 주어야 한다.
/// <br/>
/// PostTag(TagWeb) 메소드
/// </summary>
public partial class CompHmiTagManager
{
}
