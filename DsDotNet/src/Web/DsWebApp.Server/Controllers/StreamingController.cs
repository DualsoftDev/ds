

using Dual.Common.Core;
using Engine.Runtime;
using System.Net.WebSockets;
using ResultSS = Dual.Common.Core.ResultSerializable<string, string>;

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
    public ResultSS GetScreens()
    {
        return ResultSS.Ok(_model.DsStreaming.DsLayout.GetServerChannels().JoinString(";"));
    }
    [HttpGet("viewmodes")]
    public ResultSS GetViewTypes()
    {
        return ResultSS.Ok(_model.DsStreaming.DsLayout.GetViewTypeList().JoinString(";"));
    }

    [HttpGet("stream")]
    public async Task<ResultSS> GetStreamAsync([FromServices] WebSocketManager webSocketManager, string clientGuid, string channel, string viewmode)
    {
        if (webSocketManager.IsWebSocketRequest)
        {
            var clientKey = $"{clientGuid};{channel}";

            if (_dicWebSocket.ContainsKey(clientKey))
            {
                var existingWebSocket = _dicWebSocket[clientKey];
                await existingWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconnecting", CancellationToken.None);
                _dicWebSocket.Remove(clientKey);
            }

            var webSocket = _dicWebSocket.ContainsKey(clientKey) ? _dicWebSocket[clientKey] : null;

            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                Console.WriteLine("Closing previous WebSocket...");
                webSocket.Abort();
            }

            webSocket = await webSocketManager.AcceptWebSocketAsync();
            _dicWebSocket[clientKey] = webSocket;

            await _model.DsStreaming.ImageStreaming(webSocket, channel, viewmode, clientGuid);
        }

        return ResultSS.Ok("WebSocket stream handling logic needs to be implemented here.");
    }
}
