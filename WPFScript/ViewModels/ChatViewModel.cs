using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using System.Windows.Threading;
using MESharp.API;
using MESharp.Commands;

namespace MESharp.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged, IDisposable, IActivatableViewModel
    {
        private const int EventLimit = 300;

        private readonly DispatcherTimer _timer;
        private readonly NotifyCollectionChangedEventHandler _liveEventsChangedHandler;
        private long _eventSequence;
        private bool _disposed;
        private bool _isActive;

        public ObservableCollection<EventItem> LiveEvents { get; } = new();

        public bool HasEvents => LiveEvents.Count > 0;

        private bool _eventsSupported = true;
        public bool EventsSupported
        {
            get => _eventsSupported;
            private set
            {
                if (Set(ref _eventsSupported, value))
                {
                    OnPropertyChanged(nameof(EventsStatusMessage));
                }
            }
        }

        private string _eventsStatusMessage = string.Empty;
        public string EventsStatusMessage
        {
            get => _eventsStatusMessage;
            private set => Set(ref _eventsStatusMessage, value);
        }

        public class EventItem
        {
            public long Sequence { get; set; }
            public string Time { get; set; } = "";
            public string Channel { get; set; } = "";
            public string Names { get; set; } = "";
            public string Text { get; set; } = "";
            public string Details { get; set; } = "";
            public int Tick { get; set; }
        }

        private bool _captureEvents = true;
        public bool CaptureEvents
        {
            get => _captureEvents;
            set
            {
                if (!EventsSupported && value)
                {
                    return;
                }

                if (Set(ref _captureEvents, value) && value)
                {
                    DrainEvents();
                }
            }
        }

        public ICommand ClearEventsCommand { get; }
        public ICommand GetMessagesCommand { get; }
        public ICommand TryFindMessageCommand { get; }
        public ICommand GetFriendChatCommand { get; }
        public ICommand GetPortableTimeCommand { get; }

        // ─── GetMessages Support ─────────────────────────────────────────────
        public ObservableCollection<MessageItem> Messages { get; } = new();
        public bool HasMessages => Messages.Count > 0;

        public class MessageItem
        {
            public string Name { get; set; } = "";
            public string Text { get; set; } = "";
            public string Extra1 { get; set; } = "";
            public string Extra2 { get; set; } = "";
            public string Timestamp { get; set; } = "";
            public string TimeTotal { get; set; } = "";
        }

        // ─── TryFind Support ─────────────────────────────────────────────────
        private string _findMessageText = "";
        public string FindMessageText
        {
            get => _findMessageText;
            set => Set(ref _findMessageText, value);
        }

        private int _findMessageLimit = 100;
        public int FindMessageLimit
        {
            get => _findMessageLimit;
            set => Set(ref _findMessageLimit, value);
        }

        private string _foundMessageResult = "";
        public string FoundMessageResult
        {
            get => _foundMessageResult;
            private set => Set(ref _foundMessageResult, value);
        }

        // ─── FriendChat Support ──────────────────────────────────────────────
        public ObservableCollection<string> FriendChatMembers { get; } = new();
        public bool HasFriendChatMembers => FriendChatMembers.Count > 0;

        // ─── Portable Time Support ───────────────────────────────────────────
        private string _portableTimeRemaining = "Not checked";
        public string PortableTimeRemaining
        {
            get => _portableTimeRemaining;
            private set => Set(ref _portableTimeRemaining, value);
        }

        public ChatViewModel()
        {
            _liveEventsChangedHandler = (_, __) => OnPropertyChanged(nameof(HasEvents));
            LiveEvents.CollectionChanged += _liveEventsChangedHandler;
            Messages.CollectionChanged += (_, __) => OnPropertyChanged(nameof(HasMessages));
            FriendChatMembers.CollectionChanged += (_, __) => OnPropertyChanged(nameof(HasFriendChatMembers));

            RefreshEventSupport();

            _timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(750),
                IsEnabled = false
            };
            _timer.Tick += OnTick;

            ClearEventsCommand = new RelayCommand(_ =>
            {
                LiveEvents.Clear();
                _eventSequence = 0;
            });

            GetMessagesCommand = new RelayCommand(_ => LoadMessages());
            TryFindMessageCommand = new RelayCommand(_ => TryFindMessage());
            GetFriendChatCommand = new RelayCommand(_ => LoadFriendChat());
            GetPortableTimeCommand = new RelayCommand(_ => LoadPortableTime());

            OnActivated();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            if (_disposed)
                return;

            DrainEvents();
        }

        private void RefreshEventSupport()
        {
            if (_disposed)
                return;

            var supported = Chat.SupportsEvents;
            var message = Chat.EventsSupportError ?? string.Empty;

            if (supported != EventsSupported)
            {
                EventsSupported = supported;
                if (!supported && CaptureEvents)
                {
                    CaptureEvents = false;
                }
            }

            if (!string.Equals(EventsStatusMessage, message, StringComparison.Ordinal))
            {
                EventsStatusMessage = message;
            }
        }

        private void DrainEvents()
        {
            if (_disposed)
                return;

            RefreshEventSupport();

            if (!CaptureEvents || !EventsSupported)
                return;

            try
            {
                var events = Chat.DequeueEvents();
                if (events.Length == 0)
                    return;

                foreach (var evt in events)
                {
                    _eventSequence++;
                    LiveEvents.Add(new EventItem
                    {
                        Sequence = _eventSequence,
                        Time = FormatEventTime(evt),
                        Channel = FormatChannel(evt.ChatType),
                        Names = ComposeNames(evt),
                        Text = CleanChatText(evt.Text),
                        Details = BuildDetails(evt),
                        Tick = evt.TickCount
                    });
                }

                while (LiveEvents.Count > EventLimit)
                {
                    LiveEvents.RemoveAt(0);
                }
            }
            catch
            {
                // ignore transient failures (e.g., native layer not initialised yet)
            }
        }

        private static string CleanChatText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var sb = new StringBuilder(text.Length);
            var insideTag = false;

            foreach (var ch in text)
            {
                if (ch == '<')
                {
                    insideTag = true;
                    continue;
                }

                if (insideTag)
                {
                    if (ch == '>')
                        insideTag = false;
                    continue;
                }

                sb.Append(ch);
            }

            return sb.ToString().Trim();
        }

        private static string FormatChannel(string channelRaw)
        {
            var channel = CleanChatText(channelRaw);
            return string.IsNullOrWhiteSpace(channel) ? "(unknown)" : channel;
        }

        private static string FormatEventTime(Chat.ChatEvent evt)
        {
            if (evt.TimestampSeconds > 0)
            {
                try
                {
                    var seconds = Math.Min(evt.TimestampSeconds, (ulong)long.MaxValue);
                    return DateTimeOffset.FromUnixTimeSeconds((long)seconds).ToLocalTime().ToString("HH:mm:ss");
                }
                catch
                {
                    // ignore and fall back
                }
            }

            var formatted = CleanChatText(evt.TimestampFormatted);
            return string.IsNullOrWhiteSpace(formatted)
                ? DateTime.Now.ToString("HH:mm:ss")
                : formatted;
        }

        private static string ComposeNames(Chat.ChatEvent evt)
        {
            var parts = new[] { evt.Name, evt.Name2, evt.Name3 }
                .Select(CleanChatText)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return parts.Length > 0 ? string.Join(" | ", parts) : "(n/a)";
        }

        private void LoadMessages()
        {
            if (_disposed)
                return;

            try
            {
                Messages.Clear();
                var messages = Chat.GetMessages();

                foreach (var msg in messages)
                {
                    Messages.Add(new MessageItem
                    {
                        Name = msg.Name,
                        Text = CleanChatText(msg.Text),
                        Extra1 = msg.Extra1,
                        Extra2 = msg.Extra2,
                        Timestamp = msg.Timestamp.ToString(),
                        TimeTotal = msg.TimeTotal.ToString()
                    });
                }

                EventsStatusMessage = $"Loaded {Messages.Count} messages.";
            }
            catch (Exception ex)
            {
                EventsStatusMessage = $"Error loading messages: {ex.Message}";
            }
        }

        private void TryFindMessage()
        {
            if (_disposed || string.IsNullOrWhiteSpace(FindMessageText))
                return;

            try
            {
                if (Chat.TryFind(FindMessageText, FindMessageLimit, out var found))
                {
                    FoundMessageResult = $"Found: [{found.Name}] {CleanChatText(found.Text)}";
                }
                else
                {
                    FoundMessageResult = "Message not found.";
                }
            }
            catch (Exception ex)
            {
                FoundMessageResult = $"Error: {ex.Message}";
            }
        }

        private void LoadFriendChat()
        {
            if (_disposed)
                return;

            try
            {
                FriendChatMembers.Clear();
                var members = Chat.GetFriendChatList();

                foreach (var member in members)
                {
                    if (!string.IsNullOrWhiteSpace(member))
                        FriendChatMembers.Add(member);
                }

                EventsStatusMessage = $"Loaded {FriendChatMembers.Count} friend chat members.";
            }
            catch (Exception ex)
            {
                EventsStatusMessage = $"Error loading friend chat: {ex.Message}";
            }
        }

        private void LoadPortableTime()
        {
            if (_disposed)
                return;

            try
            {
                var time = Chat.PortableTime();
                PortableTimeRemaining = time > 0 ? $"{time} seconds remaining" : "No portable active";
            }
            catch (Exception ex)
            {
                PortableTimeRemaining = $"Error: {ex.Message}";
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            OnDeactivated();

            _disposed = true;

            try
            {
                _timer.Tick -= OnTick;
            }
            catch { /* ignore */ }

            LiveEvents.CollectionChanged -= _liveEventsChangedHandler;

            _captureEvents = false;

            GC.SuppressFinalize(this);
        }

        private static string BuildDetails(Chat.ChatEvent evt)
        {
            var parts = new List<string>();

            var skillName = CleanChatText(evt.SkillName);
            if (!string.IsNullOrWhiteSpace(skillName))
            {
                var skill = skillName;
                if (evt.SkillIndex != 0)
                    skill += $" ({evt.SkillIndex})";
                if (evt.Experience != 0)
                    skill += $" +{evt.Experience}xp";
                parts.Add(skill);
            }

            if (evt.ItemId != 0 || evt.ItemAmount != 0)
            {
                parts.Add($"Item {evt.ItemId} x{evt.ItemAmount}");
            }

            var prettyTime = CleanChatText(evt.TimestampFormatted);
            if (!string.IsNullOrWhiteSpace(prettyTime))
            {
                parts.Add(prettyTime);
            }

            if (evt.TickCount != 0)
            {
                parts.Add($"Tick {evt.TickCount}");
            }

            return parts.Count == 0 ? string.Empty : string.Join("; ", parts);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                return true;
            }
            return false;
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void OnActivated()
        {
            if (_disposed)
                return;

            if (_isActive)
            {
                DrainEvents();
                return;
            }

            _isActive = true;
            if (!_timer.IsEnabled)
            {
                try { _timer.Start(); } catch { /* ignore */ }
            }

            DrainEvents();
        }

        public void OnDeactivated()
        {
            if (_disposed || !_isActive)
                return;

            _isActive = false;
            try { _timer.Stop(); } catch { /* ignore */ }
        }
    }
}
