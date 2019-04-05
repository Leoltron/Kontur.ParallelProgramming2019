using System;

namespace ClusterClient
{
    public class ResultOrTimeout<T>
    {
        public static readonly ResultOrTimeout<T> Timeout = new ResultOrTimeout<T>(default(T), true);

        private ResultOrTimeout(T value, bool timeout = false)
        {
            Value = value;
            IsTimeout = timeout;
        }

        public T Value { get; }
        public bool IsTimeout { get; }

        public T ValueOrThrow => IsTimeout ? throw new TimeoutException() : Value;

        public static implicit operator ResultOrTimeout<T>(T value)
        {
            return new ResultOrTimeout<T>(value);
        }
    }
}
