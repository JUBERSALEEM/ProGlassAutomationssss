// Services/WebSocketService.cs
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ProGlassAutomation.Services
{
    /// <summary>
    /// WebSocket service for real-time bidirectional communication
    /// </summary>
    public class WebSocketService : IDisposable
    {
        #region Events

        public event EventHandler<LiveDataMessage> OnMessageReceived;
        public event EventHandler<bool> OnConnectionStateChanged;
        public event EventHandler<string> OnError;
        public event EventHandler<string> OnLogMessage;

        #endregion

        #region Fields

        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly DispatcherTimer _heartbeatTimer;
        private readonly DispatcherTimer _reconnectTimer;
        // Services/WebSocketService.cs (continued)

        private readonly ConcurrentQueue<LiveDataMessage> _messageQueue = new();
        private bool _isConnected;
        private bool _isDisposed;
        private bool _isConnecting;
        private string _serverUrl = "ws://localhost:5000/ws";
        private int _reconnectAttempts;
        private const int MaxReconnectAttempts = 10;
        private const int MaxQueueSize = 1000;

        #endregion

        #region Properties

        public bool IsConnected => _isConnected;
        public string ServerUrl
        {
            get => _serverUrl;
            set => _serverUrl = value;
        }
        public int QueuedMessageCount => _messageQueue.Count;

        #endregion

        #region Constructor

        public WebSocketService()
        {
            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();

            // Heartbeat timer - send ping every 30 seconds
            _heartbeatTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _heartbeatTimer.Tick += HeartbeatTimer_Tick;

            // Reconnect timer - attempt reconnect every 5 seconds
            _reconnectTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _reconnectTimer.Tick += ReconnectTimer_Tick;
        }

        #endregion

        #region Connection Management

        public async Task ConnectAsync()
        {
            if (_isConnecting || _isDisposed) return;

            try
            {
                _isConnecting = true;
                OnLogMessage?.Invoke(this, $"[WebSocket] Connecting to {_serverUrl}...");

                // Create new web socket if needed
                if (_webSocket == null || _webSocket.State != WebSocketState.Closed)
                {
                    _webSocket?.Dispose();
                    _webSocket = new ClientWebSocket();
                }

                // Cancel any existing connection
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();

                // Connect with timeout
                var connectTask = _webSocket.ConnectAsync(
                    new Uri(_serverUrl),
                    _cancellationTokenSource.Token);

                var timeoutTask = Task.Delay(5000, _cancellationTokenSource.Token);

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException("Connection timed out");
                }

                await connectTask; // Ensure original task completes

                _isConnected = true;
                _reconnectAttempts = 0;
                _heartbeatTimer.Start();

                OnConnectionStateChanged?.Invoke(this, true);
                OnLogMessage?.Invoke(this, "[WebSocket] Connected successfully");

                // Start receiving messages
                _ = ReceiveMessagesAsync();

                // Send queued messages
                await SendQueuedMessagesAsync();
            }
            catch (Exception ex)
            {
                _isConnected = false;
                OnConnectionStateChanged?.Invoke(this, false);
                OnError?.Invoke(this, ex.Message);
                OnLogMessage?.Invoke(this, $"[WebSocket] Connection error: {ex.Message}");

                ScheduleReconnect();
            }
            finally
            {
                _isConnecting = false;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_isDisposed) return;

            try
            {
                _heartbeatTimer.Stop();
                _reconnectTimer.Stop();

                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Client disconnect",
                        CancellationToken.None);
                }

                _isConnected = false;
                OnConnectionStateChanged?.Invoke(this, false);
                OnLogMessage?.Invoke(this, "[WebSocket] Disconnected");
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, ex.Message);
            }
        }

        #endregion

        #region Message Sending

        public async Task SendMessageAsync(LiveDataMessage message)
        {
            if (_isDisposed) return;

            message.SenderId = GetClientId();
            message.Timestamp = DateTime.Now;

            if (_isConnected && _webSocket?.State == WebSocketState.Open)
            {
                try
                {
                    var json = SerializeMessage(message);
                    var bytes = Encoding.UTF8.GetBytes(json);
                    var segment = new ArraySegment<byte>(bytes);

                    await _webSocket.SendAsync(
                        segment,
                        WebSocketMessageType.Text,
                        true,
                        _cancellationTokenSource.Token);

                    OnLogMessage?.Invoke(this, $"[WebSocket] Sent: {message.Type}");
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(this, $"Send error: {ex.Message}");
                    QueueMessage(message);
                }
            }
            else
            {
                QueueMessage(message);
            }
        }

        public async Task SendProductionUpdateAsync(double production)
        {
            await SendMessageAsync(new LiveDataMessage
            {
                Type = "ProductionUpdate",
                Production = production,
                Timestamp = DateTime.Now
            });
        }

        public async Task SendEfficiencyUpdateAsync(double efficiency)
        {
            await SendMessageAsync(new LiveDataMessage
            {
                Type = "EfficiencyUpdate",
                Efficiency = efficiency,
                Timestamp = DateTime.Now
            });
        }

        public async Task SendMetricsUpdateAsync(LiveMetrics metrics)
        {
            await SendMessageAsync(new LiveDataMessage
            {
                Type = "MetricsUpdate",
                Production = metrics.TotalProduction,
                Efficiency = metrics.Efficiency,
                Uptime = metrics.Uptime,
                Timestamp = DateTime.Now
            });
        }

        public async Task SendPingAsync()
        {
            await SendMessageAsync(new LiveDataMessage
            {
                Type = "Ping",
                Timestamp = DateTime.Now
            });
        }

        private void QueueMessage(LiveDataMessage message)
        {
            if (_messageQueue.Count < MaxQueueSize)
            {
                _messageQueue.Enqueue(message);
            }
        }

        private async Task SendQueuedMessagesAsync()
        {
            while (_messageQueue.TryDequeue(out var message))
            {
                await SendMessageAsync(message);
                await Task.Delay(50); // Small delay between messages
            }
        }

        #endregion

        #region Message Receiving

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[8192];

            try
            {
                while (_isConnected && _webSocket?.State == WebSocketState.Open)
                {
                    var segment = new ArraySegment<byte>(buffer);
                    var result = await _webSocket.ReceiveAsync(segment, _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        OnLogMessage?.Invoke(this, "[WebSocket] Server closed connection");
                        _isConnected = false;
                        OnConnectionStateChanged?.Invoke(this, false);
                        ScheduleReconnect();
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var messageBytes = new byte[result.Count];
                        Array.Copy(buffer, messageBytes, result.Count);
                        var json = Encoding.UTF8.GetString(messageBytes);

                        var message = DeserializeMessage(json);
                        if (message != null)
                        {
                            OnMessageReceived?.Invoke(this, message);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when disconnecting
            }
            catch (Exception ex)
            {
                if (!_isDisposed)
                {
                    OnError?.Invoke(this, $"Receive error: {ex.Message}");
                    _isConnected = false;
                    OnConnectionStateChanged?.Invoke(this, false);
                    ScheduleReconnect();
                }
            }
        }

        #endregion

        #region Heartbeat & Reconnection

        private void HeartbeatTimer_Tick(object sender, EventArgs e)
        {
            if (_isConnected)
            {
                _ = SendPingAsync();
            }
        }

        private void ReconnectTimer_Tick(object sender, EventArgs e)
        {
            if (!_isConnected && _reconnectAttempts < MaxReconnectAttempts)
            {
                OnLogMessage?.Invoke(this, $"[WebSocket] Reconnect attempt {_reconnectAttempts + 1}...");
                _ = ConnectAsync();
                _reconnectAttempts++;
            }
            else if (_reconnectAttempts >= MaxReconnectAttempts)
            {
                _reconnectTimer.Stop();
                OnError?.Invoke(this, "Max reconnect attempts reached");
            }
        }

        private void ScheduleReconnect()
        {
            if (_reconnectAttempts < MaxReconnectAttempts)
            {
                _reconnectTimer.Interval = TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, _reconnectAttempts) * 5));
                _reconnectTimer.Start();
                _reconnectAttempts++;
            }
        }

        #endregion

        #region Serialization

        private string SerializeMessage(LiveDataMessage message)
        {
            return System.Text.Json.JsonSerializer.Serialize(message, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
        }

        private LiveDataMessage DeserializeMessage(string json)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<LiveDataMessage>(json, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Helpers

        private string GetClientId()
        {
            return Environment.MachineName + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _heartbeatTimer.Stop();
            _reconnectTimer.Stop();
            _cancellationTokenSource?.Cancel();
            _messageQueue.Clear();
            _webSocket?.Dispose();

            System.Diagnostics.Debug.WriteLine("[WebSocket] Disposed");
        }

        #endregion
    }
}