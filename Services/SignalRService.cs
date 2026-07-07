// Services/SignalRService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ProGlassAutomation.Services
{
    /// <summary>
    /// SignalR service for real-time WebSocket communication
    /// </summary>
    public class SignalRService : IDisposable
    {
        #region Events

        public event EventHandler<LiveDataMessage> OnMessageReceived;
        public event EventHandler<bool> OnConnectionStateChanged;
        public event EventHandler<string> OnError;

        #endregion

        #region Fields

        private readonly DispatcherTimer _reconnectTimer;
        private readonly List<LiveDataMessage> _messageBuffer = new();
        private bool _isConnected;
        private bool _isConnecting;
        private bool _isDisposed;
        private string _hubUrl = "http://localhost:5000/liveHub";
        private int _reconnectAttempts;
        private const int MaxReconnectAttempts = 5;

        #endregion

        #region Properties

        public bool IsConnected => _isConnected;
        public string HubUrl
        {
            get => _hubUrl;
            set => _hubUrl = value;
        }

        #endregion

        #region Constructor

        public SignalRService()
        {
            _reconnectTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
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
                System.Diagnostics.Debug.WriteLine($"[SignalR] Connecting to {_hubUrl}...");

                // Simulate connection (replace with actual Microsoft.AspNetCore.SignalR.Client)
                await SimulateConnectionAsync();

                _isConnected = true;
                _reconnectAttempts = 0;
                OnConnectionStateChanged?.Invoke(this, true);

                System.Diagnostics.Debug.WriteLine("[SignalR] Connected successfully");

                // Start heartbeat
                _reconnectTimer.Start();

                // Send buffered messages
                await SendBufferedMessagesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Connection error: {ex.Message}");
                OnError?.Invoke(this, ex.Message);
                OnConnectionStateChanged?.Invoke(this, false);

                // Schedule reconnect
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
                _reconnectTimer.Stop();
                _isConnected = false;
                OnConnectionStateChanged?.Invoke(this, false);
                System.Diagnostics.Debug.WriteLine("[SignalR] Disconnected");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Disconnect error: {ex.Message}");
            }
        }

        private async Task SimulateConnectionAsync()
        {
            // Simulate network delay
            await Task.Delay(500);

            // In production, use actual SignalR client:
            // var connection = new HubConnectionBuilder()
            //     .WithUrl(_hubUrl)
            //     .WithAutomaticReconnect()
            //     .Build();
            // await connection.StartAsync();
        }

        #endregion

        #region Message Broadcasting

        public async Task BroadcastProductionUpdateAsync(double production)
        {
            var message = new LiveDataMessage
            {
                Type = "ProductionUpdate",
                Production = production,
                Timestamp = DateTime.Now
            };

            await SendMessageAsync(message);
        }

        public async Task BroadcastEfficiencyUpdateAsync(double efficiency)
        {
            var message = new LiveDataMessage
            {
                Type = "EfficiencyUpdate",
                Efficiency = efficiency,
                Timestamp = DateTime.Now
            };

            await SendMessageAsync(message);
        }

        public async Task BroadcastModuleStatusAsync(string moduleName, bool isActive)
        {
            var message = new LiveDataMessage
            {
                Type = "ModuleStatusChange",
                ModuleName = moduleName,
                IsActive = isActive,
                Timestamp = DateTime.Now
            };

            await SendMessageAsync(message);
        }

        public async Task BroadcastLiveCounterAsync(string moduleName, int count)
        {
            var message = new LiveDataMessage
            {
                Type = "LiveCounterUpdate",
                ModuleName = moduleName,
                LiveCount = count,
                Timestamp = DateTime.Now
            };

            await SendMessageAsync(message);
        }

        public async Task BroadcastLicenseChangeAsync(bool isLicensed, int remainingDays)
        {
            var message = new LiveDataMessage
            {
                Type = "LicenseChange",
                IsLicensed = isLicensed,
                RemainingDays = remainingDays,
                Timestamp = DateTime.Now
            };

            await SendMessageAsync(message);
        }

        public async Task BroadcastMetricsAsync(LiveMetrics metrics)
        {
            var message = new LiveDataMessage
            {
                Type = "MetricsUpdate",
                Production = metrics.TotalProduction,
                Efficiency = metrics.Efficiency,
                Uptime = metrics.Uptime,
                Timestamp = DateTime.Now
            };

            await SendMessageAsync(message);
        }

        private async Task SendMessageAsync(LiveDataMessage message)
        {
            if (_isDisposed) return;

            if (_isConnected)
            {
                try
                {
                    // In production, invoke hub method:
                    // await _hubConnection.InvokeAsync("BroadcastMessage", message);

                    System.Diagnostics.Debug.WriteLine($"[SignalR] Sent: {message.Type}");
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SignalR] Send error: {ex.Message}");
                    BufferMessage(message);
                }
            }
            else
            {
                BufferMessage(message);
            }
        }

        private void BufferMessage(LiveDataMessage message)
        {
            if (_messageBuffer.Count < 100) // Limit buffer size
            {
                _messageBuffer.Add(message);
            }
        }

        private async Task SendBufferedMessagesAsync()
        {
            if (!_isConnected || _messageBuffer.Count == 0) return;

            var messagesToSend = new List<LiveDataMessage>(_messageBuffer);
            _messageBuffer.Clear();

            foreach (var message in messagesToSend)
            {
                await SendMessageAsync(message);
            }
        }

        #endregion

        #region Message Receiving

        public void HandleReceivedMessage(LiveDataMessage message)
        {
            if (_isDisposed) return;

            System.Diagnostics.Debug.WriteLine($"[SignalR] Received: {message.Type}");
            OnMessageReceived?.Invoke(this, message);
        }

        #endregion

        #region Reconnection

        private void ReconnectTimer_Tick(object sender, EventArgs e)
        {
            if (!_isConnected && _reconnectAttempts < MaxReconnectAttempts)
            {
                _ = ConnectAsync();
                _reconnectAttempts++;
            }
        }

        private void ScheduleReconnect()
        {
            if (_reconnectAttempts < MaxReconnectAttempts)
            {
                _reconnectTimer.Interval = TimeSpan.FromSeconds(Math.Pow(2, _reconnectAttempts) * 5);
                _reconnectTimer.Start();
                _reconnectAttempts++;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _reconnectTimer.Stop();
            _reconnectTimer.Tick -= ReconnectTimer_Tick;
            _messageBuffer.Clear();

            System.Diagnostics.Debug.WriteLine("[SignalR] Disposed");
        }

        #endregion
    }

    #region Live Data Message Model

    public class LiveDataMessage
    {
        public string? Type { get; set; }
        public double Production { get; set; }
        public double Efficiency { get; set; }
        public double Uptime { get; set; }
        public string? ModuleName { get; set; }
        public bool IsActive { get; set; }
        public bool IsLicensed { get; set; }
        public int LiveCount { get; set; }
        public int RemainingDays { get; set; }
        public DateTime Timestamp { get; set; }
        public string? SenderId { get; set; }
    }

    #endregion
}