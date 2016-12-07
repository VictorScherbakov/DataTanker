namespace DataTanker.BinaryFormat.Page
{
    /// <summary>
    /// Sizes of db items stored on pages.
    /// </summary>
    internal enum SizeRange : byte
    {
        Range0 = 0,   // up to 10 bytes (10 * 2 ^ 0)
        Range1 = 1,   // up to 20 bytes (10 * 2 ^ 1)
        Range2 = 2,   // up to 40 bytes (10 * 2 ^ 2)
        Range3 = 3,   // up to 80 bytes (10 * 2 ^ 3)
        Range4 = 4,   // up to 160 bytes (10 * 2 ^ 4)
        Range5 = 5,   // up to 320 bytes (10 * 2 ^ 5)
        Range6 = 6,   // up to 640 bytes (10 * 2 ^ 6)
        Range7 = 7,   // up to 1280 bytes (10 * 2 ^ 7)
        Range8 = 8,   // up to 2560 bytes (10 * 2 ^ 8)
        Range9 = 9,   // up to 5120 bytes (10 * 2 ^ 9)
        Range10 = 10, // up to 10240 bytes (10 * 2 ^ 10)
        Range11 = 11, // up to 20480 bytes (10 * 2 ^ 11)
        MultiPage = 12,     // object stored as page chain, actual size is limited by the size of storage
        NotApplicable = 13  // 
    }
}