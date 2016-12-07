namespace DataTanker.BinaryFormat.Page
{
    /// <summary>
    /// Contains the bytes on-page offsets.
    /// </summary>
    internal static class OnPageOffsets
    {
        public static readonly int HeaderLength = 0;
        public static readonly int PageType = 2;
        public static readonly int SizeRange = 3;

        public static class MultipageItem
        {
            public static readonly int StartPageIndex = 4;
            public static readonly int PreviousPageIndex = 12;
            public static readonly int NextPageIndex = 20;
        }

        public static class FixedSizeItem
        {
            public static readonly short EmptySlotCount = 4;
        }

        public static class Heading
        {
            public static readonly int OnDiskStructureVersion = 4;
            public static readonly int PageSize = 8;
            public static readonly int FsmPageIndex = 12;
            public static readonly int AccessMethodPageIndex = 20;
            public static readonly int AccessMethod = 28;
        }

        public static class Fsm
        {
            public static readonly int BasePageIndex = 28;
        }

        public static class BPlusTreeNode
        {
            public static readonly int ParentPageIndex = 4;
            public static readonly int PreviousPageIndex = 12;
            public static readonly int NextPageIndex = 20;
            public static readonly int IsLeaf  = 28;
        }

        public static class RadixTreeNodes
        {
            public static readonly int FreeSpace = 4;
        }
    }
}