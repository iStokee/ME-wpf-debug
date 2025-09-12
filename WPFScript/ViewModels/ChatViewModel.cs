using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using csharp_interop.csharp_api;

namespace MESharp.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        private readonly DispatcherTimer _timer;
        public ObservableCollection<MessageItem> Messages { get; } = new();

        public class MessageItem
        {
            public string Time { get; set; } = "";
            public string Name { get; set; } = "";
            public string Text { get; set; } = "";
        }

        public ChatViewModel()
        {
            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(750), DispatcherPriority.Background, (s,e) => Refresh(), Dispatcher.CurrentDispatcher);
            _timer.Start();
            Refresh();
        }

        private ulong _lastTimestamp = 0;
        private void Refresh()
        {
            try
            {
                var list = Chat.GetMessages();
                // append new items based on pc timestamp
                foreach (var m in list)
                {
                    if (m.Timestamp <= _lastTimestamp) continue;
                    Messages.Add(new MessageItem
                    {
                        Time = DateTimeOffset.FromUnixTimeMilliseconds((long)m.Timestamp).ToLocalTime().ToString("HH:mm:ss"),
                        Name = m.Name,
                        Text = m.Text
                    });
                    _lastTimestamp = m.Timestamp;
                }
                // optional: cap list size
                const int max = 200;
                while (Messages.Count > max) Messages.RemoveAt(0);
            }
            catch { /* ignore */ }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Set<T>(ref T f, T v, [CallerMemberName] string? n = null)
        {
            if (!Equals(f, v)) { f = v; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n)); }
        }
    }
}

