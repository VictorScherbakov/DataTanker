namespace DataTanker.BinaryFormat.Page
{
    /// <summary>
    /// Possible values of free-space-map entry.
    /// </summary>
    internal enum FsmValue : byte
    {
        Min = 0,
        Class0 = 0, 
        Class1 = 1, 
        Class2 = 2, 
        Class3 = 3, 
        Class4 = 4, 
        Class5 = 5,
        Class6 = 6,
        Class7 = 7,
        Class8 = 8,
        Class9 = 9,
        Class10 = 10,
        Class11 = 11,

        Reserved1 = 12,
        Reserved2 = 13,
        Reserved3 = 14,

        Full = 15,
        Max = 15
    }
}