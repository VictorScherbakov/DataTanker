namespace DataTanker
{
    using System;

    /// <summary>
    /// Simple wrapping class for comparable keys used by DataTanker storage.
    /// </summary>
    /// <typeparam name="T">The type of key</typeparam>
    public class ComparableKeyOf<T> : IComparableKey
        where T : IComparable
    {
        public ComparableKeyOf(T value)
        {
            Value = value;
        }

        public T Value { get; set; }

        public int CompareTo(object obj)
        {
            if (obj is T variable)
                return Value.CompareTo(variable);

            return Value.CompareTo(((ComparableKeyOf<T>)obj).Value);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static implicit operator ComparableKeyOf<T>(T value)
        {
            return new ComparableKeyOf<T>(value);
        }

        public static implicit operator T(ComparableKeyOf<T> value)
        {
            return value == null ? default(T) : value.Value;
        }
    }
}
