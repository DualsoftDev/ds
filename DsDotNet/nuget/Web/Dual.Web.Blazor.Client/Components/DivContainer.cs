using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Dual.Web.Blazor.Client;

public class DivContainer : ComponentBase
{
    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public string DivId { get; set; }
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "id", DivId ?? "ParentComponent");
        builder.AddContent(2, ChildContent);
        builder.CloseElement();
    }
}

/*
    <DivContainer DivId="myDivContainer">
        <p id="first">Inner text</p>
        <p id="second">Second Inner text</p>
    </DivContainer>
 */
