

using Dual.Common.Core;
using Emgu.CV.Ocl;
using Engine.Core;
using Engine.Runtime;
using System.Net.WebSockets;
using ResultSS = Dual.Common.Core.ResultSerializable<string, string>;
using ResultSArray = Dual.Common.Core.ResultSerializable<string[], string>;

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
    Dictionary<string, WebSocket> _dicWebSocket = new Dictionary<string, WebSocket>();
    [HttpGet("screens")]
    public ResultSArray GetScreens()
    {
        return ResultSArray.Ok(_model.DsStreaming.DsLayout.GetServerChannels().ToArray());

    }
    [HttpGet("viewmodes")]
    public ResultSArray GetViewTypes()
    {
        return ResultSArray.Ok(_model.DsStreaming.DsLayout.GetViewTypeList().ToArray());
    }
    [HttpGet("streamstart")]
    public async Task<ResultSS> GetStreamAsync(string clientGuid, string channel, string viewmode)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            var clientKey = $"{clientGuid};{channel}";
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

        return ResultSS.Ok("streamstart ok");
    }
    
}
