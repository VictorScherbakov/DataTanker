namespace DataTanker
{
    /// <summary>
    /// Simple wrapping class for values used by DataTanker storage.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValueOf<T> : IValue
    {
        public ValueOf(T value)
        {
            Value = value;
        }

        public T Value { get; set; }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static implicit operator ValueOf<T>(T value)
        {
            return new ValueOf<T>(value);
        }

        public static implicit operator T(ValueOf<T> value)
        {
            return value == null ? default(T) : value.Value;
        }
    }
}
