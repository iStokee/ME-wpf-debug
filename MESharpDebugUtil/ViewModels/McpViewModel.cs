using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using MESharp.Commands;
using MESharp.Services;

namespace MESharp.ViewModels
{
    public sealed class McpActivityRow
    {
        public string Time { get; init; } = string.Empty;
        public string Command { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public bool Ok { get; init; }
        public string Duration { get; init; } = string.Empty;
        public string Error { get; init; } = string.Empty;
    }

    public sealed class McpCommandStatRow
    {
        public string Command { get; init; } = string.Empty;
        public string Count { get; init; } = string.Empty;
        public string Failed { get; init; } = string.Empty;
        public string AvgMs { get; init; } = string.Empty;
        public string Last { get; init; } = string.Empty;
    }

    public sealed class McpToolRow
    {
        public string Name { get; init; } = string.Empty;
        public string Kind { get; init; } = string.Empty;
        public string Safety { get; init; } = string.Empty;
        public string Login { get; init; } = string.Empty;
        public string Mutates { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
    }

    /// <summary>
    /// Live MCP bridge diagnostics page: listener status, request activity, and tool catalog.
    /// Reads the in-process <see cref="McpDiagnostics"/> hub maintained by McpRuntimeService.
    /// </summary>
    public class McpViewModel : BaseViewModel, IActivatableViewModel, IDisposable
    {
        private readonly DispatcherTimer _timer;
        private IReadOnlyList<McpToolInfo> _toolCatalog = Array.Empty<McpToolInfo>();
        private long _lastSeenSequence = -1;
        private bool _activityFilterDirty = true;

        public McpViewModel()
        {
            RefreshCommand = new RelayCommand(_ => RefreshAll());
            ResetCountersCommand = new RelayCommand(_ =>
            {
                McpDiagnostics.ResetCounters();
                _lastSeenSequence = -1;
                _activityFilterDirty = true;
                RefreshAll();
                StatusMessage = "Counters reset.";
            });
            CopyConfigCommand = new RelayCommand(_ => CopyConnectSnippet());
            OpenDashboardCommand = new RelayCommand(_ => OpenFullDashboard());

            _timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (_, _) =>
            {
                if (AutoRefresh)
                {
                    RefreshAll();
                }
            };

            LoadToolCatalog();
        }

        public ICommand RefreshCommand { get; }
        public ICommand ResetCountersCommand { get; }
        public ICommand CopyConfigCommand { get; }
        public ICommand OpenDashboardCommand { get; }

        // ── Header / status ───────────────────────────────────────────────────

        private bool _isListening;
        public bool IsListening { get => _isListening; private set => SetProperty(ref _isListening, value); }

        private string _statusText = "Bridge stopped";
        public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }

        private string _clientText = "no client";
        public string ClientText { get => _clientText; private set => SetProperty(ref _clientText, value); }

        private bool _clientConnected;
        public bool ClientConnected { get => _clientConnected; private set => SetProperty(ref _clientConnected, value); }

        private bool _autoRefresh = true;
        public bool AutoRefresh { get => _autoRefresh; set => SetProperty(ref _autoRefresh, value); }

        private string _statusMessage = string.Empty;
        public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }

        // ── Stat values ───────────────────────────────────────────────────────

        private string _totalRequests = "–";
        public string TotalRequests { get => _totalRequests; private set => SetProperty(ref _totalRequests, value); }

        private string _failedRequests = "–";
        public string FailedRequests { get => _failedRequests; private set => SetProperty(ref _failedRequests, value); }

        private bool _hasFailures;
        public bool HasFailures { get => _hasFailures; private set => SetProperty(ref _hasFailures, value); }

        private string _avgDuration = "–";
        public string AvgDuration { get => _avgDuration; private set => SetProperty(ref _avgDuration, value); }

        private string _connections = "–";
        public string Connections { get => _connections; private set => SetProperty(ref _connections, value); }

        private string _uptime = "–";
        public string Uptime { get => _uptime; private set => SetProperty(ref _uptime, value); }

        private string _lastCommand = "–";
        public string LastCommand { get => _lastCommand; private set => SetProperty(ref _lastCommand, value); }

        // ── Bridge / config info ──────────────────────────────────────────────

        private string _pipeName = "–";
        public string PipeName { get => _pipeName; private set => SetProperty(ref _pipeName, value); }

        private string _ownerText = "–";
        public string OwnerText { get => _ownerText; private set => SetProperty(ref _ownerText, value); }

        private string _listeningSince = "–";
        public string ListeningSince { get => _listeningSince; private set => SetProperty(ref _listeningSince, value); }

        private string _autoStartText = "–";
        public string AutoStartText { get => _autoStartText; private set => SetProperty(ref _autoStartText, value); }

        private string _serverPathText = "–";
        public string ServerPathText { get => _serverPathText; private set => SetProperty(ref _serverPathText, value); }

        private bool _serverFound;
        public bool ServerFound { get => _serverFound; private set => SetProperty(ref _serverFound, value); }

        private string _gameApiVersion = "–";
        public string GameApiVersion { get => _gameApiVersion; private set => SetProperty(ref _gameApiVersion, value); }

        private string _catalogSummary = "–";
        public string CatalogSummary { get => _catalogSummary; private set => SetProperty(ref _catalogSummary, value); }

        // ── Lists / filters ───────────────────────────────────────────────────

        private IReadOnlyList<McpActivityRow> _activity = Array.Empty<McpActivityRow>();
        public IReadOnlyList<McpActivityRow> Activity { get => _activity; private set => SetProperty(ref _activity, value); }

        private IReadOnlyList<McpCommandStatRow> _commandStats = Array.Empty<McpCommandStatRow>();
        public IReadOnlyList<McpCommandStatRow> CommandStats { get => _commandStats; private set => SetProperty(ref _commandStats, value); }

        private IReadOnlyList<McpToolRow> _tools = Array.Empty<McpToolRow>();
        public IReadOnlyList<McpToolRow> Tools { get => _tools; private set => SetProperty(ref _tools, value); }

        private string _activityFilter = string.Empty;
        public string ActivityFilter
        {
            get => _activityFilter;
            set
            {
                if (SetProperty(ref _activityFilter, value))
                {
                    _activityFilterDirty = true;
                    RefreshActivity();
                }
            }
        }

        private bool _errorsOnly;
        public bool ErrorsOnly
        {
            get => _errorsOnly;
            set
            {
                if (SetProperty(ref _errorsOnly, value))
                {
                    _activityFilterDirty = true;
                    RefreshActivity();
                }
            }
        }

        private string _toolSearch = string.Empty;
        public string ToolSearch
        {
            get => _toolSearch;
            set
            {
                if (SetProperty(ref _toolSearch, value))
                {
                    RefreshTools();
                }
            }
        }

        private string _toolsCountText = string.Empty;
        public string ToolsCountText { get => _toolsCountText; private set => SetProperty(ref _toolsCountText, value); }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public void OnActivated()
        {
            RefreshAll();
            _timer.Start();
        }

        public void OnDeactivated()
        {
            _timer.Stop();
        }

        public void Dispose()
        {
            _timer.Stop();
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        private void RefreshAll()
        {
            var snapshot = McpDiagnostics.GetSnapshot();
            var listenerKnown = snapshot.ListenerActive || McpRuntimeService.HasActiveListener;

            IsListening = listenerKnown;
            StatusText = listenerKnown
                ? $"Listening · {snapshot.PipeName ?? $"MESharpMcpBridge.{Environment.ProcessId}"}"
                : "Bridge stopped";

            ClientConnected = snapshot.ClientConnected;
            ClientText = snapshot.ClientConnected
                ? $"client connected since {snapshot.LastClientConnectedUtc:HH:mm:ss} UTC"
                : snapshot.LastClientDisconnectedUtc.HasValue
                    ? $"client disconnected at {snapshot.LastClientDisconnectedUtc:HH:mm:ss} UTC"
                    : "no client has connected yet";

            TotalRequests = snapshot.TotalRequests.ToString();
            FailedRequests = snapshot.FailedRequests.ToString();
            HasFailures = snapshot.FailedRequests > 0;
            AvgDuration = snapshot.TotalRequests > 0 ? $"{snapshot.AverageDurationMs} ms" : "–";
            Connections = snapshot.TotalConnections.ToString();
            Uptime = snapshot.ListenerActive && snapshot.ListenerStartedUtc.HasValue
                ? FormatDuration(DateTime.UtcNow - snapshot.ListenerStartedUtc.Value)
                : "–";
            LastCommand = snapshot.LastCommand ?? "–";

            PipeName = snapshot.PipeName ?? $"MESharpMcpBridge.{Environment.ProcessId} (expected)";
            OwnerText = listenerKnown ? "runtime service / bridge script / dashboard" : "–";
            ListeningSince = snapshot.ListenerActive && snapshot.ListenerStartedUtc.HasValue
                ? $"{snapshot.ListenerStartedUtc:yyyy-MM-dd HH:mm:ss} UTC"
                : "–";

            try
            {
                var autostart = ServiceRegistry.GetMcpAutoStartEnabled();
                AutoStartText = autostart ? "enabled (MCP_AUTOSTART)" : "disabled (MCP_AUTOSTART=false)";

                var serverPath = ServiceRegistry.GetMcpServerPath();
                ServerFound = !string.IsNullOrWhiteSpace(serverPath);
                ServerPathText = ServerFound ? serverPath : "MESharp.McpServer.exe not found next to csharp_interop.dll";
            }
            catch (Exception ex)
            {
                AutoStartText = $"unavailable: {ex.Message}";
            }

            try
            {
                GameApiVersion = MESharp.API.Game.Version;
            }
            catch
            {
                GameApiVersion = "unavailable (not injected?)";
            }

            RefreshActivity();
        }

        private void RefreshActivity()
        {
            var calls = McpDiagnostics.GetRecentCalls();
            var newestSequence = calls.Count > 0 ? calls[0].Sequence : 0;
            if (!_activityFilterDirty && newestSequence == _lastSeenSequence)
            {
                return;
            }

            _lastSeenSequence = newestSequence;
            _activityFilterDirty = false;

            var filter = ActivityFilter.Trim();
            IEnumerable<McpCallRecord> filtered = calls;
            if (ErrorsOnly)
            {
                filtered = filtered.Where(call => !call.Ok);
            }

            if (filter.Length > 0)
            {
                filtered = filtered.Where(call =>
                    call.Command.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    (call.ErrorMessage?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            Activity = filtered
                .Select(call => new McpActivityRow
                {
                    Time = call.TimestampUtc.ToLocalTime().ToString("HH:mm:ss"),
                    Command = call.Command,
                    Status = call.Ok ? "✓ ok" : "✗ fail",
                    Ok = call.Ok,
                    Duration = call.DurationMs.ToString(),
                    Error = call.Ok ? string.Empty : $"{call.ErrorCode}: {call.ErrorMessage}"
                })
                .ToList();

            CommandStats = McpDiagnostics.GetCommandStats()
                .Select(stat => new McpCommandStatRow
                {
                    Command = stat.Command,
                    Count = stat.Count.ToString(),
                    Failed = stat.FailureCount > 0 ? stat.FailureCount.ToString() : string.Empty,
                    AvgMs = stat.AverageDurationMs.ToString(),
                    Last = stat.LastCalledUtc.ToLocalTime().ToString("HH:mm:ss")
                })
                .ToList();
        }

        private void LoadToolCatalog()
        {
            try
            {
                _toolCatalog = McpRuntimeService.GetToolCatalog();
                var categories = _toolCatalog.Select(tool => tool.Kind).Distinct().Count();
                CatalogSummary = $"{_toolCatalog.Count} tools across {categories} categories";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load tool catalog: {ex.Message}";
                return;
            }

            RefreshTools();
        }

        private void RefreshTools()
        {
            var search = ToolSearch.Trim();
            var filtered = _toolCatalog
                .Where(tool => search.Length == 0 ||
                               tool.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                               tool.Kind.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                               tool.Description.Contains(search, StringComparison.OrdinalIgnoreCase))
                .Select(tool => new McpToolRow
                {
                    Name = tool.Name,
                    Kind = tool.Kind,
                    Safety = tool.Safety,
                    Login = tool.RequiresLogin ? "yes" : string.Empty,
                    Mutates = tool.MutatesGame ? "yes" : string.Empty,
                    Description = tool.Description
                })
                .ToList();

            Tools = filtered;
            ToolsCountText = $"{filtered.Count} / {_toolCatalog.Count}";
        }

        // ── Actions ───────────────────────────────────────────────────────────

        private void CopyConnectSnippet()
        {
            string serverPath;
            try
            {
                serverPath = ServiceRegistry.GetMcpServerPath();
            }
            catch
            {
                serverPath = string.Empty;
            }

            var path = string.IsNullOrWhiteSpace(serverPath) ? "<path-to>\\MESharp.McpServer.exe" : serverPath;
            var escaped = path.Replace("\\", "\\\\");
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"mcpServers\": {");
            builder.AppendLine("    \"mesharp\": {");
            builder.AppendLine($"      \"command\": \"{escaped}\",");
            builder.AppendLine("      \"env\": {");
            builder.AppendLine($"        \"MESHARP_SESSION_PID\": \"{Environment.ProcessId}\"");
            builder.AppendLine("      }");
            builder.AppendLine("    }");
            builder.AppendLine("  }");
            builder.Append('}');

            try
            {
                Clipboard.SetText(builder.ToString());
                StatusMessage = "Copied MCP client config to clipboard.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Clipboard failed: {ex.Message}";
            }
        }

        private void OpenFullDashboard()
        {
            try
            {
                // The full dashboard is now a standalone, live-updatable tool (MESharpMcpTool);
                // launch it through the unified tool launcher instead of an in-process host.
                var launched = MESharp.Services.Tools.ToolUpdater.Launch("MESharpMcpTool");
                StatusMessage = launched
                    ? "Opened full MCP dashboard window."
                    : "Could not open dashboard (is MESharpMcpTool.dll deployed to CSharp_scripts?).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to open dashboard: {ex.Message}";
            }
        }

        private static string FormatDuration(TimeSpan span)
        {
            if (span.TotalHours >= 1)
            {
                return $"{(int)span.TotalHours}h {span.Minutes:00}m";
            }

            return span.TotalMinutes >= 1 ? $"{span.Minutes}m {span.Seconds:00}s" : $"{span.Seconds}s";
        }
    }
}
