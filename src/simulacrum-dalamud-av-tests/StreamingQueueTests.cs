using Simulacrum.AV.Buffers;

namespace Simulacrum.AV.Tests;

public class StreamingQueueTests
{
    public class Data
    {
        public int Value { get; init; }
    }

    public class EmptyQueue
    {
        private readonly StreamingQueue<Data> _queue;

        public EmptyQueue()
        {
            _queue = new StreamingQueue<Data>();
        }

        [Fact]
        public void Peek_ShouldReturnNull()
        {
            var data = _queue.Peek();

            Assert.Null(data);
        }
    }

    public class EmptyQueueGetEnumerator
    {
        private readonly StreamingQueue<Data> _queue;

        public EmptyQueueGetEnumerator()
        {
            _queue = new StreamingQueue<Data>();
        }

        [Fact]
        public void Current_ShouldBeNull()
        {
            using var current = _queue.GetEnumerator().Current;

            Assert.Null(current);
        }

        [Fact]
        public void MoveNext_Current_ShouldBeNull()
        {
            using var enumerator = _queue.GetEnumerator();
            enumerator.MoveNext();
            using var current = enumerator.Current;

            Assert.Null(current);
        }
    }

    public class OneElement
    {
        private const int PushedValue = 1;

        private readonly StreamingQueue<Data> _queue;

        public OneElement()
        {
            _queue = new StreamingQueue<Data>();
            _queue.Push(new Data { Value = PushedValue });
        }

        [Fact]
        public void Peek_ShouldReturnPushedValue()
        {
            var data = _queue.Peek();

            Assert.Equal(PushedValue, data?.Value.Value);
        }
    }

    public class OneElementGetEnumerator
    {
        private const int PushedValue = 1;

        private readonly StreamingQueue<Data> _queue;

        public OneElementGetEnumerator()
        {
            _queue = new StreamingQueue<Data>();
            _queue.Push(new Data { Value = PushedValue });
        }

        [Fact]
        public void Current_ShouldBeNull()
        {
            using var current = _queue.GetEnumerator().Current;

            Assert.Null(current);
        }

        [Fact]
        public void MoveNext_Current_ShouldBePushedValue()
        {
            using var enumerator = _queue.GetEnumerator();
            enumerator.MoveNext();
            using var current = enumerator.Current;

            Assert.Equal(PushedValue, current?.Value.Value);
        }

        [Fact]
        public void MoveNext_ShouldIncrementRefCount()
        {
            using var enumerator = _queue.GetEnumerator();
            enumerator.MoveNext();
            using var current = enumerator.Current;

            Assert.Equal(1, current?.RefCount);
        }

        [Fact]
        public void Current_Dispose_ShouldDecrementRefCount()
        {
            using var enumerator = _queue.GetEnumerator();
            enumerator.MoveNext();
            var current = enumerator.Current;
            current?.Dispose();

            Assert.Equal(0, current?.RefCount);
        }
    }

    public class StaleEnumerators
    {
        [Fact]
        public void MoveNext_ShouldGetNewPushedNodes()
        {
            using var queue = new StreamingQueue<Data>();
            using var enumerator = queue.GetEnumerator();
            enumerator.MoveNext();

            Assert.Null(enumerator.Current);

            queue.Push(new Data { Value = 1 });
            enumerator.MoveNext();

            Assert.Equal(1, enumerator.Current?.Value.Value);
        }

        [Fact]
        public void Clear_ShouldPurgeNextNode()
        {
            using var queue = new StreamingQueue<Data>();
            using var enumerator = queue.GetEnumerator();
            enumerator.MoveNext();

            Assert.Null(enumerator.Current);

            queue.Push(new Data { Value = 1 });
            queue.Push(new Data { Value = 2 });
            enumerator.MoveNext();

            queue.Clear();
            enumerator.MoveNext();

            Assert.Null(enumerator.Current);
        }
    }
}