namespace DataTanker
{
    /// <summary>
    /// Simple wrapping class for keys used by DataTanker storage.
    /// </summary>
    /// <typeparam name="T">The type of key</typeparam>
    public class KeyOf<T> : IKey
    {
        public KeyOf(T value)
        {
            Value = value;
        }

        public T Value { get; set; }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static implicit operator KeyOf<T>(T value)
        {
            return new KeyOf<T>(value);
        }

        public static implicit operator T(KeyOf<T> value)
        {
            return value == null ? default(T) : value.Value;
        }
    }
}