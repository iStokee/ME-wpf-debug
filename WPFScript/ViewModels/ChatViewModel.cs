using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using csharp_interop.csharp_api;
using MESharp.Commands;

namespace MESharp.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        private const int SnapshotLimit = 200;
        private const int EventLimit = 300;

        private readonly DispatcherTimer _timer;
        private long _eventSequence;

        public ObservableCollection<MessageItem> Messages { get; } = new();
        public ObservableCollection<EventItem> LiveEvents { get; } = new();

        public bool HasMessages => Messages.Count > 0;
        public bool HasEvents => LiveEvents.Count > 0;

        public class MessageItem
        {
            public int Sequence { get; set; }
            public string Time { get; set; } = "";
            public string Name { get; set; } = "";
            public string Text { get; set; } = "";
            public string Extra1 { get; set; } = "";
            public string Extra2 { get; set; } = "";
            public string TimestampRaw { get; set; } = "";
            public string TimeTotalRaw { get; set; } = "";
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

        private bool _isLiveMode = true;
        public bool IsLiveMode
        {
            get => _isLiveMode;
            set
            {
                if (Set(ref _isLiveMode, value) && _isLiveMode)
                {
                    RefreshSnapshot(force: true);
                }
            }
        }

        private bool _captureEvents = true;
        public bool CaptureEvents
        {
            get => _captureEvents;
            set
            {
                if (Set(ref _captureEvents, value) && value)
                {
                    DrainEvents();
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ClearEventsCommand { get; }

        public ChatViewModel()
        {
            Messages.CollectionChanged += (_, __) => OnPropertyChanged(nameof(HasMessages));
            LiveEvents.CollectionChanged += (_, __) => OnPropertyChanged(nameof(HasEvents));

            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(750), DispatcherPriority.Background, OnTick, Dispatcher.CurrentDispatcher);
            _timer.Start();

            RefreshSnapshot(force: true);
            DrainEvents();

            RefreshCommand = new RelayCommand(_ =>
            {
                IsLiveMode = true;
                RefreshSnapshot(force: true);
                DrainEvents();
            });
            ClearCommand = new RelayCommand(_ => Messages.Clear());
            ClearEventsCommand = new RelayCommand(_ => LiveEvents.Clear());
        }

        private void OnTick(object? sender, EventArgs e)
        {
            RefreshSnapshot();
            DrainEvents();
        }

        private void RefreshSnapshot(bool force = false)
        {
            if (!IsLiveMode && !force)
                return;

            try
            {
                var list = Chat.GetMessages();
                Messages.Clear();

                if (list.Length == 0)
                    return;

                var start = Math.Max(0, list.Length - SnapshotLimit);
                int sequence = 1;

                for (int i = list.Length - 1; i >= start; i--)
                {
                    var m = list[i];
                    Messages.Add(new MessageItem
                    {
                        Sequence = sequence++,
                        Time = FormatMessageTime(m),
                        Name = m.Name,
                        Text = m.Text,
                        Extra1 = m.Extra1,
                        Extra2 = m.Extra2,
                        TimestampRaw = m.Timestamp.ToString(),
                        TimeTotalRaw = m.TimeTotal.ToString()
                    });
                }
            }
            catch
            {
                // ignore transient failures (e.g., native layer not initialised)
            }
        }

        private void DrainEvents()
        {
            if (!CaptureEvents)
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
                        Channel = string.IsNullOrWhiteSpace(evt.ChatType) ? "(unknown)" : evt.ChatType,
                        Names = ComposeNames(evt),
                        Text = evt.Text,
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
                // ignore
            }
        }

        private static string FormatMessageTime(Chat.Message message)
        {
            if (message.TimeTotal > 0)
            {
                var clamped = Math.Min(message.TimeTotal, 365UL * 24 * 60 * 60);
                return TimeSpan.FromSeconds(clamped).ToString(@"hh\:mm\:ss");
            }

            if (message.Timestamp > 0)
            {
                try
                {
                    if (message.Timestamp > 4_000_000_000UL)
                    {
                        var millis = Math.Min(message.Timestamp, (ulong)long.MaxValue);
                        return DateTimeOffset.FromUnixTimeMilliseconds((long)millis).ToLocalTime().ToString("HH:mm:ss");
                    }

                    var seconds = Math.Min(message.Timestamp, (ulong)long.MaxValue);
                    return DateTimeOffset.FromUnixTimeSeconds((long)seconds).ToLocalTime().ToString("HH:mm:ss");
                }
                catch
                {
                    // fall back to now
                }
            }

            return DateTime.Now.ToString("HH:mm:ss");
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

            if (!string.IsNullOrWhiteSpace(evt.TimestampFormatted))
                return evt.TimestampFormatted;

            return DateTime.Now.ToString("HH:mm:ss");
        }

        private static string ComposeNames(Chat.ChatEvent evt)
        {
            var parts = new[] { evt.Name, evt.Name2, evt.Name3 }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToArray();

            return parts.Length > 0 ? string.Join(" | ", parts) : "(n/a)";
        }

        private static string BuildDetails(Chat.ChatEvent evt)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(evt.SkillName))
            {
                var skill = evt.SkillName;
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

            if (!string.IsNullOrWhiteSpace(evt.TimestampFormatted))
            {
                parts.Add(evt.TimestampFormatted);
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
    }
}
