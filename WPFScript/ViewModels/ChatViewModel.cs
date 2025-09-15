using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MESharp.Services;

namespace MESharp.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        private readonly IChatService _chatService;
        private readonly IChatTimer _timer;
        private ulong _lastTimestamp;

        public ObservableCollection<MessageItem> Messages { get; } = new();

        public class MessageItem
        {
            public string Time { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
        }

        public ChatViewModel()
            : this(new InteropChatService(), new DispatcherChatTimer(TimeSpan.FromMilliseconds(750)))
        {
        }

        public ChatViewModel(IChatService chatService, IChatTimer timer)
        {
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _timer = timer ?? throw new ArgumentNullException(nameof(timer));
            _timer.Tick += OnTimerTick;
            _timer.Start();
            Refresh();
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            Refresh();
        }

        private void Refresh()
        {
            try
            {
                var list = _chatService.GetMessages();
                foreach (var message in list)
                {
                    if (message.Timestamp <= _lastTimestamp)
                    {
                        continue;
                    }

                    Messages.Add(new MessageItem
                    {
                        Time = DateTimeOffset.FromUnixTimeMilliseconds((long)message.Timestamp)
                            .ToLocalTime()
                            .ToString("HH:mm:ss"),
                        Name = message.Name ?? string.Empty,
                        Text = message.Text ?? string.Empty
                    });

                    _lastTimestamp = message.Timestamp;
                }

                const int max = 200;
                while (Messages.Count > max)
                {
                    Messages.RemoveAt(0);
                }
            }
            catch
            {
                // ignore
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
