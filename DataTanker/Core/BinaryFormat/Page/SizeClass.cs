namespace DataTanker.BinaryFormat.Page
{
    /// <summary>
    /// Sizes of db items stored on pages.
    /// </summary>
    internal enum SizeClass : byte
    {
        Class0 = 0,   // up to 10 bytes (10 * 2 ^ 0)
        Class1 = 1,   // up to 20 bytes (10 * 2 ^ 1)
        Class2 = 2,   // up to 40 bytes (10 * 2 ^ 2)
        Class3 = 3,   // up to 80 bytes (10 * 2 ^ 3)
        Class4 = 4,   // up to 160 bytes (10 * 2 ^ 4)
        Class5 = 5,   // up to 320 bytes (10 * 2 ^ 5)
        Class6 = 6,   // up to 640 bytes (10 * 2 ^ 6)
        Class7 = 7,   // up to 1280 bytes (10 * 2 ^ 7)
        Class8 = 8,   // up to 2560 bytes (10 * 2 ^ 8)
        Class9 = 9,   // up to 5120 bytes (10 * 2 ^ 9)
        Class10 = 10, // up to 10240 bytes (10 * 2 ^ 10)
        Class11 = 11, // up to 20480 bytes (10 * 2 ^ 11)
        MultiPage = 12,     // object stored as page chain, actual size is limited by the size of storage
        NotApplicable = 13  // 
    }
}