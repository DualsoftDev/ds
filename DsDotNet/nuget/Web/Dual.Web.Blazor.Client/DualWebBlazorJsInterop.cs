using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Newtonsoft.Json.Linq;

namespace Dual.Web.Blazor.Client;

// This class provides an example of how JavaScript functionality can be wrapped
// in a .NET class for easy consumption. The associated JavaScript module is
// loaded on demand when first needed.
//
// This class can be registered as scoped DI service and then injected into Blazor
// components for use.

/// <summary>
/// JSInterop for DualWebBlazorJsInterop
/// </summary>
public class DualWebBlazorJsInterop : IAsyncDisposable
{
    protected Lazy<Task<IJSObjectReference>> moduleTask;
    private IJSRuntime _jsRuntime;

    public DualWebBlazorJsInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        moduleTask = new (() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Dual.Web.Blazor.Client/js/dualWebBlazorJsInterop.js").AsTask());
    }

    public async ValueTask<string> Ping()
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<string>("ping");
    }


    /// <summary>
    /// 사용자 입력을 받는 모달 다이얼로그
    /// </summary>
    public async ValueTask<string> Prompt(string message)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<string>("showPrompt", message);
    }

    /// <summary>
    /// '확인', '취소' 버튼을 가진 모달 다이얼로그
    /// </summary>
    public async ValueTask<bool> Confirm(string message)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<bool>("showConfirm", message);
    }
    /// <summary>
    /// '확인' 버튼을 가진 모달 다이얼로그
    /// </summary>
    public async ValueTask Alert(string message)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("showAlert", message);
    }
    public async ValueTask<string> Jwt2Json(string jwt)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<string>("jwt2json", jwt);
    }

    public async ValueTask<string> GetUserAgent()
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<string>("getUserAgent");
    }

    public async ValueTask<WindowDimension> GetBrowserWindowDimension()
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<WindowDimension>("getBrowserWindowDimension");
    }

    public async ValueTask<WindowDimension> GetImageDimension(string imageUrl)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<WindowDimension>("getImageDimension", imageUrl);
    }

    public async ValueTask<Rect> GetClientRectById(string elementId)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<Rect>("getClientRectById", elementId);
    }

    public async ValueTask<WindowDimension> GetVideoDimension(string videoUrl)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<WindowDimension>("getVideoDimension", videoUrl);
    }
    public async ValueTask<WindowDimension> GetMediaDimension(string videoUrl)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<WindowDimension>("getMediaDimension", videoUrl);
    }


    // 사용자가 설정한 배율과 상관없는 screen (모니터) 본래의 해상도 값
    public async ValueTask<WindowDimension> GetScreenHardwareResolution()
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<WindowDimension>("getScreenHardwareResolution");
    }

    // 사용자가 설정한 배율이 적용된 screen 해상도 값
    public async ValueTask<WindowDimension> GetScreenDimension()
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<WindowDimension>("getScreenDimension");
    }

    public async ValueTask<string> CreateMemberCallFunction(object dotnetObj, string dotnetObjMethodName, string description)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<string>("createMemberCallFunction", dotnetObj, dotnetObjMethodName, description);
    }
    public async ValueTask<object> InvokeStoredFunction(string functionKey)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<object>("invokeStoredFunction", functionKey);
    }


    public async ValueTask<bool> IsFullScreen()
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<bool>("isFullScreen");
    }



    // DotNetObjectReference dotnetObj
    public async ValueTask AddEventHandler(object dotnetObj, string eventName, string dotnetEventHandlerName)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("addEventHandler", dotnetObj, eventName, dotnetEventHandlerName);
    }
    public async ValueTask AddEventHandler(string eventName, string storedFunctionKey)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("addEventHandlerWithFunctionKey", eventName, storedFunctionKey);
    }

    public async ValueTask RemoveEventHandler(string eventName, string storedFunctionKey)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("removeEventHandlerWithFunctionKey", eventName, storedFunctionKey);
    }


    public async ValueTask AddEventHandlers(object dotnetObj, ElementReference targetElement, string[] eventNames)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("addEventHandlers", dotnetObj, targetElement, eventNames);
    }
    public async ValueTask AddEventHandlersWithElementId(object dotnetObj, string targetElementId, string[] eventNames)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("addEventHandlersWithElementId", dotnetObj, targetElementId, eventNames);
    }

    public async ValueTask EnableDebugLog(bool enable)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("enableDebugLog", enable);
    }

    /// <summary>
    /// Console.log 와 동일한 기능을 수행하나, 설정에 따라 log 를 출력하지 않을 수 있음
    /// </summary>
    public async ValueTask Debug(string message)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("debug", message);
    }

    /// <summary>
    /// Console.log 와 동일한 기능
    /// </summary>
    public async ValueTask Info(string message)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("info", message);
    }

    /// <summary>
    /// Console.log 와 동일한 기능을 수행하나, 색상을 달리해서 표현(yellow)
    /// </summary>
    public async ValueTask Warn(string message)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("warn", message);
    }
    public async ValueTask Error(string message)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("error", message);
    }

    public async ValueTask OpenNewWindow(string url)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("openWindow", url);
    }

    public async ValueTask<bool> OpenFullscreen(string fullScreenElementId)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<bool>("openFullscreen", fullScreenElementId);
    }
    public async ValueTask<bool> CloseFullscreen()
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<bool>("closeFullscreen");
    }
    public async ValueTask GoFullScreen(string targetElementId)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("goFullScreen", targetElementId);
    }


    public async ValueTask<ElementReference> GetElementById(string id)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<ElementReference>("getElementById", id);
    }

    public async ValueTask<string> GetTagName(ElementReference element)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<string>("getTagName", element);
    }

    /// <summary>
    /// Runtime 에 필요한 script 를 등록하기 위해 eval 을 사용.
    /// 임의의 사용자 입력 코드는 보안상 실행하게 두면 안됨    
    /// </summary>
    public async ValueTask Eval(string snippet)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("evalSnippet", snippet);
    }

    /// <summary>
    /// Runtime 에 필요한 script 를 등록하기 위해 eval 을 사용.
    /// - IJsRuntime global module 상에서 eval 수행.
    /// - 임의의 사용자 입력 코드는 보안상 실행하게 두면 안됨    
    /// </summary>
    public async ValueTask EvalOnGlobalModule(string snippet)
    {
        await _jsRuntime.InvokeVoidAsync("eval", snippet);
    }
    public async ValueTask<bool> IsFunctionExists(string functionName)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<bool>("isFunctionExists", functionName);
    }



    /// <summary>
    /// javascript 를 loading 해서 평가.
    /// - 매번 재 평가가 필요한 javascript 를 호출 할 때 사용.  1회 loading 으로 충분하면 LoadScript 를 사용
    /// </summary>
    public async ValueTask EvalScript(string url)
    {
        var snippet = @"
            if (typeof window.evalScript === 'undefined') {
                    window.evalScript = function (url) {
                        return fetch(url)
                            .then(response => response.text())
                            .then(text => {
                                eval(text);
                                return text;
                            })
                            .catch(error => {
                                console.error('Error loading script:', error);
                            });
                    };
            }
            ";
        await _jsRuntime.InvokeVoidAsync("eval", snippet);
        await _jsRuntime.InvokeVoidAsync("evalScript", url);
    }


    /// <summary>
    /// 외부 javascript 를 loading.
    /// - 외부 javascript loading 을 통해서 HTML 요소 내에서의 script src="http://...*.js" 태그를 사용하는 것과 동일한 효과
    /// - 동적으로 필요한 script loading 이 가능해 짐.
    /// - Enable 후에는, await JsRuntime.InvokeVoidAsync("loadScript", "https://cdnjs.cloudflare.com/ajax/libs/cytoscape/2.3.15/cytoscape.js"); 등과 같은 형태로 사용
    /// - JsDual.LoadScript() 등과 같은 형태로는 구성 실패..
    /// </summary>
    public async ValueTask LoadScript(string url)
    {
        var snippet = @"
            if (typeof window.loadScript === 'undefined') {
                window.loadScript = (url) => {
                    return new Promise((resolve, reject) => {
                        if (document.querySelector(`script[src='${url}']`)) {
                            console.log(`Reusing exsisting script source url: ${url}`)
                            resolve();
                            return;
                        }

                        const script = document.createElement('script');
                        script.src = url;
                        script.onload = () => resolve();
                        script.onerror = () => {
                                console.error(`Script load error for ${url}`)
                                reject(new Error(`Script load error for ${url}`));
                        }
                        document.head.appendChild(script);
                    });
                };
            }
            ";
        await _jsRuntime.InvokeVoidAsync("eval", snippet);

        await _jsRuntime.InvokeVoidAsync("loadScript", url);
    }


    public async ValueTask SetInnerText(string elementId, string newText)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("setInnerTextById", elementId, newText);
    }


    public async ValueTask MoveToBody(string elementId)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("moveToBody", elementId);
    }

    public async ValueTask Move(string elementId, string newParentId)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("move", elementId, newParentId);
    }






    public async ValueTask AddElementClass(string elementId, string className)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("addElementClass", elementId, className);
    }
    public async ValueTask RemoveElementClass(string elementId, string className)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("removeElementClass", elementId, className);
    }
    public async ValueTask ReplaceElementClass(string elementId, string fromClass, string toClass)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("replaceElementClass", elementId, fromClass, toClass);
    }

    public async ValueTask<string> GetCssVariableValue(string variableName)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<string>("getCssVariableValue", variableName);
    }
    public async ValueTask<bool> ExistsElementWithId(string elementId)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<bool>("existsElementWithId", elementId);
    }




    public async ValueTask SetStyle(ElementReference element, string attrKey, string attrValue)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("setStyle", new object[] { element, attrKey, attrValue });
    }
    public async ValueTask SetStyle(string elementId, string attrKey, string attrValue)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("setStyleWithElementId", new object[] { elementId, attrKey, attrValue });
    }



    public async ValueTask<object> GetStyle(ElementReference element, string attrKey)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<object>("getStyle", new object[] { element, attrKey });
    }
    public async ValueTask<object> GetStyle(string elementId, string attrKey)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<object>("getStyleWithElementId", new object[] { elementId, attrKey });
    }


    public async ValueTask SetAttribute(ElementReference element, string attrKey, object attrValue)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("setAttribute", new object[] { element, attrKey, attrValue });
    }


    public async ValueTask SetAttribute(string id, string attrKey, object attrValue)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("setAttributeWithElementId", new object[] { id, attrKey, attrValue });
    }

    public async ValueTask<object> GetAttribute(ElementReference element, string attrKey)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<object>("getAttribute", new object[] { element, attrKey });
    }
    public async ValueTask<object> GetAttribute(string elementId, string attrKey)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<object>("getAttributeWithElementId", new object[] { elementId, attrKey });
    }


    public async ValueTask<Rect> GetImageContentOffset(string imageDivId)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<Rect>("getImageContentOffset", imageDivId);
    }

    public async ValueTask StartWebcam(string canvasElementId, string videoElementId)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("startWebcam", canvasElementId, videoElementId);
    }
    public async ValueTask StartVideo(object canvasHelper, ElementReference canvasElement, ElementReference videoElement)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("startVideo", canvasHelper, canvasElement, videoElement);
    }

    public async ValueTask ShowTooltip(string tooltipId, double x, double y, string message)
    {
        var module = await moduleTask.Value;
        await Console.Out.WriteLineAsync($"ShowTooltip({tooltipId}, {x}, {y}, {message})");
        await module.InvokeVoidAsync("showTooltipWithElementId", tooltipId, x, y, message);
    }
    public async ValueTask HideTooltip(string tooltipId)
    {
        var module = await moduleTask.Value;
        await Console.Out.WriteLineAsync($"HideTooltip({tooltipId})");
        await module.InvokeVoidAsync("hideTooltipWithElementId", tooltipId);
    }

    public async ValueTask ShowTheTooltip(double x, double y, string message)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("showTheTooltip", x, y, message);
    }
    public async ValueTask HideTheTooltip()
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("hideTheTooltip");
    }

    public async ValueTask ShowPopup(string elementId, double x, double y)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("showPopup");
    }
    public async ValueTask HidePopup(string elementId)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("hidePopup");
    }



    public async ValueTask AttachTheTooltipEvents(string targetId, string message)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("attachTheTooltipEvents", targetId, message);
    }

    public async ValueTask AttachTooltipEvents(string targetId, string tooltipId)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("attachTooltipEvents", targetId, tooltipId);
    }

    public async ValueTask SetDivDisplay(string tooltipDivId, string display)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("setDivDisplay", tooltipDivId, display);
    }

    
    public async ValueTask ChangeTheme(string theme)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("changeTheme", theme);
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated)
        {
            var module = await moduleTask.Value;
            await module.DisposeAsync();
        }
    }

}
