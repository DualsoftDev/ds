

using Dual.Common.Core;
using Emgu.CV.Ocl;
using Engine.Core;
using Engine.Runtime;
using System.Net.WebSockets;
using RestResultString = Dual.Web.Blazor.Shared.RestResult<string>;
using ResultSArray = Dual.Web.Blazor.Shared.RestResult<string[]>;
using DevExpress.Pdf.Native.BouncyCastle.Asn1;

namespace DsWebApp.Server.Controllers;



/// <summary>
/// Streaming controller.  GetScreens/GetViewTypes
/// </summary>
[ApiController]
[Route("api/[controller]")]
//[Authorize(Roles = "Administrator")]
public class StreamingController(ServerGlobal global) : ControllerBaseWithLogger(global.Logger)
{
    RuntimeModel _model => global.RuntimeModel;
    Dictionary<string, WebSocket> _dicWebSocket => Dict.DicWebSocket;
   [HttpGet("screens")]
    public ResultSArray GetScreens()
    {
        if (_model == null)
            return ResultSArray.Err("RuntimeModel is not uploaded");

        return ResultSArray.Ok(_model.DsStreaming.DsLayout.GetServerChannels().ToArray());
    }
    [HttpGet("viewmodes")]
    public ResultSArray GetViewTypes()
    {
        if (_model == null)
            return ResultSArray.Err("RuntimeModel is not uploaded");

        return ResultSArray.Ok(_model.DsStreaming.DsLayout.GetViewTypeList().ToArray());
    }
    [HttpGet("streamstart")]
    public async Task<RestResultString> GetStreamAsync(string clientGuid, string channel, string viewmode)
    {
        if (_model == null)
            return RestResultString.Err("RuntimeModel is not uploaded");

        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            var clientKey = $"{clientGuid}";
            var webSocket = _dicWebSocket.ContainsKey(clientKey) ? _dicWebSocket[clientKey] : null;

            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                Console.WriteLine("Abort previous WebSocket...");
                webSocket.Abort();
                _dicWebSocket.Remove(clientKey);
            }

            webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            _dicWebSocket[clientKey] = webSocket;

            await _model.DsStreaming.ImageStreaming(webSocket, channel, viewmode, clientGuid);
        }

        return RestResultString.Ok("streamstart ok");
    }

}

public static class Dict
{
    public static Dictionary<string, WebSocket> DicWebSocket = new Dictionary<string, WebSocket>();
}

