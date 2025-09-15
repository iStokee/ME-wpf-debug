using System;
using System.Collections.Generic;
using MESharp.Models;
using MESharp.Services;
using MESharp.ViewModels;
using Xunit;

namespace MESharp.Tests.ViewModels
{
    public class ChatViewModelTests
    {
        [Fact]
        public void ConstructorLoadsInitialMessagesAndStartsTimer()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var message = new ChatMessage
            {
                Timestamp = (ulong)timestamp,
                Name = "Alice",
                Text = "Hello"
            };

            var service = FakeChatService.Create(message);
            var timer = new ManualChatTimer();

            var viewModel = new ChatViewModel(service, timer);

            Assert.True(timer.Started);
            Assert.Collection(viewModel.Messages, item =>
            {
                var expected = DateTimeOffset.FromUnixTimeMilliseconds((long)message.Timestamp)
                    .ToLocalTime()
                    .ToString("HH:mm:ss");
                Assert.Equal(expected, item.Time);
                Assert.Equal("Alice", item.Name);
                Assert.Equal("Hello", item.Text);
            });
        }

        [Fact]
        public void RefreshAddsOnlyNewerMessages()
        {
            var first = new ChatMessage { Timestamp = 100, Name = "Bob", Text = "First" };
            var second = new ChatMessage { Timestamp = 200, Name = "Carol", Text = "Second" };

            var service = new FakeChatService(
                () => new[] { first },
                () => new[] { first, second }
            );

            var timer = new ManualChatTimer();
            var viewModel = new ChatViewModel(service, timer);

            timer.RaiseTick();

            Assert.Equal(2, viewModel.Messages.Count);
            Assert.Equal("First", viewModel.Messages[0].Text);
            Assert.Equal("Second", viewModel.Messages[1].Text);
        }

        [Fact]
        public void RefreshSkipsMessagesWithDuplicateTimestamp()
        {
            var first = new ChatMessage { Timestamp = 100, Name = "Bob", Text = "First" };
            var duplicate = new ChatMessage { Timestamp = 100, Name = "Carol", Text = "Duplicate" };

            var service = new FakeChatService(
                () => new[] { first },
                () => new[] { duplicate, first }
            );

            var timer = new ManualChatTimer();
            var viewModel = new ChatViewModel(service, timer);

            timer.RaiseTick();

            Assert.Single(viewModel.Messages);
            Assert.Equal("First", viewModel.Messages[0].Text);
        }

        [Fact]
        public void RefreshKeepsOnlyMostRecentTwoHundredMessages()
        {
            var messages = new List<ChatMessage>();
            for (var i = 0; i < 205; i++)
            {
                messages.Add(new ChatMessage
                {
                    Timestamp = (ulong)(i + 1),
                    Name = $"User {i + 1}",
                    Text = $"Message #{i + 1}"
                });
            }

            var service = FakeChatService.Create(messages.ToArray());
            var timer = new ManualChatTimer();

            var viewModel = new ChatViewModel(service, timer);

            Assert.Equal(200, viewModel.Messages.Count);
            Assert.Equal("Message #6", viewModel.Messages[0].Text);
            Assert.Equal("Message #205", viewModel.Messages[^1].Text);
        }

        [Fact]
        public void RefreshContinuesAfterServiceException()
        {
            var first = new ChatMessage { Timestamp = 1, Name = "Tester", Text = "First" };
            var second = new ChatMessage { Timestamp = 2, Name = "Tester", Text = "Second" };

            var service = new FakeChatService(
                () => new[] { first },
                () => throw new InvalidOperationException("boom"),
                () => new[] { first, second }
            );

            var timer = new ManualChatTimer();
            var viewModel = new ChatViewModel(service, timer);

            timer.RaiseTick();
            timer.RaiseTick();

            Assert.Equal(2, viewModel.Messages.Count);
            Assert.Equal("Second", viewModel.Messages[1].Text);
        }

        private sealed class ManualChatTimer : IChatTimer
        {
            public event EventHandler? Tick;

            public bool Started { get; private set; }

            public void Start()
            {
                Started = true;
            }

            public void RaiseTick()
            {
                Tick?.Invoke(this, EventArgs.Empty);
            }
        }

        private sealed class FakeChatService : IChatService
        {
            private readonly Queue<Func<IReadOnlyList<ChatMessage>>> _responses;

            public FakeChatService(params Func<IReadOnlyList<ChatMessage>>[] responses)
            {
                _responses = new Queue<Func<IReadOnlyList<ChatMessage>>>(responses);
            }

            public IReadOnlyList<ChatMessage> GetMessages()
            {
                if (_responses.Count == 0)
                {
                    return Array.Empty<ChatMessage>();
                }

                return _responses.Dequeue().Invoke();
            }

            public static FakeChatService Create(params ChatMessage[] messages)
            {
                return new FakeChatService(() => messages);
            }
        }
    }
}
