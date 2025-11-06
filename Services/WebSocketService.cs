using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SolanaPumpTracker.Utils;

namespace SolanaPumpTracker.Services
{
    public sealed class WebSocketService : IDisposable
    {
        private ClientWebSocket? _ws;
        private CancellationTokenSource? _loopCts;

        public event Action<string>? MessageReceived;
        public event Action<bool, string>? ConnectionChanged;

        public async Task ConnectAsync(Uri uri, string apiKey, CancellationToken ct)
        {
            await DisconnectAsync();

            _ws = new ClientWebSocket();
            _ws.Options.SetRequestHeader("X-API-Key", apiKey);

            await _ws.ConnectAsync(uri, ct);
            ConnectionChanged?.Invoke(true, "connected");

            _loopCts = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveLoopAsync(_loopCts.Token));
        }

        public async Task SendTextAsync(string text, CancellationToken ct)
        {
            if (_ws == null || _ws.State != WebSocketState.Open) return;
            var bytes = Encoding.UTF8.GetBytes(text);
            await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
        }

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            if (_ws == null) return;
            var buffer = new byte[1024 * 64];
            var sb = new StringBuilder();

            try
            {
                while (!ct.IsCancellationRequested && _ws.State == WebSocketState.Open)
                {
                    sb.Clear();
                    WebSocketReceiveResult? res;
                    do
                    {
                        res = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                        if (res.MessageType == WebSocketMessageType.Close)
                        {
                            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", ct);
                            ConnectionChanged?.Invoke(false, "server closed");
                            return;
                        }
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, res.Count));
                    } while (!res.EndOfMessage);

                    var msg = sb.ToString();
                    SimpleLog.Info("WS RX: " + (msg.Length > 512 ? msg[..512] + "...(cut)" : msg));
                    MessageReceived?.Invoke(msg);
                }
            }
            catch (Exception ex)
            {
                SimpleLog.Info("WS loop error: " + ex.Message);
                ConnectionChanged?.Invoke(false, ex.Message);
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _loopCts?.Cancel();
                if (_ws != null)
                {
                    if (_ws.State == WebSocketState.Open)
                    {
                        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                    }
                    _ws.Dispose();
                }
            }
            catch { }
            finally
            {
                _ws = null;
                _loopCts?.Dispose();
                _loopCts = null;
            }
        }

        public void Dispose()
        {
            _loopCts?.Cancel();
            _loopCts?.Dispose();
            _ws?.Dispose();
        }
    }
}
