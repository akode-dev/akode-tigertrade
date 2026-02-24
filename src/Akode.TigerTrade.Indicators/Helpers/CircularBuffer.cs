using System;
using System.Collections;
using System.Collections.Generic;

namespace Akode.TigerTrade.Indicators
{
    public class CircularBuffer<T> : IEnumerable<T>
    {
        private T[] _buffer;
        private readonly int _capacity;
        private int _head;
        private int _tail;
        public int Capacity => _capacity;
        public int Count { get; private set; }
        public T this[int index]
        {
            get
            {
                if (index >= Count) throw new IndexOutOfRangeException();
                return _buffer[GetInternalIndex(index)];
            }
            set
            {
                if (index >= Count) throw new IndexOutOfRangeException();
                _buffer[GetInternalIndex(index)] = value;
            }
        }
        public CircularBuffer(int capacity)
        {
            _capacity = capacity > 0 ? capacity : 1;
            _buffer = new T[_capacity];
            _head = 0;
            _tail = 0;
            Count = 0;
        }
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private int GetNextIndex(int current)
        {
            return (current + 1) % _capacity;
        }

        private int GetInternalIndex(int index)
        {
            return (_head + index) % _capacity;
        }

        public void Push(T obj)
        {
            if (_capacity == 0) return;
            _buffer[_tail] = obj;
            _tail = GetNextIndex(_tail);

            if (Count == _capacity)
            {
                _head = GetNextIndex(_head);
            }
            else
            {
                Count++;
            }
        }

        public T Pop()
        {
            if (Count == 0) return default(T);
            var obj = _buffer[_head];
            _buffer[_head] = default(T);
            _head = GetNextIndex(_head);
            Count--;
            return obj;
        }

        public T Peek()
        {
            if (Count == 0) return default(T);
            return _buffer[_head];
        }
    }
}
