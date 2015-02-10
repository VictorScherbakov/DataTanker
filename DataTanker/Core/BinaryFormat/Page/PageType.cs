namespace DataTanker.BinaryFormat.Page
{
    internal enum PageType : byte 
    {
        Heading = 1,
        FreeSpaceMap = 2,
        FixedSizeItem = 3,
        MultipageItem = 4,
        BPlusTree = 5,
        RadixTree = 6
    }
}