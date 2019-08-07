namespace DataTanker.Utils
{
    using System.IO;

    internal static class StreamExtensions
    {
        /// <summary>
        /// Reads sequence of bytes from stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="length">The length of sequence to read</param>
        /// <returns>An array read</returns>
        public static byte[] BlockingRead(this Stream stream, int length)
        {
            var result = new byte[length];
            int read = 0;
            while (read < length)
            {
                var increment = stream.Read(result, read, length - read);
                if (increment == 0)
                    throw new EndOfStreamException();

                read += increment;
            }

            return result;
        }

        /// <summary>
        /// Reads sequence of bytes from stream to the specified array
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer">A buffer to read to</param>
        public static void BlockingRead(this Stream stream, byte[] buffer)
        {
            var length = buffer.Length;
            int read = 0;

            while (read < length)
            {
                var increment = stream.Read(buffer, read, length - read);
                if (increment == 0)
                    throw new EndOfStreamException();

                read += increment;
            }
        }
    }
}