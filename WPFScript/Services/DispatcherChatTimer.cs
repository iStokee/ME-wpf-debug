using System;
using System.Windows.Threading;

namespace MESharp.Services
{
    public sealed class DispatcherChatTimer : IChatTimer
    {
        private readonly DispatcherTimer _timer;

        public DispatcherChatTimer(TimeSpan interval)
        {
            _timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
            {
                Interval = interval
            };
        }

        public event EventHandler Tick
        {
            add => _timer.Tick += value;
            remove => _timer.Tick -= value;
        }

        public void Start()
        {
            _timer.Start();
        }
    }
}
